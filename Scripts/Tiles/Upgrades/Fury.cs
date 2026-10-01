using Godot;

// 狂怒：踩到该地块（骰色匹配）时，若选骰时生命低于 50%，攻击 +10。
public sealed class Fury : TileUpgradeEffect
{
	private const int BonusAttack = 10;
	private const float LowHpThreshold = 0.5f;

	public override MapTileData.TileUpgrade Id => MapTileData.TileUpgrade.Fury;
	public override MapTileData.TileColor Color => MapTileData.TileColor.Red;
	public override string DisplayName => "狂怒";
	public override string Description => $"若生命低于 {LowHpThreshold * 100f:0}%，攻击 +{BonusAttack}";
	public override int Weight => 2;

	public override void OnLanded(TileEffectContext ctx)
	{
		if (!ctx.DiceMatched)
			return;

		if (HpRatio() < LowHpThreshold)
		{
			GameState.Instance.TempAtk += BonusAttack;
			GD.Print($"狂怒：生命 {ctx.HpAtSelection}/{ctx.MaxHpAtSelection} 低于 50%，攻击 +{BonusAttack}");
		}
	}
}
