using Godot;

// 反击：踩到该地块（骰色匹配）时，本回合敌人打在你护盾上的伤害将反弹给敌人。
// 实际反射在 BattleSystem 里执行（读 GameState.ReflectShieldDamage）。
public sealed class Counter : TileUpgradeEffect
{
	public override MapTileData.TileUpgrade Id => MapTileData.TileUpgrade.Counter;
	public override MapTileData.TileColor Color => MapTileData.TileColor.Blue;
	public override string DisplayName => "反击";
	public override string Description => "本回合敌人打在你护盾上的伤害，将如数反弹给敌人";
	public override int Weight => 3;

	public override void OnLanded(TileEffectContext ctx)
	{
		if (!ctx.DiceMatched)
			return;

		GameState.Instance.ReflectShieldDamage = true;
		GD.Print("反击：本回合敌人打在护盾上的伤害将反弹");
	}
}
