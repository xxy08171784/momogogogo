using Godot;

// 暗影：踩中该地块时，若选骰时生命低于 25%，攻击、防御各 +15。
public sealed class Shadow : TileUpgradeEffect
{
	private const int BonusAmount = 15;
	private const float LowHpThreshold = 0.25f;

	public override MapTileData.TileUpgrade Id => MapTileData.TileUpgrade.Shadow;
	public override MapTileData.TileColor Color => MapTileData.TileColor.Black;
	public override string DisplayName => "暗影";
	public override string Description => $"若生命低于 {LowHpThreshold * 100f:0}%，攻击、防御各 +{BonusAmount}";
	public override int Weight => 2;

	public override void OnLanded(TileEffectContext ctx)
	{
		if (HpRatio() < LowHpThreshold)
		{
			GameState state = GameState.Instance;
			state.TempAtk += BonusAmount;
			state.TempDef += BonusAmount;
			GD.Print($"暗影：生命 {ctx.HpAtSelection}/{ctx.MaxHpAtSelection} 低于 25%，攻防各 +{BonusAmount}");
		}
	}
}
