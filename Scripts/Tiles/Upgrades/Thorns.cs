using Godot;

// 荆棘：踩到该地块（骰色匹配）时，本回合怪物使用伤害行动命中玩家时反弹 10 点伤害。
public sealed class Thorns : TileUpgradeEffect
{
	private const int ReflectDamage = 10;

	public override MapTileData.TileUpgrade Id => MapTileData.TileUpgrade.Thorns;
	public override MapTileData.TileColor Color => MapTileData.TileColor.Blue;
	public override string DisplayName => "荆棘";
	public override string Description => $"受到怪物反击时反弹 {ReflectDamage} 点伤害";
	public override int Weight => 2;

	public override void OnLanded(TileEffectContext ctx)
	{
		if (!ctx.DiceMatched)
			return;

		GameState.Instance.ThornsReflectDamage = ReflectDamage;
		GD.Print($"荆棘：本回合怪物攻击玩家时反弹 {ReflectDamage} 点伤害");
	}
}
