using Godot;

// 灼热：踩到该地块（骰色匹配）时攻击 +3；若红骰点数 ≥5，再 +5。
public sealed class Scorch : TileUpgradeEffect
{
	private const int BonusAttack = 3;
	private const int ExtraAttack = 5;
	private const int HighRedThreshold = 5;

	public override MapTileData.TileUpgrade Id => MapTileData.TileUpgrade.Scorch;
	public override MapTileData.TileColor Color => MapTileData.TileColor.Red;
	public override string DisplayName => "灼热";
	public override string Description => $"攻击 +{BonusAttack}；若红骰≥{HighRedThreshold}，再 +{ExtraAttack}";
	public override int Weight => 2;

	public override void OnLanded(TileEffectContext ctx)
	{
		if (!ctx.DiceMatched)
			return;

		GameState.Instance.TempAtk += BonusAttack;

		if (ctx.RedValue >= HighRedThreshold)
		{
			GameState.Instance.TempAtk += ExtraAttack;
			GD.Print($"灼热：攻击 +{BonusAttack}（红骰 {ctx.RedValue}≥5，再 +{ExtraAttack}）");
		}
		else
		{
			GD.Print($"灼热：攻击 +{BonusAttack}");
		}
	}
}
