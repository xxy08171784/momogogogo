using Godot;

// 黑市：踩中该地块即失去 2 血，本回合攻击 +5。
public sealed class BlackMarket : TileUpgradeEffect
{
	private const int SelfDamage = 2;
	private const int BonusAttack = 5;

	public override MapTileData.TileUpgrade Id => MapTileData.TileUpgrade.BlackMarket;
	public override MapTileData.TileColor Color => MapTileData.TileColor.Black;
	public override string DisplayName => "黑市";
	public override string Description => $"失去 {SelfDamage} 血，本回合攻击 +{BonusAttack}";
	public override int Weight => 2;

	public override void OnLanded(TileEffectContext ctx)
	{
		GameState state = GameState.Instance;
		DamagePlayer(SelfDamage);
		state.TempAtk += BonusAttack;
		GD.Print($"黑市：失去 {SelfDamage} 血，本回合攻击 +{BonusAttack}，HP={state.PlayerHp}");
	}
}
