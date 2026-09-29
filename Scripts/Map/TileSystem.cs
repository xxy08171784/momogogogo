using Godot;
using System;
using System.Collections.Generic;

// 地块系统：踩踏计数 + 手动升级 + 地块效果结算。
// 由 Map 在 _Ready 里代码创建，避免改动 map.tscn。
// 所有 UI（进度条、升级按钮、升级面板）均代码生成。
public partial class TileSystem : Node2D
{
	// =========================
	// 数值配置（每轮升级保持一致）
	// =========================

	private const int ArmorBreakBonusAtk = 2;    // 破甲：攻击 +2
	private const int BastionBonusDef = 2;       // 坚守：防御 +2
	private const int CounterFlatDamage = 3;     // 反击：伤害 +3
	private const int HealingAmount = 4;         // 治疗：回血 +4
	private const float ConvertHealRatio = 0.2f; // 嗜血/守护：20%
	private const float ChargeMultiplier = 1.2f; // 蓄力：下回合攻击 ×120%

	// =========================
	// UI 布局常量
	// =========================

	private const float SegmentWidth = 10f;
	private const float SegmentHeight = 12f;
	private const float SegmentGap = 4f;
	private const float BarOffsetX = -22f;
	private const float BarOffsetY = 62f;

	private static readonly Color SegmentEmptyColor = new(0.25f, 0.25f, 0.25f, 0.85f);

	// =========================
	// 运行时引用
	// =========================

	private List<MapTileData> tiles;
	private Node2D tilePoints;
	private Button rollButton;
	private Font chineseFont;

	private readonly List<ColorRect[]> tileProgressSegments = new();
	private readonly List<Button> tileUpgradePrompts = new();
	private int upgradingTileIndex = -1;
	private int pendingAutoUpgradeTile = -1;

	private ColorRect upgradePanel;
	private Label upgradeTitleLabel;
	private Label upgradeDescLabel;
	private Button upgradeOptionA;
	private Button upgradeOptionB;
	private Button upgradeOptionC;

	public override void _Ready()
	{
		chineseFont = GD.Load<Font>("res://Style/ChineseUIFont.tres");
	}

	// Map 在读档之后调用，此时 tiles 已是最新状态。
	public void Initialize(List<MapTileData> mapTiles, Node2D pointsNode, Button rollBtn)
	{
		tiles = mapTiles;
		tilePoints = pointsNode;
		rollButton = rollBtn;

		BuildTileUI();
		BuildUpgradePanel();
		RefreshAllTileUI();
		SetPromptStates();
	}

	// =========================================================
	// 核心：踩中地块的结算（取代原 Map.ResolveTileEffect）
	// =========================================================

	public void ResolveLandedTile()
	{
		GameState state = GameState.Instance;

		// 清掉上一回合遗留的战斗修正
		state.ResetTileTurnModifiers();

		int tileIndex = state.PlayerPosition;
		MapTileData tile = tiles[tileIndex];

		// 消费上一回合的蓄力：本回合选红骰 → 攻击 ×1.2；选蓝骰 → 作废
		if (state.ChargeActive)
		{
			if (state.DiceColor == "red")
			{
				state.AttackMultiplier = ChargeMultiplier;
				GD.Print("蓄力生效：本回合攻击 ×120%");
			}
			else
			{
				GD.Print("蓄力失效：本回合选择了蓝骰");
			}

			state.ChargeActive = false;
		}

		bool diceMatches = tile.Color switch
		{
			MapTileData.TileColor.Red => state.DiceColor == "red",
			MapTileData.TileColor.Blue => state.DiceColor == "blue",
			_ => false
		};

		// 基础效果（沿用策划案更新前的行为）
		ApplyBaseEffect(tile, diceMatches);

		// 升级效果（叠加在基础效果之上）
		ApplyUpgradeEffect(tile, diceMatches);

		// 踩踏计数：黑格无升级，不计数
		if (tile.Color != MapTileData.TileColor.Black
			&& tile.UpgradeChoice == MapTileData.TileUpgrade.None)
		{
			tile.HitCount = Mathf.Min(tile.HitCount + 1, MapTileData.UpgradeThreshold);

			if (tile.HitCount >= MapTileData.UpgradeThreshold)
			{
				// 进度满：本回合结束后自动弹出升级面板
				pendingAutoUpgradeTile = tileIndex;
				GD.Print($"地块 {tileIndex + 1} 踩满 {MapTileData.UpgradeThreshold} 次，准备升级");
			}
		}

		RefreshTileUI(tileIndex);
	}

