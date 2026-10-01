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

	// 每次地块结算前调用，清掉上一回合遗留的战斗修正。
	public void ResetTileTurnModifiers()
	{
		FlatDamageBonus = 0;
		SkipPlayerAttack = false;
		AttackMultiplier = 1f;
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
