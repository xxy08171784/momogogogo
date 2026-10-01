using Godot;

// 献祭：踩中该地块即失去 3 血，并随机 3 个其它地块升级进度 +1。
public sealed class Sacrifice : TileUpgradeEffect
{
	private const int SelfDamage = 3;
	private const int ProgressGainCount = 3;

	public override MapTileData.TileUpgrade Id => MapTileData.TileUpgrade.Sacrifice;
	public override MapTileData.TileColor Color => MapTileData.TileColor.Black;
	public override string DisplayName => "献祭";
	public override string Description => $"失去 {SelfDamage} 血，随机 {ProgressGainCount} 个地块进度 +1";
	public override int Weight => 1;

	public override void OnLanded(TileEffectContext ctx)
	{
		GameState state = GameState.Instance;
		DamagePlayer(SelfDamage);
		ctx.RandomProgressGain = ProgressGainCount;
		GD.Print($"献祭：失去 {SelfDamage} 血，随机 {ProgressGainCount} 个地块进度 +1，HP={state.PlayerHp}");
	}
}
