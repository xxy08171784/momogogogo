using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class Map : Node2D
{
	private const int AttackUpgradeAmount = 1;
	private const int DefenseUpgradeAmount = 1;
	private const int HpUpgradeAmount = 5;
	private const int DiceUpgradeAmount = 3;
	private const int ResonanceUpgradeAmount = 10;
	private const int LifestealUpgradeAmount = 3;
	private const int TurnShieldUpgradeAmount = 4;

	private enum CharacterUpgradeType
	{
		Attack,
		Defense,
		MaxHp,
		RedDice,
		BlueDice,
		DoubleDiceResonance,
		Lifesteal,
		TurnShield
	}

	private readonly CharacterUpgradeType[] currentUpgradeChoices =
		new CharacterUpgradeType[3];

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
		GameState state = GameState.Instance;

		if (color == "red")
		{
			state.TempAtk += value;
			state.TempAtk += state.RedDiceAtkBonus;
		}
		else if (color == "blue")
		{
			state.TempDef += value;
			state.TempDef += state.BlueDiceDefBonus;
		}

		if (
			state.Dice["red"] == state.Dice["blue"]
			&& state.DoubleDiceResonanceBonus > 0
		)
		{
			state.TempAtk += state.DoubleDiceResonanceBonus;
			state.TempDef += state.DoubleDiceResonanceBonus;
			GD.Print(
				$"双骰共鸣：红蓝骰点数相同，本回合攻防 +{state.DoubleDiceResonanceBonus}"
			);
		}
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
			savedBoss.CurrentHp,
			savedBoss.ActionIndex,
			savedBoss.NextAttackMultiplier,
			savedBoss.CurrentShield,
			savedBoss.PowerBonus
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
			goblin.CurrentHp,
			goblin.CurrentActionIndex,
			goblin.NextAttackMultiplier,
			goblin.CurrentShield,
			goblin.PowerBonus
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
		GameState state = GameState.Instance;
		int totalAtk = Mathf.Max(
			Mathf.RoundToInt(
				(state.PlayerAtk + state.TempAtk)
				* state.AttackMultiplier
			) + state.FlatDamageBonus,
			0
		);
		int totalDef = state.GetEffectiveDefense();

		playerStatsLabel.Text =
			$"玩家属性\n" +
			$"攻击 = {totalAtk}\n" +
			$"防御 = {totalDef}\n" +
			$"护盾 = {state.TotalPlayerShield}";
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

		MonsterAction action = goblin.GetCurrentAction();
		string chargeText = goblin.NextAttackMultiplier > 1
			? $"\n蓄力：下一次攻击 x{goblin.NextAttackMultiplier}"
			: "";
		string shieldText = goblin.CurrentShield > 0
			? $"\n护盾 = {goblin.CurrentShield}"
			: "";
		string powerText = goblin.PowerBonus > 0
			? $"\n力量：{goblin.PowerBonus}"
			: "";

		bossStatsLabel.Text =
			$"第 {bossRushSystem.CurrentBossNumber} / {bossRushSystem.BossCount} 只\n" +
			$"{goblin.DisplayName}\n" +
			$"HP = {goblin.CurrentHp} / {goblin.MaxHp}\n" +
			$"攻击 = {goblin.Atk}    防御 = {goblin.Def}\n" +
			$"行动 {goblin.CurrentActionIndex + 1}/{goblin.ActionCount}：{action.Name}\n" +
			$"{action.Description}" +
			chargeText +
			shieldText +
			powerText;
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
		GameState.Instance.ResetTileTurnModifiers();
		GameState.Instance.EndTurn();

		UpdatePlayerStatsDisplay();

		switch (outcome)
		{
			case BattleSystem.BattleOutcome.EnemyDefeated:
				SetTurnState(
					GameState.TurnState.Upgrading
				);
				ShowUpgradePanel();

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
		RollUpgradeChoices();

		upgradeTitleLabel.Text =
			$"击败 {goblin.DisplayName}！选择一项升级";

		upgradeCurrentStatsLabel.Text =
			$"当前：HP {GameState.Instance.PlayerHp}/{GameState.Instance.PlayerMaxHp}    " +
			$"攻击 {GameState.Instance.PlayerAtk}    " +
			$"防御 {GameState.Instance.PlayerDef}    " +
			$"护盾 {GameState.Instance.TotalPlayerShield}";

		upgradeAttackButton.Text = GetUpgradeText(currentUpgradeChoices[0]);
		upgradeDefenseButton.Text = GetUpgradeText(currentUpgradeChoices[1]);
		upgradeHpButton.Text = GetUpgradeText(currentUpgradeChoices[2]);

		upgradePanel.Visible = true;
	}

	private void RollUpgradeChoices()
	{
		List<CharacterUpgradeType> pool = new()
		{
			CharacterUpgradeType.Attack,
			CharacterUpgradeType.Defense,
			CharacterUpgradeType.MaxHp,
			CharacterUpgradeType.RedDice,
			CharacterUpgradeType.BlueDice,
			CharacterUpgradeType.DoubleDiceResonance,
			CharacterUpgradeType.Lifesteal,
			CharacterUpgradeType.TurnShield
		};

		for (int choiceIndex = 0; choiceIndex < currentUpgradeChoices.Length; choiceIndex++)
		{
			int totalWeight = 0;
			foreach (CharacterUpgradeType type in pool)
				totalWeight += GetUpgradeWeight(type);

			int roll = GD.RandRange(1, totalWeight);
			int cumulative = 0;
			int selectedIndex = 0;

			for (int i = 0; i < pool.Count; i++)
			{
				cumulative += GetUpgradeWeight(pool[i]);
				if (roll <= cumulative)
				{
					selectedIndex = i;
					break;
				}
			}

			currentUpgradeChoices[choiceIndex] = pool[selectedIndex];
			pool.RemoveAt(selectedIndex);
		}
	}

	private static int GetUpgradeWeight(CharacterUpgradeType type)
	{
		return type switch
		{
			CharacterUpgradeType.Attack => 3,
			CharacterUpgradeType.Defense => 3,
			CharacterUpgradeType.MaxHp => 3,
			CharacterUpgradeType.RedDice => 3,
			CharacterUpgradeType.BlueDice => 3,
			CharacterUpgradeType.DoubleDiceResonance => 1,
			CharacterUpgradeType.Lifesteal => 1,
			CharacterUpgradeType.TurnShield => 1,
			_ => 1
		};
	}

	private static string GetUpgradeText(CharacterUpgradeType type)
	{
		return type switch
		{
			CharacterUpgradeType.Attack =>
				"攻击强化\n基础攻击永久 +1",
			CharacterUpgradeType.Defense =>
				"防御强化\n基础防御永久 +1",
			CharacterUpgradeType.MaxHp =>
				"生命强化\n最大生命 +5\n立即回复5点",
			CharacterUpgradeType.RedDice =>
				"红骰磨砺\n选择红骰时攻击 +3",
			CharacterUpgradeType.BlueDice =>
				"蓝骰磨砺\n选择蓝骰时防御 +3",
			CharacterUpgradeType.DoubleDiceResonance =>
				"双骰共鸣\n红蓝点数相同\n本回合攻防 +10",
			CharacterUpgradeType.Lifesteal =>
				"嗜血\n造成伤害后回复3\n每回合1次",
			CharacterUpgradeType.TurnShield =>
				"护盾\n每回合开始获得4护盾\n回合结束消失",
			_ => "未知升级"
		};
	}

	private void ApplyUpgrade(CharacterUpgradeType choice)
	{
		if (
			GameState.Instance.CurrentTurnState
			!= GameState.TurnState.Upgrading
		)
			return;

		GameState state = GameState.Instance;
		switch (choice)
		{
			case CharacterUpgradeType.Attack:
				state.PlayerAtk += AttackUpgradeAmount;
				break;
			case CharacterUpgradeType.Defense:
				state.PlayerDef += DefenseUpgradeAmount;
				player.PlayDefendSound();
				break;
			case CharacterUpgradeType.MaxHp:
				state.PlayerMaxHp += HpUpgradeAmount;
				state.HealPlayer(HpUpgradeAmount);
				player.PlayHealSound();
				break;
			case CharacterUpgradeType.RedDice:
				state.RedDiceAtkBonus += DiceUpgradeAmount;
				break;
			case CharacterUpgradeType.BlueDice:
				state.BlueDiceDefBonus += DiceUpgradeAmount;
				break;
			case CharacterUpgradeType.DoubleDiceResonance:
				state.DoubleDiceResonanceBonus += ResonanceUpgradeAmount;
				break;
			case CharacterUpgradeType.Lifesteal:
				state.LifestealHealAmount += LifestealUpgradeAmount;
				break;
			case CharacterUpgradeType.TurnShield:
				state.TurnStartShieldAmount += TurnShieldUpgradeAmount;
				break;
		}

		state.PlayerLevel += 1;
		player.PlayUpgradeSound();
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
		ApplyUpgrade(currentUpgradeChoices[0]);
	}

	private void OnUpgradeDefensePressed()
	{
		ApplyUpgrade(currentUpgradeChoices[1]);
	}

	private void OnUpgradeHpPressed()
	{
		ApplyUpgrade(currentUpgradeChoices[2]);
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

		GameState.Instance.BeginTurn();
		UpdatePlayerStatsDisplay();

		// 幸运"免费行动"：下一次玩家行动中，怪物跳过自己的主动行动。
		// 黑化领域的原地战斗同样属于一次玩家行动，因此也可以消耗幸运。
		if (GameState.Instance.FreeActionsPending > 0)
		{
			GameState.Instance.FreeActionsPending -= 1;
			GameState.Instance.NoCounterThisBattle = true;
			GD.Print("幸运：本次行动怪物跳过行动");
		}

		if (GameState.Instance.SkipMovementNextTurn)
		{
			// 黑化领域：本回合完全跳过骰子、移动和地块收益，
			// TempAtk/TempDef 保持 0，直接使用玩家基础攻防进入战斗。
			GameState.Instance.SkipMovementNextTurn = false;
			GameState.Instance.ResetTempStats();
			GameState.Instance.ResetTileTurnModifiers();
			UpdatePlayerStatsDisplay();
			UpdateBossStatsDisplay();

			GD.Print("黑化领域生效：玩家本回合原地以基础攻防进行战斗");
			SetTurnState(GameState.TurnState.Battling);

			BattleSystem.BattleOutcome lockedOutcome = await battleSystem.ResolveTurn();
			UpdatePlayerHealthDisplay();
			UpdateBossStatsDisplay();
			FinishTurn(lockedOutcome);
			return;
		}

		SetTurnState(GameState.TurnState.Rolling);
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
