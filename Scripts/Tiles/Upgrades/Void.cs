using Godot;

// 虚空：踩中该地块即攻击、防御各 +5，但受到 3 点伤害。
public sealed class Void : TileUpgradeEffect
{
	private const int BonusAmount = 5;
	private const int SelfDamage = 3;

	public override MapTileData.TileUpgrade Id => MapTileData.TileUpgrade.Void;
	public override MapTileData.TileColor Color => MapTileData.TileColor.Black;
	public override string DisplayName => "虚空";
	public override string Description => $"攻击、防御各 +{BonusAmount}，但受到 {SelfDamage} 点伤害";
	public override int Weight => 2;

	public override void OnLanded(TileEffectContext ctx)
	{
		GameState state = GameState.Instance;
		state.TempAtk += BonusAmount;
		state.TempDef += BonusAmount;
		DamagePlayer(SelfDamage);
		GD.Print($"虚空：攻防各 +{BonusAmount}，但受到 {SelfDamage} 点伤害，HP={state.PlayerHp}");
	}
}
