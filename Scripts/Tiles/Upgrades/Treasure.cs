using Godot;

// 宝箱：踩中该地块即随机 3 个其它地块升级进度 +1（由 TileSystem 统一执行）。
public sealed class Treasure : TileUpgradeEffect
{
	private const int ProgressGainCount = 3;

	public override MapTileData.TileUpgrade Id => MapTileData.TileUpgrade.Treasure;
	public override MapTileData.TileColor Color => MapTileData.TileColor.White;
	public override string DisplayName => "宝箱";
	public override string Description => $"随机 {ProgressGainCount} 个地块升级进度 +1";
	public override int Weight => 2;

	public override void OnLanded(TileEffectContext ctx)
	{
		ctx.RandomProgressGain = ProgressGainCount;
		GD.Print($"宝箱：随机 {ProgressGainCount} 个地块进度 +1");
	}
}
