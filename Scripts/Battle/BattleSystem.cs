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
		int playerAttack = Mathf.Max(GameState.Instance.PlayerAtk + GameState.Instance.TempAtk, 0);
		int playerDefense = Mathf.Max(GameState.Instance.PlayerDef + GameState.Instance.TempDef, 0);

		int damageToEnemy = Mathf.Max(playerAttack - enemy.Def, 0);
		GD.Print($"玩家攻击：ATK {playerAttack} - {enemy.DisplayName} DEF {enemy.Def} = {damageToEnemy} 伤害");

		await player.PlayAttackAnimation();
		enemy.TakeDamage(damageToEnemy);
		await enemy.PlayHitAnimation();

		if (enemy.IsDead())
		{
			await enemy.PlayDeathAnimation();
			GD.Print($"{enemy.DisplayName} 被击败");
			return BattleOutcome.EnemyDefeated;
		}

		// ========================================================
		// 地块系统握手：幸运"免费行动" —— 本回合怪物不反击。
		// 若队友重写 ResolveTurn，请保留这段：读取并清除 NoCounterThisBattle。
		// ========================================================
		bool freeAction = GameState.Instance.NoCounterThisBattle;
		GameState.Instance.NoCounterThisBattle = false;

		if (freeAction)
		{
			GD.Print("幸运免费行动：怪物本回合不反击");
			GD.Print($"回合战斗结束：玩家 HP={GameState.Instance.PlayerHp}，{enemy.DisplayName} HP={enemy.CurrentHp}");
			return BattleOutcome.Continue;
		}

		int damageToPlayer = Mathf.Max(enemy.Atk - playerDefense, 0);
		GD.Print($"{enemy.DisplayName} 反击：ATK {enemy.Atk} - 玩家 DEF {playerDefense} = {damageToPlayer} 伤害");

		await enemy.PlayAttackAnimation();
		player.TakeDamage(damageToPlayer);
		await player.PlayHitAnimation();

		if (GameState.Instance.PlayerHp <= 0)
		{
			GD.Print("玩家被击败");
			return BattleOutcome.PlayerDefeated;
		}

		GD.Print($"回合战斗结束：玩家 HP={GameState.Instance.PlayerHp}，{enemy.DisplayName} HP={enemy.CurrentHp}");
		return BattleOutcome.Continue;
	}
}
