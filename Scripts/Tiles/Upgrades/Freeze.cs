using Godot;

// 冰封：踩到该地块（骰色匹配）时，当前怪物攻击永久 -2（可叠加，只影响当前 Boss）。
public sealed class Freeze : TileUpgradeEffect
{
	private const int DebuffAmount = 2;

	public override MapTileData.TileUpgrade Id => MapTileData.TileUpgrade.Freeze;
	public override MapTileData.TileColor Color => MapTileData.TileColor.Blue;
	public override string DisplayName => "冰封";
	public override string Description => $"当前怪物攻击永久 -{DebuffAmount}";
	public override int Weight => 2;

	public override void OnLanded(TileEffectContext ctx)
	{
		if (!ctx.DiceMatched)
			return;

		GameState state = GameState.Instance;
		state.EnemyDamageDebuff += DebuffAmount;
		GD.Print($"冰封：当前怪物攻击 -{DebuffAmount}（累计 -{state.EnemyDamageDebuff}）");
	}
}
