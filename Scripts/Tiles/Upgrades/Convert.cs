using Godot;

// 转化：作为黑格的升级选项；真正的"变色 + 直接升级"在
// TileSystem.OnUpgradeChosen 选择该选项的瞬间执行，故 OnLanded 无操作。
public sealed class Convert : TileUpgradeEffect
{
	public override MapTileData.TileUpgrade Id => MapTileData.TileUpgrade.Convert;
	public override MapTileData.TileColor Color => MapTileData.TileColor.Black;
	public override string DisplayName => "转化";
	public override string Description => "升级后该黑色地块变为随机颜色地块，并直接获得一次该颜色的升级";
	public override int Weight => 1;

	public override void OnLanded(TileEffectContext ctx) { }
}
