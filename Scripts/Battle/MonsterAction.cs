// 单个怪物回合行为的数据描述。
// 一个行动可以同时拥有攻击、减伤、回血、反伤、蓄力或控制效果。
public sealed class MonsterAction
{
	public string Name { get; init; } = "";
	public string Description { get; init; } = "";

	// 本回合怪物主动攻击的基础伤害。
	public int Damage { get; init; }

	// 玩家本回合攻击该怪物时，额外减少的伤害。
	public int DamageReduction { get; init; }

	// 怪物在本回合行动阶段回复的生命。
	public int Heal { get; init; }

	// 怪物在行动阶段获得的护盾，持续到被消耗。
	public int ShieldGain { get; init; }

	// 玩家攻击命中后受到的真实反伤：无视防御和护盾。
	public int ReflectTrueDamage { get; init; }

	// 为下一次“造成伤害的怪物行动”设置攻击倍率。
	public int ChargeMultiplier { get; init; } = 1;

	// 让玩家下一回合跳过投骰、移动和地块结算，直接以基础攻防战斗。
	public bool LockPlayerNextTurn { get; init; }

	// 玩家当前 HP < 10 时，本行动的治疗量翻倍。
	public bool DoubleHealWhenPlayerLow { get; init; }

	// 永久提高怪物之后所有伤害。
	public int PowerGain { get; init; }

	// 仅 Boss 首回合使用：玩家防御与回血效果永久降低 50%。
	public bool ApplyBlackDomain { get; init; }
}
