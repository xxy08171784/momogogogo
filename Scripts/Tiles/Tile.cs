using Godot;

// 单个地块对象：一个可被鼠标悬浮的 Area2D，同时充当该格的坐标锚点。
// Player 用它的 GlobalPosition 做逐格移动，TileSystem 用它定位进度条与悬浮提示。
public partial class Tile : Area2D
{
	[Signal]
	public delegate void HoveredEventHandler(int tileIndex);

	[Signal]
	public delegate void UnhoveredEventHandler(int tileIndex);

	// 与 map.tscn 里 TilePoints/Tile{n} 对应的格号（0 起）。
	[Export]
	public int TileIndex { get; set; }

	// 由 TileSystem 在初始化时注入，指向 allTiles[TileIndex]。
	// 存档读档替换的是同一个 MapTileData 实例，所以这里始终是最新数据。
	public MapTileData TileData { get; set; }

	public override void _Ready()
	{
		InputPickable = true;

		MouseEntered += OnMouseEntered;
		MouseExited += OnMouseExited;
	}

	private void OnMouseEntered()
	{
		EmitSignal(SignalName.Hovered, TileIndex);
	}

	private void OnMouseExited()
	{
		EmitSignal(SignalName.Unhovered, TileIndex);
	}
}