	// =========================
	// 基础效果
	// =========================

	private static void ApplyBaseEffect(MapTileData tile, bool diceMatches)
	{
		GameState state = GameState.Instance;

		switch (tile.Color)
		{
			case MapTileData.TileColor.White:
				ApplyWhiteBaseEffect(tile);
				break;

			case MapTileData.TileColor.Red:
				if (diceMatches)
					state.TempAtk += tile.Value;
				break;

			case MapTileData.TileColor.Blue:
				if (diceMatches)
					state.TempDef += tile.Value;
				break;
		}
	}

	// 白格：首次踩中激活；之后回血，满血转护盾（沿用原逻辑）。
	private static void ApplyWhiteBaseEffect(MapTileData tile)
	{
		GameState state = GameState.Instance;

		if (tile.Level == 0)
		{
			tile.Level = 1;
			tile.Value = 1;
			GD.Print("首次踩中空白格：地块已激活为 1 级，本次不获得回血或护盾");
			return;
		}

		if (state.PlayerHp < state.PlayerMaxHp)
		{
			int missing = state.PlayerMaxHp - state.PlayerHp;
			int heal = Mathf.Min(tile.Value, missing);
			state.PlayerHp += heal;
			GD.Print($"踩中空白格：回复 {heal} 点生命（地块当前回血值 {tile.Value}）");
			return;
		}

		if (state.PlayerShield < GameState.MaxShield)
		{
			int gain = Mathf.Min(tile.Value, GameState.MaxShield - state.PlayerShield);
			state.PlayerShield += gain;
			GD.Print($"满血踩中空白格：获得 {gain} 点护盾，当前护盾 {state.PlayerShield}/{GameState.MaxShield}");
			return;
		}

		GD.Print("满血且护盾已满：空白格本次对玩家无效果");
	}

	// =========================
	// 升级效果
	// =========================

	private static void ApplyUpgradeEffect(MapTileData tile, bool diceMatches)
	{
		if (tile.UpgradeChoice == MapTileData.TileUpgrade.None)
			return;

		GameState state = GameState.Instance;

		switch (tile.UpgradeChoice)
		{
			// ---- 红格 ----
			case MapTileData.TileUpgrade.ArmorBreak:
				if (diceMatches)
				{
					state.TempAtk += ArmorBreakBonusAtk;
					GD.Print($"破甲：本回合攻击 +{ArmorBreakBonusAtk}");
				}
				break;

			case MapTileData.TileUpgrade.Bloodthirst:
				if (diceMatches)
				{
					int totalAtk = TotalAttack();
					int heal = Mathf.RoundToInt(totalAtk * ConvertHealRatio);
					HealPlayer(heal);
					GD.Print($"嗜血：本回合攻击 {totalAtk} 的 20% → 回复 {heal}");
				}
				break;

			case MapTileData.TileUpgrade.Charge:
				if (diceMatches)
				{
					state.SkipPlayerAttack = true;
					state.ChargeActive = true;
					GD.Print("蓄力：本回合不攻击，下一回合攻击 ×120%");
				}
				break;

			// ---- 蓝格 ----
			case MapTileData.TileUpgrade.Bastion:
				if (diceMatches)
				{
					state.TempDef += BastionBonusDef;
					GD.Print($"坚守：本回合防御 +{BastionBonusDef}");
				}
				break;

			case MapTileData.TileUpgrade.Counter:
				if (diceMatches)
				{
					state.FlatDamageBonus += CounterFlatDamage;
					GD.Print($"反击：本回合伤害 +{CounterFlatDamage}");
				}
				break;

			case MapTileData.TileUpgrade.Ward:
				if (diceMatches)
				{
					int totalDef = TotalDefense();
					int heal = Mathf.RoundToInt(totalDef * ConvertHealRatio);
					HealPlayer(heal);
					GD.Print($"守护：本回合防御 {totalDef} 的 20% → 回复 {heal}");
				}
				break;

			// ---- 白格 ----
			case MapTileData.TileUpgrade.Healing:
				HealPlayer(HealingAmount);
				GD.Print($"治疗：回复 {HealingAmount} 点生命");
				break;

			case MapTileData.TileUpgrade.Lucky:
				// 幸运：本回合结算后，玩家可再行动一次；
				// 下一次玩家行动时怪物不反击（免费行动）。
				state.FreeActionsPending += 1;
				GD.Print("幸运：踩中白格，获得一次免费行动（下次怪物不反击）");
				break;
		}
	}

