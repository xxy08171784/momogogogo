using Godot;

// 星辉：踩中该地块即生命上限 +2 并回复 2（上限永久增加，可叠加）。
public sealed class Starlight : TileUpgradeEffect
{
	private const int MaxHpGain = 2;
	private const int HealAmount = 2;

	public override MapTileData.TileUpgrade Id => MapTileData.TileUpgrade.Starlight;
	public override MapTileData.TileColor Color => MapTileData.TileColor.White;
	public override string DisplayName => "星辉";
	public override string Description => $"生命上限 +{MaxHpGain} 并回复 {HealAmount}";
	public override int Weight => 1;

	public override void OnLanded(TileEffectContext ctx)
	{
		GameState.Instance.PlayerMaxHp += MaxHpGain;
		HealPlayer(HealAmount);
		GD.Print($"星辉：生命上限 +{MaxHpGain}，回复 {HealAmount}");
	}
}
