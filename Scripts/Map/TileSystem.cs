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

	// 蓄力：下一回合攻击力总数 ×300%（其余升级数值见 Scripts/Tiles/Upgrades/）。
	private const float ChargeMultiplier = 3.0f;

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

	// 当前升级面板抽出的选项（加权抽样结果），保证按钮索引与升级枚举一一对应。
	private MapTileData.TileUpgrade[] currentUpgradeOptions;

	private TileTooltip tooltip;

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

		SetupTileHover();

		BuildTileUI();
		BuildUpgradePanel();
		RefreshAllTileUI();
		SetPromptStates();
	}

	// =========================================================
	// 地块悬浮提示
	// =========================================================

	private void SetupTileHover()
	{
		tooltip = new TileTooltip();
		tooltip.Initialize(chineseFont);
		AddChild(tooltip);

		for (int i = 0; i < tiles.Count; i++)
		{
			Tile tile = tilePoints.GetNodeOrNull<Tile>($"Tile{i}");

			if (tile == null)
			{
				GD.PrintErr($"TileSystem：找不到 Tile{i}，悬浮提示未接入");
				continue;
			}

			int tileIndex = tile.TileIndex;

			if (tileIndex < 0 || tileIndex >= tiles.Count)
				continue;

			tile.TileData = tiles[tileIndex];
			tile.Hovered += OnTileHovered;
			tile.Unhovered += OnTileUnhovered;
		}
	}

	private void OnTileHovered(int tileIndex)
	{
		if (tileIndex < 0 || tileIndex >= tiles.Count)
			return;

		// 暂停 / 地块升级面板 / Boss 升级状态打开时，不显示悬浮。
		if (GetTree().Paused)
			return;

		if (upgradePanel != null && upgradePanel.Visible)
			return;

		if (GameState.Instance.CurrentTurnState == GameState.TurnState.Upgrading)
			return;

		Node2D tileNode = tilePoints.GetNodeOrNull<Node2D>($"Tile{tileIndex}");

		if (tileNode == null)
			return;

		tooltip.ShowFor(tiles[tileIndex], tileNode.GlobalPosition);
	}

	private void OnTileUnhovered(int tileIndex)
	{
		if (tooltip != null)
			tooltip.Hide();
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

		// 消费上一回合的蓄力：本回合选红骰 → 攻击 ×3.0；选蓝骰 → 作废
		if (state.ChargeActive)
		{
			if (state.DiceColor == "red")
			{
				state.AttackMultiplier = ChargeMultiplier;
				GD.Print("蓄力生效：本回合攻击 ×300%");
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

		TileUpgradeEffect effect = TileUpgradeRegistry.Get(tile.UpgradeChoice);

		if (effect == null)
			return;

		effect.OnLanded(BuildEffectContext(tile, diceMatches));
	}

	private static TileEffectContext BuildEffectContext(MapTileData tile, bool diceMatches)
	{
		GameState state = GameState.Instance;

		return new TileEffectContext
		{
			Tile = tile,
			DiceMatched = diceMatches,
			DiceColor = state.DiceColor,
			RedValue = state.Dice["red"],
			BlueValue = state.Dice["blue"],
			// 第一批没有"选骰时判定"的效果，这里取落点值即可；
			// 条件型到第二批再改为真正的"选骰时快照"。
			HpAtSelection = state.PlayerHp,
			MaxHpAtSelection = state.PlayerMaxHp
		};
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

			Node2D marker = tilePoints.GetNode<Node2D>($"Tile{i}");
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

		// 按权重抽 3 个不重复选项（第一批各色池不足 3 个时返回全部）。
		TileUpgradeEffect[] picks = TileUpgradeRegistry.DrawWeighted(tile.Color, 3);

		currentUpgradeOptions = new MapTileData.TileUpgrade[picks.Length];
		(string, string)[] options = new (string, string)[picks.Length];

		for (int i = 0; i < picks.Length; i++)
		{
			currentUpgradeOptions[i] = picks[i].Id;
			options[i] = (picks[i].DisplayName, picks[i].Description);
		}

		upgradeTitleLabel.Text = $"选择{colorName}地块升级";
		upgradeDescLabel.Text = $"{tileIndex + 1} 号地块 · 效果永久生效";

		ConfigureOption(upgradeOptionA, options, 0);
		ConfigureOption(upgradeOptionB, options, 1);
		ConfigureOption(upgradeOptionC, options, 2);

		tooltip?.Hide();
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

		if (currentUpgradeOptions == null
			|| optionIndex < 0
			|| optionIndex >= currentUpgradeOptions.Length)
			return;

		int index = upgradingTileIndex;
		MapTileData tile = tiles[index];

		tile.UpgradeChoice = currentUpgradeOptions[optionIndex];

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

	// 地块类型 → 升级选项由 TileUpgradeRegistry 提供
	//（各效果类自带 DisplayName / Description / Weight）。


	// Map 每次切换回合状态时调用，用于刷新"升级"按钮可点击状态。
	public void OnTurnStateChanged(GameState.TurnState newState)
	{
		// 非待机状态（战斗/升级/Boss 升级面板等）时收起悬浮，避免残留。
		if (newState != GameState.TurnState.ReadyToRoll)
			tooltip?.Hide();

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