	private static int TotalAttack()
	{
		GameState state = GameState.Instance;
		float total = (state.PlayerAtk + state.TempAtk) * state.AttackMultiplier;
		return Mathf.RoundToInt(total);
	}

	private static int TotalDefense()
	{
		GameState state = GameState.Instance;
		return state.PlayerDef + state.TempDef;
	}

	private static void HealPlayer(int amount)
	{
		GameState state = GameState.Instance;

		if (amount <= 0 || state.PlayerHp >= state.PlayerMaxHp)
			return;

		int healed = Mathf.Min(amount, state.PlayerMaxHp - state.PlayerHp);
		state.PlayerHp += healed;
		GD.Print($"回血 +{healed}，当前 HP {state.PlayerHp}/{state.PlayerMaxHp}");
	}

	// =========================================================
	// UI：进度条 / 升级按钮
	// =========================================================

	private void BuildTileUI()
	{
		for (int i = 0; i < tiles.Count; i++)
		{
			if (tiles[i].Color == MapTileData.TileColor.Black)
			{
				tileProgressSegments.Add(null);
				tileUpgradePrompts.Add(null);
				continue;
			}

			Marker2D marker = tilePoints.GetNode<Marker2D>($"Marker2D{i}");
			Vector2 origin = marker.Position + new Vector2(BarOffsetX, BarOffsetY);

			// 5 小格进度条
			ColorRect[] segments = new ColorRect[MapTileData.UpgradeThreshold];

			for (int s = 0; s < segments.Length; s++)
			{
				ColorRect segment = new()
				{
					Name = $"TileProgress{i}_Seg{s}",
					Position = origin + new Vector2(s * (SegmentWidth + SegmentGap), 0),
					Size = new Vector2(SegmentWidth, SegmentHeight),
					Color = SegmentEmptyColor,
					MouseFilter = Control.MouseFilterEnum.Ignore,
					ZIndex = 20
				};

				AddChild(segment);
				segments[s] = segment;
			}

			tileProgressSegments.Add(segments);

			// "升级"按钮（默认隐藏）
			int tileIndex = i;
			Button prompt = new()
			{
				Name = $"TileUpgradePrompt{i}",
				Text = "升级",
				Position = origin + new Vector2(-8f, SegmentHeight + 6f),
				Size = new Vector2(76f, 44f),
				ZIndex = 30,
				Visible = false
			};

			prompt.AddThemeFontOverride("font", chineseFont);
			prompt.AddThemeFontSizeOverride("font_size", 20);
			prompt.Pressed += () => OnPromptPressed(tileIndex);

			AddChild(prompt);
			tileUpgradePrompts.Add(prompt);
		}
	}

	private void RefreshAllTileUI()
	{
		for (int i = 0; i < tiles.Count; i++)
			RefreshTileUI(i);
	}

	private void RefreshTileUI(int tileIndex)
	{
		if (tiles == null || tileIndex < 0 || tileIndex >= tiles.Count)
			return;

		MapTileData tile = tiles[tileIndex];

		ColorRect[] segments = tileProgressSegments[tileIndex];
		if (segments != null)
		{
			for (int s = 0; s < segments.Length; s++)
			{
				if (segments[s] == null)
					continue;

				segments[s].Color = s < tile.HitCount
					? ProgressFillColor(tile.Color)
					: SegmentEmptyColor;
			}
		}

		// 提示按钮与状态刷新
		SetPromptStates();
	}

	private void SetPromptStates()
	{
		if (tileUpgradePrompts.Count == 0)
			return;

		bool idle = GameState.Instance.CurrentTurnState == GameState.TurnState.ReadyToRoll;
		bool panelOpen = upgradePanel != null && upgradePanel.Visible;

		for (int i = 0; i < tiles.Count; i++)
		{
			Button prompt = tileUpgradePrompts[i];

			if (prompt == null)
				continue;

			MapTileData tile = tiles[i];
			bool ready = tile.Color != MapTileData.TileColor.Black
				&& tile.HitCount >= MapTileData.UpgradeThreshold
				&& tile.UpgradeChoice == MapTileData.TileUpgrade.None;

			prompt.Visible = ready;
			prompt.Disabled = !idle || panelOpen;
		}
	}

