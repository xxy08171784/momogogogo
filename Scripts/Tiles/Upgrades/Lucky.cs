using Godot;

// 幸运：踩中该地块即获得一次"免费行动"（下一次玩家行动时怪物不反击）。
// 当前实现沿用旧版效果（非新版策划的"二次投掷"），按用户确认保持不变。
public sealed class Lucky : TileUpgradeEffect
{
	public override MapTileData.TileUpgrade Id => MapTileData.TileUpgrade.Lucky;
	public override MapTileData.TileColor Color => MapTileData.TileColor.White;
	public override string DisplayName => "幸运";
	public override string Description => "获得一次免费行动（下次玩家行动时怪物不反击）";
	public override int Weight => 3;

	public override void OnLanded(TileEffectContext ctx)
	{
		GameState.Instance.FreeActionsPending += 1;
		GD.Print("幸运：获得一次免费行动（下次怪物不反击）");
	}
}
