using Godot;

// 治疗：踩中该地块即回复 4 点生命。
public sealed class Healing : TileUpgradeEffect
{
	private const int HealAmount = 4;

	public override MapTileData.TileUpgrade Id => MapTileData.TileUpgrade.Healing;
	public override MapTileData.TileColor Color => MapTileData.TileColor.White;
	public override string DisplayName => "治疗";
	public override string Description => $"踩中该地块时，回血 +{HealAmount}";
	public override int Weight => 3;

	public override void OnLanded(TileEffectContext ctx)
	{
		HealPlayer(HealAmount);
		GD.Print($"治疗：回复 {HealAmount} 点生命");
	}
}
