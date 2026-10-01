using Godot;
using System.Collections.Generic;

public partial class GameState : Node
{
	public static GameState Instance { get; private set; }

	public const int TileCount = 12;
	public const int MaxShield = 3;

	public enum TurnState
	{
		ReadyToRoll,
		Rolling,
		WaitingForDiceSelection,
		Moving,
		ResolvingTile,
		Battling,
		Upgrading,
		GameOver
	}

	public int PlayerLevel { get; set; }
	public int PlayerMaxHp { get; set; }
	public int PlayerHp { get; set; }
	public int PlayerAtk { get; set; }
	public int PlayerDef { get; set; }
	public int PlayerShield { get; set; }
	public int PlayerTurnShield { get; set; }

	// 角色升级带来的永久被动。
	public int RedDiceAtkBonus { get; set; }
	public int BlueDiceDefBonus { get; set; }
	public int DoubleDiceResonanceBonus { get; set; }
	public int LifestealHealAmount { get; set; }
	public int TurnStartShieldAmount { get; set; }
	public bool LifestealUsedThisTurn { get; set; }

	// 角色升级各自的选取次数（key = CharacterUpgradeType 枚举名），供"已获得升级"面板显示。
	public Dictionary<string, int> UpgradeCounts { get; } = new();

	// 黑化领域：只施加一次，之后永久影响本局。
	public bool BlackDomainApplied { get; set; }
	public float DefenseEffectMultiplier { get; set; } = 1f;
	public float HealingEffectMultiplier { get; set; } = 1f;

	public int TotalPlayerShield => PlayerShield + PlayerTurnShield;

	// 仅在本回合生效，回合结束后清零。
	public int TempAtk { get; set; }
	public int TempDef { get; set; }

	// ========================================================
	// 地块 → 战斗 共享接口（战斗系统只读，勿改结构）
	// 由 TileSystem 在每次地块结算时写入，BattleSystem 读取。
	// ========================================================

	// 反击：对敌人额外伤害（+3）
	public int FlatDamageBonus;
	// 蓄力：本回合玩家不出手，怪物仍会反击
	public bool SkipPlayerAttack;
	// 蓄力：本回合攻击 ×1.2（默认 1.0）
	public float AttackMultiplier = 1f;

	// 已蓄力：下一回合选红骰生效，选蓝骰作废
	public bool ChargeActive;

	// 幸运：积攒的"免费行动"次数（下一次玩家行动时怪物跳过主动行动）
	public int FreeActionsPending;
	// 幸运：本回合怪物跳过主动行动（玩家行动开始时由 FreeActionsPending 提升而来）
	public bool NoCounterThisBattle;

	// 反击：本回合敌人打在护盾上的伤害反弹给敌人
	public bool ReflectShieldDamage;
	// 破甲：本回合无视怪物护盾
	public bool IgnoreEnemyShield;

	// ========================================================
	// 第二批：条件判定 / 指引重掷 / 怪物伤害修正
	// ========================================================

	// 选骰时的 HP 快照（狂怒/暗影等条件型按"选骰时"判定）。
	public int HpAtSelection;
	public int MaxHpAtSelection;

	// 指引：下一次投掷可重掷一个骰子（选骰时消耗）。
	public bool RerollAvailable;

	// 冰封：怪物攻击永久减（只影响当前 Boss，换 Boss 时清零）。
	public int EnemyDamageDebuff;
	// 诅咒：本回合怪物攻击加成。
	public int EnemyDamageBoostThisTurn;
	// 荆棘：本回合怪物命中玩家时反弹的固定伤害。
	public int ThornsReflectDamage;

	// 每次地块结算前调用，清掉上一回合遗留的战斗修正。
	public void ResetTileTurnModifiers()
	{
		FlatDamageBonus = 0;
		SkipPlayerAttack = false;
		AttackMultiplier = 1f;
		ReflectShieldDamage = false;
		IgnoreEnemyShield = false;
		EnemyDamageBoostThisTurn = 0;
		ThornsReflectDamage = 0;
	}

	public int PlayerPosition { get; set; }
	public TurnState CurrentTurnState { get; set; } = TurnState.ReadyToRoll;
	public string DiceColor { get; set; } = "red";
	public bool SkipMovementNextTurn { get; set; }

	public Dictionary<string, int> Dice { get; } = new()
	{
		["red"] = 0,
		["blue"] = 0
	};

	public override void _Ready()
	{
		Instance = this;
		ResetToDefaults();
	}

	public void ResetToDefaults()
	{
		PlayerLevel = 1;
		PlayerMaxHp = 33;
		PlayerHp = PlayerMaxHp;
		PlayerAtk = 2;
		PlayerDef = 0;
		PlayerShield = 0;
		PlayerTurnShield = 0;
		RedDiceAtkBonus = 0;
		BlueDiceDefBonus = 0;
		DoubleDiceResonanceBonus = 0;
		LifestealHealAmount = 0;
		TurnStartShieldAmount = 0;
		LifestealUsedThisTurn = false;
		UpgradeCounts.Clear();
		BlackDomainApplied = false;
		DefenseEffectMultiplier = 1f;
		HealingEffectMultiplier = 1f;
		TempAtk = 0;
		TempDef = 0;
		FlatDamageBonus = 0;
		SkipPlayerAttack = false;
		AttackMultiplier = 1f;
		ChargeActive = false;
		FreeActionsPending = 0;
		NoCounterThisBattle = false;
		ReflectShieldDamage = false;
		IgnoreEnemyShield = false;
		HpAtSelection = 0;
		MaxHpAtSelection = 0;
		RerollAvailable = false;
		EnemyDamageDebuff = 0;
		EnemyDamageBoostThisTurn = 0;
		ThornsReflectDamage = 0;
		PlayerPosition = 0;
		CurrentTurnState = TurnState.ReadyToRoll;
		DiceColor = "red";
		Dice["red"] = 0;
		Dice["blue"] = 0;
		SkipMovementNextTurn = false;
	}

	public void BeginTurn()
	{
		PlayerTurnShield = Mathf.Max(TurnStartShieldAmount, 0);
		LifestealUsedThisTurn = false;
	}

	public void EndTurn()
	{
		PlayerTurnShield = 0;
		LifestealUsedThisTurn = false;
	}

	public int GetEffectiveDefense()
	{
		int rawDefense = Mathf.Max(PlayerDef + TempDef, 0);
		return Mathf.Max(
			Mathf.FloorToInt(rawDefense * Mathf.Clamp(DefenseEffectMultiplier, 0f, 1f)),
			0
		);
	}

	public int HealPlayer(int amount)
	{
		if (amount <= 0 || PlayerHp >= PlayerMaxHp)
			return 0;

		int adjustedAmount = Mathf.Max(
			Mathf.FloorToInt(amount * Mathf.Clamp(HealingEffectMultiplier, 0f, 1f)),
			0
		);
		int healed = Mathf.Min(adjustedAmount, PlayerMaxHp - PlayerHp);
		PlayerHp += healed;
		return healed;
	}

	public void ApplyBlackDomain()
	{
		if (BlackDomainApplied)
			return;

		BlackDomainApplied = true;
		DefenseEffectMultiplier = 0.5f;
		HealingEffectMultiplier = 0.5f;
	}

	public void ResetTempStats()
	{
		TempAtk = 0;
		TempDef = 0;
	}
}
