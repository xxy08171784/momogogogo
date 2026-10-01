using Godot;

// 坚守：踩到该地块（骰色匹配）时，本回合防御 +5。
public sealed class Bastion : TileUpgradeEffect
{
	private const int BonusDefense = 5;

	public override MapTileData.TileUpgrade Id => MapTileData.TileUpgrade.Bastion;
	public override MapTileData.TileColor Color => MapTileData.TileColor.Blue;
	public override string DisplayName => "坚守";
	public override string Description => $"踩中该地块时防御 +{BonusDefense}";
	public override int Weight => 3;

	public override void OnLanded(TileEffectContext ctx)
	{
		if (!ctx.DiceMatched)
			return;

		GameState.Instance.TempDef += BonusDefense;
		GD.Print($"坚守：本回合防御 +{BonusDefense}");
	}
}
