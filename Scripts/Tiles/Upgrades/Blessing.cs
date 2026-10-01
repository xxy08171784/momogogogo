using Godot;

// 祝福：踩中该地块即本回合攻击、防御各 +5。
public sealed class Blessing : TileUpgradeEffect
{
	private const int BonusAmount = 5;

	public override MapTileData.TileUpgrade Id => MapTileData.TileUpgrade.Blessing;
	public override MapTileData.TileColor Color => MapTileData.TileColor.White;
	public override string DisplayName => "祝福";
	public override string Description => $"本回合攻击、防御各 +{BonusAmount}";
	public override int Weight => 2;

	public override void OnLanded(TileEffectContext ctx)
	{
		GameState.Instance.TempAtk += BonusAmount;
		GameState.Instance.TempDef += BonusAmount;
		GD.Print($"祝福：本回合攻防各 +{BonusAmount}");
	}
}
