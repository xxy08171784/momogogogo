using Godot;

// 回响：踩到该地块（骰色匹配）时，若蓝骰点数 ≥5，防御 +10。
public sealed class Echo : TileUpgradeEffect
{
	private const int BonusDefense = 10;
	private const int HighBlueThreshold = 5;

	public override MapTileData.TileUpgrade Id => MapTileData.TileUpgrade.Echo;
	public override MapTileData.TileColor Color => MapTileData.TileColor.Blue;
	public override string DisplayName => "回响";
	public override string Description => $"若蓝骰≥{HighBlueThreshold}，防御 +{BonusDefense}";
	public override int Weight => 2;

	public override void OnLanded(TileEffectContext ctx)
	{
		if (!ctx.DiceMatched)
			return;

		if (ctx.BlueValue >= HighBlueThreshold)
		{
			GameState.Instance.TempDef += BonusDefense;
			GD.Print($"回响：蓝骰 {ctx.BlueValue}≥5，防御 +{BonusDefense}");
		}
	}
}
