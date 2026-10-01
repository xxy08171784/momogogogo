using Godot;

// 共鸣：踩到该地块（骰色匹配）时，若红蓝骰点数相同，攻击 +20。
public sealed class Resonance : TileUpgradeEffect
{
	private const int BonusAttack = 20;

	public override MapTileData.TileUpgrade Id => MapTileData.TileUpgrade.Resonance;
	public override MapTileData.TileColor Color => MapTileData.TileColor.Red;
	public override string DisplayName => "共鸣";
	public override string Description => $"若红蓝骰点数相同，攻击 +{BonusAttack}";
	public override int Weight => 2;

	public override void OnLanded(TileEffectContext ctx)
	{
		if (!ctx.DiceMatched)
			return;

		if (ctx.RedValue == ctx.BlueValue)
		{
			GameState.Instance.TempAtk += BonusAttack;
			GD.Print($"共鸣：红蓝骰同为 {ctx.RedValue}，攻击 +{BonusAttack}");
		}
	}
}