	private static Color ProgressFillColor(MapTileData.TileColor color)
	{
		return color switch
		{
			MapTileData.TileColor.Red => new Color(0.9f, 0.25f, 0.25f),
			MapTileData.TileColor.Blue => new Color(0.25f, 0.5f, 0.95f),
			MapTileData.TileColor.White => new Color(0.95f, 0.9f, 0.7f),
			_ => new Color(0.6f, 0.6f, 0.6f)
		};
	}

	// =========================================================
	// UI：升级选择面板
	// =========================================================

	private void BuildUpgradePanel()
	{
		upgradePanel = new ColorRect
		{
			Name = "TileUpgradePanel",
			Visible = false,
			Position = new Vector2(610f, 300f),
			Size = new Vector2(700f, 400f),
			Color = new Color(0.02f, 0.03f, 0.045f, 0.96f),
			MouseFilter = Control.MouseFilterEnum.Stop,
			ZIndex = 100
		};
		AddChild(upgradePanel);

		upgradeTitleLabel = MakeLabel(
			"TileUpgradeTitle", "地块升级", 30,
			new Vector2(30f, 22f), new Vector2(640f, 50f),
			new Color(1f, 0.9f, 0.45f));
		upgradePanel.AddChild(upgradeTitleLabel);

		upgradeDescLabel = MakeLabel(
			"TileUpgradeDesc", $"踩满 {MapTileData.UpgradeThreshold} 次，选择一项永久升级", 20,
			new Vector2(30f, 80f), new Vector2(640f, 40f),
			new Color(0.9f, 0.9f, 0.9f));
		upgradePanel.AddChild(upgradeDescLabel);

		upgradeOptionA = MakeButton(
			"TileUpgradeOptionA", "A",
			new Vector2(55f, 140f), new Vector2(180f, 150f),
			() => OnUpgradeChosen(0));
		upgradePanel.AddChild(upgradeOptionA);

		upgradeOptionB = MakeButton(
			"TileUpgradeOptionB", "B",
			new Vector2(255f, 140f), new Vector2(180f, 150f),
			() => OnUpgradeChosen(1));
		upgradePanel.AddChild(upgradeOptionB);

		upgradeOptionC = MakeButton(
			"TileUpgradeOptionC", "C",
			new Vector2(455f, 140f), new Vector2(180f, 150f),
			() => OnUpgradeChosen(2));
		upgradePanel.AddChild(upgradeOptionC);

		Button cancel = MakeButton(
			"TileUpgradeCancel", "取消",
			new Vector2(255f, 310f), new Vector2(180f, 50f),
			OnUpgradeCancelPressed);
		upgradePanel.AddChild(cancel);
	}

	private Label MakeLabel(
		string name, string text, int fontSize,
		Vector2 position, Vector2 size, Color color)
	{
		Label label = new()
		{
			Name = name,
			Text = text,
			Position = position,
			Size = size,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};

		label.AddThemeFontOverride("font", chineseFont);
		label.AddThemeFontSizeOverride("font_size", fontSize);
		label.AddThemeColorOverride("font_color", color);
		return label;
	}

	private Button MakeButton(
		string name, string text,
		Vector2 position, Vector2 size,
		Action onClick)
	{
		Button button = new()
		{
			Name = name,
			Text = text,
			Position = position,
			Size = size
		};

		button.AddThemeFontOverride("font", chineseFont);
		button.AddThemeFontSizeOverride("font_size", 20);
		button.Pressed += onClick;
		return button;
	}

	private void OnPromptPressed(int tileIndex)
	{
		if (upgradePanel.Visible)
			return;

		if (GameState.Instance.CurrentTurnState != GameState.TurnState.ReadyToRoll)
			return;

		upgradingTileIndex = tileIndex;
		ShowUpgradePanel(tileIndex);
	}

	private void ShowUpgradePanel(int tileIndex)
	{
		MapTileData tile = tiles[tileIndex];
		string colorName = tile.Color switch
		{
			MapTileData.TileColor.Red => "红色",
			MapTileData.TileColor.Blue => "蓝色",
			_ => "白色"
		};

		upgradeTitleLabel.Text = $"选择{colorName}地块升级";
		upgradeDescLabel.Text = $"{tileIndex + 1} 号地块 · 效果永久生效";

		(string shortText, string tooltip)[] options = GetUpgradeOptions(tile.Color);

		ConfigureOption(upgradeOptionA, options, 0);
		ConfigureOption(upgradeOptionB, options, 1);
		ConfigureOption(upgradeOptionC, options, 2);

		upgradePanel.Visible = true;
		rollButton.Disabled = true;
		SetPromptStates();
	}

