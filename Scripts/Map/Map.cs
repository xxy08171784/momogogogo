using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class Map : Node2D
{
	private const int AttackUpgradeAmount = 1;
	private const int DefenseUpgradeAmount = 1;
	private const int HpUpgradeAmount = 5;

	private Player player;
	private Goblin goblin;
	private BattleSystem battleSystem;
	private BossRushSystem bossRushSystem;
	private Dice redDice;
	private Dice blueDice;
	private Button rollButton;

	// 摇骰子音效
	private AudioStreamPlayer diceRollSound;

	private ProgressBar playerHpBar;
	private Label playerHpLabel;
	private Label playerStatsLabel;
	private Label bossStatsLabel;

	private ColorRect upgradePanel;
	private Label upgradeTitleLabel;
	private Label upgradeCurrentStatsLabel;
	private Button upgradeAttackButton;
	private Button upgradeDefenseButton;
	private Button upgradeHpButton;
	private Node2D tilePointsNode;
	private TileSystem tileSystem;

	private static readonly MapTileData.TileColor[] TileColors =
	{
		MapTileData.TileColor.White,
		MapTileData.TileColor.Red,
		MapTileData.TileColor.Blue,
		MapTileData.TileColor.White,
		MapTileData.TileColor.Red,
		MapTileData.TileColor.Blue,
		MapTileData.TileColor.Black,
		MapTileData.TileColor.Red,
		MapTileData.TileColor.White,
		MapTileData.TileColor.Blue,
		MapTileData.TileColor.Red,
		MapTileData.TileColor.White
	};

	private readonly List<MapTileData> allTiles = new();

	public override void _Ready()
	{
		player = GetNode<Player>("Entities/Player");
		battleSystem = GetNode<BattleSystem>("BattleSystem");
		bossRushSystem = GetNode<BossRushSystem>("BossRushSystem");
		redDice = GetNode<Dice>("Entities/RedDice");
		blueDice = GetNode<Dice>("Entities/BlueDice");
		rollButton = GetNode<Button>("UI/RollButton");

		// 获取摇骰子音效节点
		diceRollSound = GetNode<AudioStreamPlayer>("DiceRollSound");

		playerHpBar = GetNode<ProgressBar>("UI/PlayerHealthBar");
		playerHpLabel = GetNode<Label>("UI/PlayerHealthBar/HpLabel");
		playerStatsLabel = GetNode<Label>("UI/PlayerStatsPanel/StatsLabel");
		bossStatsLabel = GetNode<Label>("UI/GoblinStatsPanel/StatsLabel");

		upgradePanel = GetNode<ColorRect>("UI/UpgradePanel");
		upgradeTitleLabel = GetNode<Label>("UI/UpgradePanel/Title");
		upgradeCurrentStatsLabel = GetNode<Label>("UI/UpgradePanel/CurrentStats");
		upgradeAttackButton = GetNode<Button>("UI/UpgradePanel/AttackButton");
		upgradeDefenseButton = GetNode<Button>("UI/UpgradePanel/DefenseButton");
		upgradeHpButton = GetNode<Button>("UI/UpgradePanel/HpButton");

		tilePointsNode = GetNode<Node2D>("TilePoints");

		rollButton.Pressed += OnRollButtonPressed;
		redDice.Selected += OnDiceSelected;
		blueDice.Selected += OnDiceSelected;

		upgradeAttackButton.Pressed += OnUpgradeAttackPressed;
		upgradeDefenseButton.Pressed += OnUpgradeDefensePressed;
		upgradeHpButton.Pressed += OnUpgradeHpPressed;

		bossRushSystem.Setup(
			GetNode<Node2D>("Entities"),
			GetNode<Marker2D>("Entities/BossSpawnPoint")
		);

		CreateMap();

		bool loadedSave = SaveManager.Instance.TryLoadRequestedGame(
			allTiles,
			out BossSaveData savedBoss
		);

		if (!loadedSave)
			GameState.Instance.ResetToDefaults();

		CreateTileSystem();

		player.SnapToTile(GameState.Instance.PlayerPosition);

		UpdatePlayerHealthDisplay();
		UpdatePlayerStatsDisplay();

		if (loadedSave)
			RestoreBoss(savedBoss);
		else
			SpawnNextBoss();

		SetTurnState(GameState.TurnState.ReadyToRoll);

		if (!loadedSave)
			SaveCurrentGame();
	}

	private void CreateMap()
	{
		allTiles.Clear();

		foreach (MapTileData.TileColor color in TileColors)
			allTiles.Add(CreateTile(color));
	}

	private void CreateTileSystem()
	{
		tileSystem = new TileSystem
		{
			Name = "TileSystem"
		};

		AddChild(tileSystem);
		tileSystem.Initialize(allTiles, tilePointsNode, rollButton);
	}

	private static MapTileData CreateTile(
		MapTileData.TileColor tileColor
	)
	{
		MapTileData tile = new()
		{
			Level = tileColor == MapTileData.TileColor.White ? 0 : 1,
			Color = tileColor
		};

		tile.Value =
			tileColor == MapTileData.TileColor.White ? 0 : 1;

		return tile;
	}

	public void RollDice()
	{
		GameState.Instance.Dice["red"] = GD.RandRange(1, 6);
		GameState.Instance.Dice["blue"] = GD.RandRange(1, 6);
	}

	public bool SelectDiceColor(string color)
	{
		if (color != "red" && color != "blue")
			return false;

		GameState.Instance.DiceColor = color;
		return true;
	}

	public async Task<bool> HandleDiceSelected(string color)
	{
		if (
			GameState.Instance.CurrentTurnState
			!= GameState.TurnState.WaitingForDiceSelection
		)
			return false;

		if (!SelectDiceColor(color))
			return false;

		int steps = GameState.Instance.Dice[color];

		ApplySelectedDiceBonus(color, steps);
		UpdatePlayerStatsDisplay();

		SetTurnState(GameState.TurnState.Moving);

		await player.MoveBySteps(steps);

		SetTurnState(GameState.TurnState.ResolvingTile);

		ResolveTileEffect();

		UpdatePlayerHealthDisplay();
		UpdatePlayerStatsDisplay();

		SetTurnState(GameState.TurnState.Battling);

		BattleSystem.BattleOutcome outcome =
			await battleSystem.ResolveTurn();

		UpdatePlayerHealthDisplay();
		UpdateBossStatsDisplay();

		FinishTurn(outcome);

		return true;
	}

	private static void ApplySelectedDiceBonus(
		string color,
		int value
	)
	{
		if (color == "red")
			GameState.Instance.TempAtk += value;
		else if (color == "blue")
			GameState.Instance.TempDef += value;
	}

	public void ResolveTileEffect()
	{
		// 地块结算已移入 TileSystem，避免 Map.cs 与队友战斗改动冲突。
		tileSystem.ResolveLandedTile();
	}

	private void SpawnNextBoss()
	{
		if (goblin != null && IsInstanceValid(goblin))
			goblin.HpChanged -= OnBossHpChanged;

		goblin = bossRushSystem.SpawnNextBoss();

		if (goblin == null)
		{
			SetTurnState(GameState.TurnState.GameOver);
			return;
		}

		goblin.HpChanged += OnBossHpChanged;

		battleSystem.Setup(player, goblin);

		UpdateBossStatsDisplay();
	}

	private void RestoreBoss(BossSaveData savedBoss)
	{
		goblin = bossRushSystem.RestoreBoss(
			savedBoss.Index,
			savedBoss.CurrentHp
		);

		if (goblin == null)
		{
			GD.PrintErr(
				"Boss 读档失败，无法恢复当前 Boss"
			);

			return;
		}

		goblin.HpChanged += OnBossHpChanged;

		battleSystem.Setup(player, goblin);

		UpdateBossStatsDisplay();
	}

	public bool SaveCurrentGame()
	{
		if (
			goblin == null
			|| !IsInstanceValid(goblin)
			|| goblin.CurrentHp <= 0
		)
			return false;

		return SaveManager.Instance.SaveGame(
			allTiles,
			bossRushSystem.CurrentBossIndex,
			goblin.CurrentHp
		);
	}

	public bool SaveIfStable()
	{
		if (
			GameState.Instance.CurrentTurnState
			!= GameState.TurnState.ReadyToRoll
		)
			return false;

		return SaveCurrentGame();
	}

	private void UpdatePlayerStatsDisplay()
	{
		int totalAtk =
			GameState.Instance.PlayerAtk
			+ GameState.Instance.TempAtk;

		int totalDef =
			GameState.Instance.PlayerDef
			+ GameState.Instance.TempDef;

		playerStatsLabel.Text =
			$"玩家属性\n" +
			$"攻击 = {totalAtk}\n" +
			$"防御 = {totalDef}\n" +
			$"护盾 = {GameState.Instance.PlayerShield} / {GameState.MaxShield}";
	}

	private void UpdateBossStatsDisplay()
	{
		if (
			goblin == null
			|| !IsInstanceValid(goblin)
		)
		{
			bossStatsLabel.Text = "Boss 已全部击败";
			return;
		}

		bossStatsLabel.Text =
			$"第 {bossRushSystem.CurrentBossNumber} / {bossRushSystem.BossCount} 只\n" +
			$"{goblin.DisplayName}\n" +
			$"HP = {goblin.CurrentHp} / {goblin.MaxHp}\n" +
			$"攻击 = {goblin.Atk}\n" +
			$"防御 = {goblin.Def}";
	}

	private void OnBossHpChanged(
		int currentHp,
		int maxHp
	)
	{
		UpdateBossStatsDisplay();
	}

	private void UpdatePlayerHealthDisplay()
	{
		playerHpBar.MaxValue =
			GameState.Instance.PlayerMaxHp;

		playerHpBar.Value =
			GameState.Instance.PlayerHp;

		playerHpLabel.Text =
			$"玩家 HP  {GameState.Instance.PlayerHp} / {GameState.Instance.PlayerMaxHp}";
	}

	private void FinishTurn(
		BattleSystem.BattleOutcome outcome
	)
	{
		GameState.Instance.ResetTempStats();

		UpdatePlayerStatsDisplay();

		switch (outcome)
		{
			case BattleSystem.BattleOutcome.EnemyDefeated:

				if (bossRushSystem.HasNextBoss)
				{
					SetTurnState(
						GameState.TurnState.Upgrading
					);

					ShowUpgradePanel();
				}
				else
				{
					EnterVictoryScene();
				}

				break;

			case BattleSystem.BattleOutcome.PlayerDefeated:

				EnterDefeatScene();

				break;

			default:

				SetTurnState(
					GameState.TurnState.ReadyToRoll
				);

				SaveCurrentGame();

				break;
		}
	}

	private void ShowUpgradePanel()
	{
		upgradeTitleLabel.Text =
			$"击败 {goblin.DisplayName}！选择一项升级";

		upgradeCurrentStatsLabel.Text =
			$"当前：HP {GameState.Instance.PlayerHp}/{GameState.Instance.PlayerMaxHp}    " +
			$"攻击 {GameState.Instance.PlayerAtk}    " +
			$"防御 {GameState.Instance.PlayerDef}    " +
			$"护盾 {GameState.Instance.PlayerShield}/{GameState.MaxShield}";

		upgradeAttackButton.Text =
			$"攻击 +{AttackUpgradeAmount}";

		upgradeDefenseButton.Text =
			$"防御 +{DefenseUpgradeAmount}";

		upgradeHpButton.Text =
			$"最大生命 +{HpUpgradeAmount}";

		upgradePanel.Visible = true;
	}

	private void ApplyUpgrade(
		string choice,
		int amount
	)
	{
		if (
			GameState.Instance.CurrentTurnState
			!= GameState.TurnState.Upgrading
		)
			return;

		if (!player.Upgrade(choice, amount))
			return;

		upgradePanel.Visible = false;

		UpdatePlayerHealthDisplay();
		UpdatePlayerStatsDisplay();

		if (bossRushSystem.HasNextBoss)
		{
			SpawnNextBoss();

			SetTurnState(
				GameState.TurnState.ReadyToRoll
			);

			SaveCurrentGame();
		}
		else
		{
			EnterVictoryScene();
		}
	}

	public void EnterVictoryScene()
	{
		SetTurnState(
			GameState.TurnState.GameOver
		);

		GD.Print(
			"所有 Boss 已全部击败，进入通关场景"
		);

		GetTree().ChangeSceneToFile(
			"res://Scenes/Victory.tscn"
		);
	}

	public void EnterDefeatScene()
	{
		SetTurnState(
			GameState.TurnState.GameOver
		);

		GD.Print(
			"玩家被击败，进入失败场景"
		);

		GetTree().ChangeSceneToFile(
			"res://Scenes/GameOver.tscn"
		);
	}

	private void OnUpgradeAttackPressed()
	{
		ApplyUpgrade(
			"atk",
			AttackUpgradeAmount
		);
	}

	private void OnUpgradeDefensePressed()
	{
		ApplyUpgrade(
			"def",
			DefenseUpgradeAmount
		);
	}

	private void OnUpgradeHpPressed()
	{
		ApplyUpgrade(
			"hp",
			HpUpgradeAmount
		);
	}

	public void SetTurnState(
		GameState.TurnState newState
	)
	{
		GameState.Instance.CurrentTurnState =
			newState;

		rollButton.Disabled =
			newState != GameState.TurnState.ReadyToRoll;

		tileSystem?.OnTurnStateChanged(newState);
	}

	private async void OnDiceSelected(
		string color
	)
	{
		await HandleDiceSelected(color);
	}

	private async void OnRollButtonPressed()
	{
		if (
			GameState.Instance.CurrentTurnState
			!= GameState.TurnState.ReadyToRoll
		)
			return;

		SetTurnState(
			GameState.TurnState.Rolling
		);

		// 幸运"免费行动"：若积攒了免费行动，本回合战斗怪物不反击
		if (GameState.Instance.FreeActionsPending > 0)
		{
			GameState.Instance.FreeActionsPending -= 1;
			GameState.Instance.NoCounterThisBattle = true;
			GD.Print("幸运：本次行动怪物不反击");
		}

		GameState.Instance.ResetTempStats();
  
		UpdatePlayerStatsDisplay();

		// 生成两个骰子的结果
		RollDice();

		// 播放摇骰子音效
		diceRollSound.Play();

		// 红蓝骰子同时播放摇骰子动画
		Task redAnimation =
			redDice.PlayRollAnimation(
				GameState.Instance.Dice["red"]
			);

		Task blueAnimation =
			blueDice.PlayRollAnimation(
				GameState.Instance.Dice["blue"]
			);

		// 等待两个骰子动画全部结束
		await Task.WhenAll(
			redAnimation,
			blueAnimation
		);

		SetTurnState(
			GameState.TurnState.WaitingForDiceSelection
		);
	}
}
