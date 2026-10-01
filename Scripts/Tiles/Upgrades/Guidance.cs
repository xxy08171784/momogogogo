using Godot;

// 指引：踩中该地块即下一次投掷可重掷一个骰子（重掷选中的那颗，选骰时消耗）。
public sealed class Guidance : TileUpgradeEffect
{
	public override MapTileData.TileUpgrade Id => MapTileData.TileUpgrade.Guidance;
	public override MapTileData.TileColor Color => MapTileData.TileColor.White;
	public override string DisplayName => "指引";
	public override string Description => "下一次投掷可重掷一个骰子（重掷你选中的那颗）";
	public override int Weight => 2;

	public override void OnLanded(TileEffectContext ctx)
	{
		GameState.Instance.RerollAvailable = true;
		GD.Print("指引：下一次投掷可重掷一个骰子");
	}
}
