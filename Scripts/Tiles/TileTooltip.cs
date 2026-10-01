using Godot;

// 地块悬浮说明面板：自定义深色描边面板 + 中文 Label。
// 由 TileSystem 在 Initialize 时代码创建（与现有升级面板的生成方式一致）。
// 尺寸用字体度量手动计算（不依赖容器首帧布局），位置贴在地块上方并做屏幕边界钳制；
// 暂停时自动隐藏。
public partial class TileTooltip : Panel
{
	private const float MaxWidth = 420f;
	private const float PaddingX = 14f;
	private const float PaddingY = 10f;
	private const int FontSize = 20;

	private Font font;
	private Label label;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
	}

	// 暂停时 Area2D 不再派发 MouseExited，悬浮面板可能卡住不消失，
	// 这里用 Always 处理模式自检：树暂停就隐藏。
	public override void _Process(double delta)
	{
		if (Visible && GetTree() != null && GetTree().Paused)
			Hide();
	}

	// 必须在 AddChild 前调用；chineseFont 由 TileSystem._Ready 加载后传入。
	public void Initialize(Font chineseFont)
	{
		font = chineseFont;

		MouseFilter = MouseFilterEnum.Ignore;
		ZIndex = 90;
		Visible = false;

		StyleBoxFlat style = new()
		{
			BgColor = new Color(0.025f, 0.03f, 0.045f, 0.96f),
			BorderColor = new Color(1f, 0.85f, 0.45f, 0.85f),
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
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = MouseFilterEnum.Ignore
		};

		label.AddThemeFontOverride("font", font);
		label.AddThemeFontSizeOverride("font_size", FontSize);
		label.AddThemeColorOverride("font_color", new Color(0.95f, 0.95f, 0.95f, 1f));

		AddChild(label);
	}

	public void ShowFor(MapTileData tile, Vector2 anchorGlobalPosition)
	{
		if (label == null || tile == null)
			return;

		string text = TileDescription.Build(tile);
		label.Text = text;

		float textHeight = MeasureTextHeight(text);
		Vector2 size = new(MaxWidth + PaddingX * 2f, textHeight + PaddingY * 2f);

		label.Size = new Vector2(MaxWidth, textHeight);
		Size = size;

		// 贴在地块上方，居中并夹在屏幕内。
		Vector2 position = anchorGlobalPosition + new Vector2(-size.X * 0.5f, -size.Y - 30f);

		Rect2 viewport = GetViewportRect();
		position.X = Mathf.Clamp(position.X, 8f, viewport.Size.X - size.X - 8f);
		position.Y = Mathf.Clamp(position.Y, 8f, viewport.Size.Y - size.Y - 8f);

		GlobalPosition = position;
		Show();
	}

	// 按 '\n' 分行，估算每行在 MaxWidth 下换行后的行数，累加高度。
	// 中文按字换行，用整行宽度除以可用宽度向上取整即可，够精确。
	private float MeasureTextHeight(string text)
	{
		if (font == null)
			return FontSize + PaddingY;

		float lineHeight = font.GetHeight(FontSize);
		float height = 0f;

		foreach (string rawLine in text.Split('\n'))
		{
			if (rawLine.Length == 0)
			{
				height += lineHeight * 0.6f;
				continue;
			}

			float width = font.GetStringSize(
				rawLine,
				HorizontalAlignment.Left,
				-1,
				FontSize
			).X;

			int wrapped = Mathf.Max(1, Mathf.CeilToInt(width / MaxWidth));
			height += wrapped * lineHeight;
		}

		return height;
	}
}
