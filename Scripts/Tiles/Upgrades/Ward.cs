using Godot;

// 守护：踩到该地块（骰色匹配）时，本回合防御力的 30% 转为回复。
public sealed class Ward : TileUpgradeEffect
{
	private const float HealRatio = 0.3f;

	public override MapTileData.TileUpgrade Id => MapTileData.TileUpgrade.Ward;
	public override MapTileData.TileColor Color => MapTileData.TileColor.Blue;
	public override string DisplayName => "守护";
	public override string Description => $"本回合防御力的 {HealRatio * 100f:0}% 转为回复";
	public override int Weight => 3;

	public override void OnLanded(TileEffectContext ctx)
	{
		if (!ctx.DiceMatched)
			return;

		int totalDef = TotalDefense();
		int heal = Mathf.RoundToInt(totalDef * HealRatio);
		HealPlayer(heal);
		GD.Print($"守护：本回合防御 {totalDef} 的 30% → 回复 {heal}");
	}
}
