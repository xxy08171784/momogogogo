using Godot;
using System.Threading.Tasks;

public partial class BattleSystem : Node
{
	public enum BattleOutcome
	{
		Continue,
		PlayerDefeated,
		EnemyDefeated
	}

	private Player player;
	private Goblin enemy;

	public void Setup(Player battlePlayer, Goblin battleEnemy)
	{
		player = battlePlayer;
		enemy = battleEnemy;
	}

	public async Task<BattleOutcome> ResolveTurn()
	{
		GameState state = GameState.Instance;

		// 怪方挡值：防御 + 护盾（破甲时忽略护盾）。
		int enemyBlock =
			enemy.Def + (state.IgnoreEnemyShield ? 0 : enemy.Shield);

		// ===== 玩家出手（蓄力：本回合不出手）=====
		if (state.SkipPlayerAttack)
		{
			GD.Print("蓄力：本回合玩家不攻击");
		}
		else
		{
			int totalAttack =
				Mathf.RoundToInt(
					(state.PlayerAtk + state.TempAtk) * state.AttackMultiplier
				) + state.FlatDamageBonus;

			int damageToEnemy = Mathf.Max(totalAttack - enemyBlock, 0);
			GD.Print($"玩家攻击：总攻 {totalAttack} - {enemy.DisplayName} 防御 {enemyBlock} = {damageToEnemy} 伤害");

			await player.PlayAttackAnimation();
			enemy.TakeDamage(damageToEnemy);
			await enemy.PlayHitAnimation();

			if (enemy.IsDead())
			{
				await enemy.PlayDeathAnimation();
				GD.Print($"{enemy.DisplayName} 被击败");
				return BattleOutcome.EnemyDefeated;
			}
		}

		// ========================================================
		// 地块系统握手：幸运"免费行动" —— 本回合怪物不反击。
		// 若队友重写 ResolveTurn，请保留这段：读取并清除 NoCounterThisBattle。
		// ========================================================
		bool freeAction = state.NoCounterThisBattle;
		state.NoCounterThisBattle = false;

		if (freeAction)
		{
			GD.Print("幸运免费行动：怪物本回合不反击");
			GD.Print($"回合战斗结束：玩家 HP={state.PlayerHp}，{enemy.DisplayName} HP={enemy.CurrentHp}");
			return BattleOutcome.Continue;
		}

		// ===== 敌人反击 =====
		int playerDefense = Mathf.Max(state.PlayerDef + state.TempDef, 0);
		int damageToPlayer = Mathf.Max(enemy.Atk - playerDefense, 0);
		GD.Print($"{enemy.DisplayName} 反击：ATK {enemy.Atk} - 玩家 DEF {playerDefense} = {damageToPlayer} 伤害");

		await enemy.PlayAttackAnimation();
		player.TakeDamage(damageToPlayer, out int absorbedByShield);
		await player.PlayHitAnimation();

		if (state.PlayerHp <= 0)
		{
			GD.Print("玩家被击败");
			return BattleOutcome.PlayerDefeated;
		}

		// ===== 反击地块：护盾吸收的伤害反弹给敌人（走怪物防御，可击杀）=====
		if (state.ReflectShieldDamage)
		{
			state.ReflectShieldDamage = false;

			if (absorbedByShield > 0)
			{
				int reflectDamage = Mathf.Max(absorbedByShield - enemy.Def, 0);
				GD.Print($"反击：护盾吸收 {absorbedByShield} 点，反弹 {reflectDamage} 点给 {enemy.DisplayName}");

				if (reflectDamage > 0)
				{
					enemy.TakeDamage(reflectDamage);
					await enemy.PlayHitAnimation();

					if (enemy.IsDead())
					{
						await enemy.PlayDeathAnimation();
						GD.Print($"{enemy.DisplayName} 被反击击杀");
						return BattleOutcome.EnemyDefeated;
					}
				}
			}
		}

		GD.Print($"回合战斗结束：玩家 HP={state.PlayerHp}，{enemy.DisplayName} HP={enemy.CurrentHp}");
		return BattleOutcome.Continue;
	}
}
