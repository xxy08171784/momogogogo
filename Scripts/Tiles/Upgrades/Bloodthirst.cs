using Godot;

// 嗜血：踩到该地块（骰色匹配）时，本回合攻击力的 30% 转为回复。
public sealed class Bloodthirst : TileUpgradeEffect
{
	private const float HealRatio = 0.3f;

	public override MapTileData.TileUpgrade Id => MapTileData.TileUpgrade.Bloodthirst;
	public override MapTileData.TileColor Color => MapTileData.TileColor.Red;
	public override string DisplayName => "嗜血";
	public override string Description => $"本回合攻击力的 {HealRatio * 100f:0}% 转为回复";
	public override int Weight => 3;

	public override void OnLanded(TileEffectContext ctx)
	{
		if (!ctx.DiceMatched)
			return;

		int totalAtk = TotalAttack();
		int heal = Mathf.RoundToInt(totalAtk * HealRatio);
		HealPlayer(heal);
		GD.Print($"嗜血：本回合攻击 {totalAtk} 的 30% → 回复 {heal}");
	}
}
