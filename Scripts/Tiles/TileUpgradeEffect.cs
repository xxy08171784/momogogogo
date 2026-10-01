using Godot;
using System.Collections.Generic;

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
		int healed = state.HealPlayer(amount);

		if (healed > 0)
			GD.Print($"回血 +{healed}，当前 HP {state.PlayerHp}/{state.PlayerMaxHp}");
	}

	// 本回合总攻击力（含蓄力倍率）。
	protected static int TotalAttack()
	{
		GameState state = GameState.Instance;
		return Mathf.RoundToInt((state.PlayerAtk + state.TempAtk) * state.AttackMultiplier);
	}

	// 本回合有效防御力（含黑化领域等减益）。
	protected static int TotalDefense()
	{
		return GameState.Instance.GetEffectiveDefense();
	}

	// 地块直接扣血（不经过护盾/防御），返回实际扣掉的血量。
	protected static int DamagePlayer(int amount)
	{
		GameState state = GameState.Instance;
		int safe = Mathf.Max(amount, 0);
		int previous = state.PlayerHp;
		state.PlayerHp = Mathf.Max(previous - safe, 0);
		return previous - state.PlayerHp;
	}

	// 条件型用的生命比例：选骰时的 HP / 最大 HP。
	protected static float HpRatio()
	{
		GameState state = GameState.Instance;

		if (state.MaxHpAtSelection <= 0)
			return 1f;

		return (float)state.HpAtSelection / state.MaxHpAtSelection;
	}
}

// 一次"地块结算"的上下文。
// HpAtSelection/MaxHpAtSelection 由 Map 在选骰时快照写入，条件型据此判定。
public sealed class TileEffectContext
{
	public MapTileData Tile;
	public bool DiceMatched;
	public string DiceColor;
	public int RedValue;
	public int BlueValue;
	public int HpAtSelection;
	public int MaxHpAtSelection;

	// 供效果操作其它地块：全部地块列表、当前格号。
	public IList<MapTileData> AllTiles;
	public int SelfIndex;

	// 宝箱/献祭：声明"随机 N 个其它地块进度 +1"，由 TileSystem 统一执行。
	public int RandomProgressGain;
	// 若效果想精确指定某几格进度 +1，可直接往这里塞格号。
	public List<int> ProgressGain;
}
