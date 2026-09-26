using Godot;

[GlobalClass]
public partial class MapTileData : Resource
{
	public enum TileColor
	{
		White,
		Black,
		Red,
		Blue
	}

	public const int MaxUpgrade = 2;

	[Export]
	public TileColor Color { get; set; } = TileColor.White;

	[Export]
	public int Level { get; set; }

	[Export]
	public int Value { get; set; }
}
