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
	private Node2D tileLevelLabelsNode;
	private readonly List<Label> tileLevelLabels = new();

	private static readonly MapTileData.TileColor[] TileColors =
	{
		MapTileData.TileColor.White, // 1
		MapTileData.TileColor.Red,   // 2
		MapTileData.TileColor.Blue,  // 3
		MapTileData.TileColor.White, // 4
		MapTileData.TileColor.Red,   // 5
		MapTileData.TileColor.Blue,  // 6
		MapTileData.TileColor.Black, // 7
		MapTileData.TileColor.Red,   // 8
		MapTileData.TileColor.White, // 9
		MapTileData.TileColor.Blue,  // 10
		MapTileData.TileColor.Red,   // 11
		MapTileData.TileColor.White  // 12
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
		tileLevelLabelsNode = GetNode<Node2D>("TileLevelLabels");

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

		bool loadedSave = SaveManager.Instance.TryLoadRequestedGame(allTiles, out BossSaveData savedBoss);
		if (!loadedSave)
			GameState.Instance.ResetToDefaults();

		CreateTileLevelLabels();

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

	private void CreateTileLevelLabels()
	{
		foreach (Node child in tileLevelLabelsNode.GetChildren())
			child.QueueFree();

		tileLevelLabels.Clear();

		for (int i = 0; i < allTiles.Count; i++)
		{
			Marker2D marker = tilePointsNode.GetNode<Marker2D>($"Marker2D{i}");
			Label label = new()
			{
				Name = $"TileLevel{i}",
				Text = allTiles[i].Level.ToString(),
				Position = marker.Position + new Vector2(-22, 62),
				Size = new Vector2(44, 36),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				MouseFilter = Control.MouseFilterEnum.Ignore,
				ZIndex = 20
			};

			label.AddThemeFontSizeOverride("font_size", 24);
			label.AddThemeColorOverride("font_color", Colors.White);
			label.AddThemeColorOverride("font_outline_color", Colors.Black);
			label.AddThemeConstantOverride("outline_size", 6);

			tileLevelLabelsNode.AddChild(label);
			tileLevelLabels.Add(label);
		}
	}

	private void UpdateTileLevelLabel(int tileIndex)
	{
		if (tileIndex < 0 || tileIndex >= tileLevelLabels.Count)
			return;

		tileLevelLabels[tileIndex].Text = allTiles[tileIndex].Level.ToString();
	}


	private static MapTileData CreateTile(MapTileData.TileColor tileColor)
	{
		MapTileData tile = new()
		{
			Level = tileColor == MapTileData.TileColor.White ? 0 : 1,
			Color = tileColor
		};

		tile.Value = tileColor == MapTileData.TileColor.White ? 0 : 1;
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
		if (GameState.Instance.CurrentTurnState != GameState.TurnState.WaitingForDiceSelection)
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
		BattleSystem.BattleOutcome outcome = await battleSystem.ResolveTurn();
		UpdatePlayerHealthDisplay();
		UpdateBossStatsDisplay();
		FinishTurn(outcome);

		return true;
	}

	private static void ApplySelectedDiceBonus(string color, int value)
	{
		if (color == "red")
			GameState.Instance.TempAtk += value;
		else if (color == "blue")
			GameState.Instance.TempDef += value;
	}

	public void ResolveTileEffect()
	{
		int tileIndex = GameState.Instance.PlayerPosition;
		MapTileData tile = allTiles[tileIndex];

		// 空白格：第一次踩中只激活，不给收益。
		// 第二次开始先按当前等级结算回血/护盾，再把地块升级到下一等级。
		if (tile.Color == MapTileData.TileColor.White)
		{
			if (tile.Level == 0)
			{
				tile.Level = 1;
				tile.Value = 1;
				UpdateTileLevelLabel(tileIndex);
				GD.Print("首次踩中空白格：地块已激活为 1 级，本次不获得回血或护盾");
				return;
			}

			ResolveWhiteTileEffect(tile);

			if (tile.Level < MapTileData.MaxUpgrade)
			{
				tile.Level += 1;
				tile.Value = tile.Level;
			}

			UpdateTileLevelLabel(tileIndex);
			return;
		}

		if (tile.Color == MapTileData.TileColor.Red && GameState.Instance.DiceColor == "red")
			GameState.Instance.TempAtk += tile.Value;
		else if (tile.Color == MapTileData.TileColor.Blue && GameState.Instance.DiceColor == "blue")
			GameState.Instance.TempDef += tile.Value;

		// 其他颜色仍按原规则：玩家效果结算后，地块自身始终成长。
		if (tile.Level >= MapTileData.MaxUpgrade)
			return;

		tile.Level += 1;

		switch (tile.Color)
		{
			case MapTileData.TileColor.Red:
			case MapTileData.TileColor.Blue:
				tile.Value += 1;
				break;
			case MapTileData.TileColor.Black:
				tile.Value -= 1;
				break;
		}

		UpdateTileLevelLabel(tileIndex);
	}

	private static void ResolveWhiteTileEffect(MapTileData tile)
	{
		if (GameState.Instance.PlayerHp < GameState.Instance.PlayerMaxHp)
		{
			int missingHp = GameState.Instance.PlayerMaxHp - GameState.Instance.PlayerHp;
			int actualHeal = Mathf.Min(tile.Value, missingHp);
			GameState.Instance.PlayerHp += actualHeal;
			GD.Print($"踩中空白格：回复 {actualHeal} 点生命（地块当前回血值 {tile.Value}）");
			return;
		}

		if (GameState.Instance.PlayerShield < GameState.MaxShield)
		{
			int shieldGain = Mathf.Min(tile.Value, GameState.MaxShield - GameState.Instance.PlayerShield);
			GameState.Instance.PlayerShield += shieldGain;
			GD.Print($"满血踩中空白格：获得 {shieldGain} 点护盾（地块等级 {tile.Level}），当前护盾 {GameState.Instance.PlayerShield}/{GameState.MaxShield}");
			return;
		}

		GD.Print("满血且护盾已满：空白格本次对玩家无效果");
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
		goblin = bossRushSystem.RestoreBoss(savedBoss.Index, savedBoss.CurrentHp);
		if (goblin == null)
		{
			GD.PrintErr("Boss 读档失败，无法恢复当前 Boss");
			return;
		}

		goblin.HpChanged += OnBossHpChanged;
		battleSystem.Setup(player, goblin);
		UpdateBossStatsDisplay();
	}

	public bool SaveCurrentGame()
	{
		if (goblin == null || !IsInstanceValid(goblin) || goblin.CurrentHp <= 0)
			return false;

		return SaveManager.Instance.SaveGame(
			allTiles,
			bossRushSystem.CurrentBossIndex,
			goblin.CurrentHp
		);
	}

	public bool SaveIfStable()
	{
		if (GameState.Instance.CurrentTurnState != GameState.TurnState.ReadyToRoll)
			return false;

		return SaveCurrentGame();
	}

	private void UpdatePlayerStatsDisplay()
	{
		int totalAtk = GameState.Instance.PlayerAtk + GameState.Instance.TempAtk;
		int totalDef = GameState.Instance.PlayerDef + GameState.Instance.TempDef;
		playerStatsLabel.Text =
			$"玩家属性\n" +
			$"攻击 = {totalAtk}\n" +
			$"防御 = {totalDef}\n" +
			$"护盾 = {GameState.Instance.PlayerShield} / {GameState.MaxShield}";
	}

	private void UpdateBossStatsDisplay()
	{
		if (goblin == null || !IsInstanceValid(goblin))
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

	private void OnBossHpChanged(int currentHp, int maxHp)
	{
		UpdateBossStatsDisplay();
	}

	private void UpdatePlayerHealthDisplay()
	{
		playerHpBar.MaxValue = GameState.Instance.PlayerMaxHp;
		playerHpBar.Value = GameState.Instance.PlayerHp;
		playerHpLabel.Text = $"玩家 HP  {GameState.Instance.PlayerHp} / {GameState.Instance.PlayerMaxHp}";
	}

	private void FinishTurn(BattleSystem.BattleOutcome outcome)
	{
		GameState.Instance.ResetTempStats();
		UpdatePlayerStatsDisplay();

		switch (outcome)
		{
			case BattleSystem.BattleOutcome.EnemyDefeated:
				if (bossRushSystem.HasNextBoss)
				{
					SetTurnState(GameState.TurnState.Upgrading);
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
				SetTurnState(GameState.TurnState.ReadyToRoll);
				SaveCurrentGame();
				break;
		}
	}

	private void ShowUpgradePanel()
	{
		upgradeTitleLabel.Text = $"击败 {goblin.DisplayName}！选择一项升级";
		upgradeCurrentStatsLabel.Text =
			$"当前：HP {GameState.Instance.PlayerHp}/{GameState.Instance.PlayerMaxHp}    " +
			$"攻击 {GameState.Instance.PlayerAtk}    防御 {GameState.Instance.PlayerDef}    " +
			$"护盾 {GameState.Instance.PlayerShield}/{GameState.MaxShield}";

		upgradeAttackButton.Text = $"攻击 +{AttackUpgradeAmount}";
		upgradeDefenseButton.Text = $"防御 +{DefenseUpgradeAmount}";
		upgradeHpButton.Text = $"最大生命 +{HpUpgradeAmount}";
		upgradePanel.Visible = true;
	}

	private void ApplyUpgrade(string choice, int amount)
	{
		if (GameState.Instance.CurrentTurnState != GameState.TurnState.Upgrading)
			return;

		if (!player.Upgrade(choice, amount))
			return;

		upgradePanel.Visible = false;
		UpdatePlayerHealthDisplay();
		UpdatePlayerStatsDisplay();

		if (bossRushSystem.HasNextBoss)
		{
			SpawnNextBoss();
			SetTurnState(GameState.TurnState.ReadyToRoll);
			SaveCurrentGame();
		}
		else
		{
			// 正常流程中最后一只 Boss 不会再进入升级界面，
			// 这里保留为兜底入口。
			EnterVictoryScene();
		}
	}

	// 对外保留明确的胜利入口，后续剧情、事件或调试也可以直接调用。
	public void EnterVictoryScene()
	{
		SetTurnState(GameState.TurnState.GameOver);
		GD.Print("所有 Boss 已全部击败，进入通关场景");
		GetTree().ChangeSceneToFile("res://Scenes/Victory.tscn");
	}

	// 对外保留明确的失败入口。
	public void EnterDefeatScene()
	{
		SetTurnState(GameState.TurnState.GameOver);
		GD.Print("玩家被击败，进入失败场景");
		GetTree().ChangeSceneToFile("res://Scenes/GameOver.tscn");
	}

	private void OnUpgradeAttackPressed() => ApplyUpgrade("atk", AttackUpgradeAmount);
	private void OnUpgradeDefensePressed() => ApplyUpgrade("def", DefenseUpgradeAmount);
	private void OnUpgradeHpPressed() => ApplyUpgrade("hp", HpUpgradeAmount);

	public void SetTurnState(GameState.TurnState newState)
	{
		GameState.Instance.CurrentTurnState = newState;
		rollButton.Disabled = newState != GameState.TurnState.ReadyToRoll;
	}

	private async void OnDiceSelected(string color)
	{
		await HandleDiceSelected(color);
	}

	private async void OnRollButtonPressed()
	{
		if (GameState.Instance.CurrentTurnState != GameState.TurnState.ReadyToRoll)
			return;

		SetTurnState(GameState.TurnState.Rolling);
		GameState.Instance.ResetTempStats();
		UpdatePlayerStatsDisplay();
		RollDice();

		Task redAnimation = redDice.PlayRollAnimation(GameState.Instance.Dice["red"]);
		Task blueAnimation = blueDice.PlayRollAnimation(GameState.Instance.Dice["blue"]);
		await Task.WhenAll(redAnimation, blueAnimation);

		SetTurnState(GameState.TurnState.WaitingForDiceSelection);
	}
}
