using Godot;

// 蓄力：踩到该地块（骰色匹配）时，本回合不攻击，下一回合攻击力总数 ×300%；
// 若下一回合选择蓝色骰子则失效（由 TileSystem 在回合结算时判定并消费）。
public sealed class Charge : TileUpgradeEffect
{
	public override MapTileData.TileUpgrade Id => MapTileData.TileUpgrade.Charge;
	public override MapTileData.TileColor Color => MapTileData.TileColor.Red;
	public override string DisplayName => "蓄力";
	public override string Description => "本回合不攻击，下一回合攻击力总数 ×300%（若下回合选蓝骰则失效）";
	public override int Weight => 3;

	public override void OnLanded(TileEffectContext ctx)
	{
		if (!ctx.DiceMatched)
			return;

		GameState state = GameState.Instance;
		state.SkipPlayerAttack = true;
		state.ChargeActive = true;
		GD.Print("蓄力：本回合不攻击，下一回合攻击 ×300%（选蓝骰失效）");
	}
}
