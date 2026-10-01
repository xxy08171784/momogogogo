using Godot;

// 破甲：踩到该地块（骰色匹配）时，本回合攻击 +3，且无视怪物护盾。
public sealed class ArmorBreak : TileUpgradeEffect
{
	private const int BonusAttack = 3;

	public override MapTileData.TileUpgrade Id => MapTileData.TileUpgrade.ArmorBreak;
	public override MapTileData.TileColor Color => MapTileData.TileColor.Red;
	public override string DisplayName => "破甲";
	public override string Description => $"踩中该地块时攻击 +{BonusAttack}，并无视怪物护盾";
	public override int Weight => 3;

	public override void OnLanded(TileEffectContext ctx)
	{
		if (!ctx.DiceMatched)
			return;

		GameState.Instance.TempAtk += BonusAttack;
		GameState.Instance.IgnoreEnemyShield = true;
		GD.Print($"破甲：本回合攻击 +{BonusAttack}，无视怪物护盾");
	}
}
