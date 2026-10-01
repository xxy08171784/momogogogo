using Godot;

// 地块升级效果基类。每个升级写一个子类，注册进 TileUpgradeRegistry。
// 无状态单例：只读写 GameState，不持有任何回合数据。
public abstract class TileUpgradeEffect
{
	// 稳定存档 ID：只用 MapTileData.TileUpgrade 枚举，末尾追加，绝不复用旧值。
	public abstract MapTileData.TileUpgrade Id { get; }

	// 属于哪一色池（红/蓝/白/黑）。
	public abstract MapTileData.TileColor Color { get; }

	public abstract string DisplayName { get; }
	public abstract string Description { get; }

	// 加权抽样用的相对权重（同色池内）。
	public abstract int Weight { get; }

	// =========================
	// 挂载点（默认空实现，子类按需重写）
	// =========================

	// 踩中该地块时结算（绝大多数效果在此生效）。
	public virtual void OnLanded(TileEffectContext ctx) { }

	// 选骰时（指引重掷等；条件型判定在第二批使用）。
	public virtual void OnDiceSelected(TileEffectContext ctx) { }

	// 战斗数值结算（后续批次）。
	public virtual void ModifyCombatStats(TileEffectContext ctx) { }

	// 玩家出手造成伤害后（后续批次）。
	public virtual void OnPlayerDealtDamage(TileEffectContext ctx, int damageDealt) { }

	// 敌人反击命中玩家后（后续批次）。
	public virtual void OnEnemyDealtDamage(TileEffectContext ctx, int absorbedByShield) { }

	// =========================
	// 共享工具
	// =========================

	protected static void HealPlayer(int amount)
	{
		GameState state = GameState.Instance;

		if (amount <= 0 || state.PlayerHp >= state.PlayerMaxHp)
			return;

		int healed = Mathf.Min(amount, state.PlayerMaxHp - state.PlayerHp);
		state.PlayerHp += healed;
		GD.Print($"回血 +{healed}，当前 HP {state.PlayerHp}/{state.PlayerMaxHp}");
	}

	// 本回合总攻击力（含蓄力倍率）。
	protected static int TotalAttack()
	{
		GameState state = GameState.Instance;
		return Mathf.RoundToInt((state.PlayerAtk + state.TempAtk) * state.AttackMultiplier);
	}

	// 本回合总防御力。
	protected static int TotalDefense()
	{
		GameState state = GameState.Instance;
		return state.PlayerDef + state.TempDef;
	}
}

// 一次"地块结算"的上下文。第一批只用到 Tile/DiceMatched 与骰子数值；
// HpAtSelection 目前取落点时的值，条件型到第二批才改成真正的"选骰时快照"。
public sealed class TileEffectContext
{
	public MapTileData Tile;
	public bool DiceMatched;
	public string DiceColor;
	public int RedValue;
	public int BlueValue;
	public int HpAtSelection;
	public int MaxHpAtSelection;
}
