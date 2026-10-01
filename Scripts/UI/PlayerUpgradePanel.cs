using Godot;
using System.Collections.Generic;

// 左上角常驻面板：显示玩家已获得的"角色升级"（各选了几次、累计效果）。
// 地块升级仍在地块悬浮提示上看，不在这里。
// 由 Map 代码创建（不改 map.tscn），字体自行加载。
public partial class PlayerUpgradePanel : Panel
{
	private const float PanelWidth = 240f;
	private const float PaddingX = 14f;
	private const float PaddingY = 10f;
	private const int FontSize = 18;
	private const float LineHeightScale = 1.2f;

	// 固定顺序；count<=0 的不显示。key = CharacterUpgradeType 枚举名。
	private static readonly (string Key, string Label)[] Order =
	{
		("Attack", "攻击强化"),
		("Defense", "防御强化"),
		("MaxHp", "生命强化"),
		("RedDice", "红骰磨砺"),
		("BlueDice", "蓝骰磨砺"),
		("DoubleDiceResonance", "双骰共鸣"),
		("Lifesteal", "嗜血"),
		("TurnShield", "护盾"),
	};

	private Font font;
	private Label label;

	public override void _Ready()
	{
		ZIndex = 5;
		Visible = false;
	}

	// 必须在 AddChild 前调用。
	public void Initialize(Font chineseFont)
	{
		font = chineseFont;

		MouseFilter = MouseFilterEnum.Ignore;

		StyleBoxFlat style = new()
		{
			BgColor = new Color(0.03f, 0.04f, 0.06f, 0.85f),
			BorderColor = new Color(0.6f, 0.7f, 0.9f, 0.6f),
			BorderWidthLeft = 2,
			BorderWidthTop = 2,
			BorderWidthRight = 2,
			BorderWidthBottom = 2,
			CornerRadiusTopLeft = 6,
			CornerRadiusTopRight = 6,
			CornerRadiusBottomLeft = 6,
			CornerRadiusBottomRight = 6
		};

		AddThemeStyleboxOverride("panel", style);

		label = new Label
		{
			Position = new Vector2(PaddingX, PaddingY),
			MouseFilter = MouseFilterEnum.Ignore
		};

		label.AddThemeFontOverride("font", font);
		label.AddThemeFontSizeOverride("font_size", FontSize);
		label.AddThemeColorOverride("font_color", new Color(0.92f, 0.94f, 0.98f, 1f));

		AddChild(label);
	}

	public void Refresh()
	{
		if (label == null || GameState.Instance == null)
			return;

		GameState state = GameState.Instance;
		Dictionary<string, int> counts = state.UpgradeCounts;
		List<string> lines = new();

		foreach ((string key, string name) in Order)
		{
			counts.TryGetValue(key, out int count);

			if (count <= 0)
				continue;

			string line = $"{name} ×{count}";

			// 累计效果补充说明（直接取 GameState 的聚合值，避免重复硬编码）。
			if (key == "Lifesteal")
				line += $"（每回合回复 {state.LifestealHealAmount}）";
			else if (key == "TurnShield")
				line += $"（每回合护盾 {state.TurnStartShieldAmount}）";

			lines.Add(line);
		}

		if (lines.Count == 0)
			lines.Add("（暂无）");

		label.Text = "已获得升级\n" + string.Join("\n", lines);

		// 尺寸：宽固定，高按行数（标题 1 行 + 内容）。
		float lineHeight = (font?.GetHeight(FontSize) ?? FontSize) * LineHeightScale;
		int totalLines = lines.Count + 1;
		float bodyHeight = totalLines * lineHeight;

		label.Size = new Vector2(PanelWidth, bodyHeight);
		Size = new Vector2(PanelWidth + PaddingX * 2f, bodyHeight + PaddingY * 2f);
		Position = new Vector2(20f, 20f);
		Visible = true;
	}
}
