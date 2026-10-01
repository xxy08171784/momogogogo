using Godot;

// 诅咒：踩中该地块即本回合攻击 +5，但怪物攻击 +3。
public sealed class Curse : TileUpgradeEffect
{
	private const int BonusAttack = 5;
	private const int EnemyBoost = 3;

	public override MapTileData.TileUpgrade Id => MapTileData.TileUpgrade.Curse;
	public override MapTileData.TileColor Color => MapTileData.TileColor.Black;
	public override string DisplayName => "诅咒";
	public override string Description => $"本回合攻击 +{BonusAttack}，但怪物攻击 +{EnemyBoost}";
	public override int Weight => 2;

	public override void OnLanded(TileEffectContext ctx)
	{
		GameState state = GameState.Instance;
		state.TempAtk += BonusAttack;
		state.EnemyDamageBoostThisTurn += EnemyBoost;
		GD.Print($"诅咒：本回合攻击 +{BonusAttack}，但怪物攻击 +{EnemyBoost}");
	}
}
