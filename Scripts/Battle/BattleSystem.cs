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
		MonsterAction action = enemy.GetCurrentAction();
		int basePlayerAttack = Mathf.Max(state.PlayerAtk + state.TempAtk, 0);
		int playerAttack = Mathf.Max(
			Mathf.RoundToInt(basePlayerAttack * state.AttackMultiplier),
			0
		);
		int playerDefense = Mathf.Max(state.PlayerDef + state.TempDef, 0);
		int effectiveEnemyDefense = Mathf.Max(enemy.Def + action.DamageReduction, 0);

		GD.Print(
			$"{enemy.DisplayName} 当前行动：{action.Name}（{action.Description}）"
		);

		if (!state.SkipPlayerAttack)
		{
			int damageToEnemy =
				Mathf.Max(playerAttack - effectiveEnemyDefense, 0)
				+ Mathf.Max(state.FlatDamageBonus, 0);
			GD.Print(
				$"玩家攻击：ATK {playerAttack} - {enemy.DisplayName} DEF {enemy.Def}" +
				$" - 行动减伤 {action.DamageReduction} = {damageToEnemy} 伤害"
			);

			await player.PlayAttackAnimation();
			int actualDamageToEnemy = enemy.TakeDamage(damageToEnemy);
			await enemy.PlayHitAnimation();

			if (action.ReflectTrueDamage > 0 && actualDamageToEnemy > 0)
			{
				int reflectedDamage = player.TakeTrueDamage(action.ReflectTrueDamage);
				GD.Print(
					$"{action.Name}：反弹 {reflectedDamage} 点真实伤害，" +
					$"玩家 HP={state.PlayerHp}"
				);
				await player.PlayHitAnimation();

				// 反伤可以在玩家击杀怪物的同一瞬间击败玩家；玩家失败优先结算。
				if (state.PlayerHp <= 0)
				{
					GD.Print("玩家被反伤击败");
					return BattleOutcome.PlayerDefeated;
				}
			}

			if (enemy.IsDead())
			{
				await enemy.PlayDeathAnimation();
				GD.Print($"{enemy.DisplayName} 被击败");
				return BattleOutcome.EnemyDefeated;
			}
		}
		else
		{
			GD.Print("地块蓄力：本回合玩家不攻击");
		}

		// 幸运：本回合怪物跳过主动行动，但行动循环仍推进一格。
		bool freeAction = state.NoCounterThisBattle;
		state.NoCounterThisBattle = false;
		if (freeAction)
		{
			GD.Print("幸运免费行动：怪物本回合跳过行动");
			if (action.Damage > 0)
				enemy.ConsumeNextAttackMultiplier();

			enemy.AdvanceAction();
			MonsterAction skippedNextAction = enemy.GetCurrentAction();
			GD.Print(
				$"回合战斗结束：玩家 HP={state.PlayerHp}，" +
				$"{enemy.DisplayName} HP={enemy.CurrentHp}；下一行动={skippedNextAction.Name}"
			);
			return BattleOutcome.Continue;
		}

		if (action.Damage > 0)
		{
			int attackMultiplier = enemy.ConsumeNextAttackMultiplier();
			int rawDamage = action.Damage * attackMultiplier;
			int damageAfterDefense = Mathf.Max(rawDamage - playerDefense, 0);

			GD.Print(
				$"{enemy.DisplayName} 使用 {action.Name}：" +
				$"{action.Damage} x {attackMultiplier} - 玩家 DEF {playerDefense}" +
				$" = {damageAfterDefense} 点待结算伤害"
			);

			await enemy.PlayAttackAnimation();
			int hpDamage = player.TakeDamage(damageAfterDefense);
			await player.PlayHitAnimation();

			GD.Print(
				$"玩家实际损失 {hpDamage} 点生命，" +
				$"HP={state.PlayerHp}，护盾={state.PlayerShield}"
			);
		}

		if (action.Heal > 0)
		{
			int healAmount = action.Heal;
			if (action.DoubleHealWhenPlayerLow && state.PlayerHp < 10)
				healAmount *= 2;

			int actualHeal = enemy.Heal(healAmount);
			GD.Print(
				$"{enemy.DisplayName} 使用 {action.Name}：恢复 {actualHeal} 点生命，" +
				$"HP={enemy.CurrentHp}/{enemy.MaxHp}"
			);
		}

		if (action.ChargeMultiplier > 1)
		{
			enemy.SetNextAttackMultiplier(action.ChargeMultiplier);
			GD.Print(
				$"{enemy.DisplayName} 使用 {action.Name}：下一次攻击 x{action.ChargeMultiplier}"
			);
		}

		if (action.LockPlayerNextTurn)
		{
			state.SkipMovementNextTurn = true;
			GD.Print(
				$"{enemy.DisplayName} 使用 {action.Name}：玩家下一回合只能原地战斗"
			);
		}

		if (state.PlayerHp <= 0)
		{
			GD.Print("玩家被击败");
			return BattleOutcome.PlayerDefeated;
		}

		enemy.AdvanceAction();
		MonsterAction nextAction = enemy.GetCurrentAction();
		GD.Print(
			$"回合战斗结束：玩家 HP={state.PlayerHp}，" +
			$"{enemy.DisplayName} HP={enemy.CurrentHp}；下一行动={nextAction.Name}"
		);
		return BattleOutcome.Continue;
	}
}