	private static void ConfigureOption(
		Button button, (string, string)[] options, int index)
	{
		if (index >= options.Length)
		{
			button.Visible = false;
			return;
		}

		button.Text = options[index].Item1;
		button.TooltipText = options[index].Item2;
		button.Visible = true;
	}

	private void OnUpgradeChosen(int optionIndex)
	{
		if (upgradingTileIndex < 0)
			return;

		int index = upgradingTileIndex;
		MapTileData tile = tiles[index];
		MapTileData.TileUpgrade[] upgrades = GetUpgradeChoices(tile.Color);

		if (optionIndex < 0 || optionIndex >= upgrades.Length)
			return;

		tile.UpgradeChoice = upgrades[optionIndex];

		CloseUpgradePanel();
		RefreshTileUI(index);

		GD.Print($"地块 {index + 1} 升级为：{tile.UpgradeChoice}");
	}

	private void OnUpgradeCancelPressed()
	{
		CloseUpgradePanel();
	}

	private void CloseUpgradePanel()
	{
		upgradePanel.Visible = false;
		rollButton.Disabled = false;
		upgradingTileIndex = -1;
		SetPromptStates();
	}

	// =========================
	// 地块类型 → 升级选项
	// =========================

	private static MapTileData.TileUpgrade[] GetUpgradeChoices(MapTileData.TileColor color)
	{
		return color switch
		{
			MapTileData.TileColor.Red => new[]
			{
				MapTileData.TileUpgrade.ArmorBreak,
				MapTileData.TileUpgrade.Bloodthirst,
				MapTileData.TileUpgrade.Charge
			},
			MapTileData.TileColor.Blue => new[]
			{
				MapTileData.TileUpgrade.Bastion,
				MapTileData.TileUpgrade.Counter,
				MapTileData.TileUpgrade.Ward
			},
			MapTileData.TileColor.White => new[]
			{
				MapTileData.TileUpgrade.Healing,
				MapTileData.TileUpgrade.Lucky
			},
			_ => Array.Empty<MapTileData.TileUpgrade>()
		};
	}

	private static (string, string)[] GetUpgradeOptions(MapTileData.TileColor color)
	{
		return color switch
		{
			MapTileData.TileColor.Red => new[]
			{
				("破甲：攻击+2", "以后踩到该地块，攻击力 +2"),
				("嗜血：攻击20%回血", "以后踩到该地块，本回合攻击力 20% 转为回复"),
				("蓄力：下回合攻击×120%", "以后踩到该地块，本回合不攻击，下一回合攻击 ×120%（选蓝骰失效）")
			},
			MapTileData.TileColor.Blue => new[]
			{
				("坚守：防御+2", "以后踩到该地块，防御力 +2"),
				("反击：伤害+3", "以后踩到该地块，伤害 +3"),
				("守护：防御20%回血", "以后踩到该地块，本回合防御力 20% 转为回复")
			},
			MapTileData.TileColor.White => new[]
			{
				("治疗：回血+4", "以后踩到该地块，回血 +4"),
				("幸运：额外再掷一次", "以后踩到该地块，获得二次投掷骰子的机会")
			},
			_ => Array.Empty<(string, string)>()
		};
	}

	// Map 每次切换回合状态时调用，用于刷新"升级"按钮可点击状态。
	public void OnTurnStateChanged(GameState.TurnState newState)
	{
		SetPromptStates();
		TryAutoOpenUpgrade(newState);
	}

	// 踩满进度后，等回合结束（回到 ReadyToRoll）自动弹出升级面板。
	private void TryAutoOpenUpgrade(GameState.TurnState newState)
	{
		if (newState != GameState.TurnState.ReadyToRoll)
			return;

		if (upgradePanel == null || upgradePanel.Visible)
			return;

		int tileIndex = pendingAutoUpgradeTile;
		if (tileIndex < 0 || tileIndex >= tiles.Count)
			return;

		MapTileData tile = tiles[tileIndex];

		if (tile.UpgradeChoice != MapTileData.TileUpgrade.None)
		{
			pendingAutoUpgradeTile = -1;
			return;
		}

		pendingAutoUpgradeTile = -1;
		upgradingTileIndex = tileIndex;
		ShowUpgradePanel(tileIndex);
	}
}
