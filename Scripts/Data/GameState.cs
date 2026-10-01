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

	// 幸运：积攒的"免费行动"次数（下一次玩家行动时怪物不反击）
	public int FreeActionsPending;
	// 幸运：本回合战斗怪物不反击（玩家行动开始时由 FreeActionsPending 提升而来）
	public bool NoCounterThisBattle;

	// 反击：本回合敌人打在护盾上的伤害反弹给敌人
	public bool ReflectShieldDamage;
	// 破甲：本回合无视怪物护盾
	public bool IgnoreEnemyShield;

	// 每次地块结算前调用，清掉上一回合遗留的战斗修正。
	public void ResetTileTurnModifiers()
	{
		FlatDamageBonus = 0;
		SkipPlayerAttack = false;
		AttackMultiplier = 1f;
		ReflectShieldDamage = false;
		IgnoreEnemyShield = false;
	}

	public int PlayerPosition { get; set; }
	public TurnState CurrentTurnState { get; set; } = TurnState.ReadyToRoll;
	public string DiceColor { get; set; } = "red";

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
		PlayerMaxHp = 30;
		PlayerHp = PlayerMaxHp;
		PlayerAtk = 3;
		PlayerDef = 1;
		PlayerShield = 0;
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
		PlayerPosition = 0;
		CurrentTurnState = TurnState.ReadyToRoll;
		DiceColor = "red";
		Dice["red"] = 0;
		Dice["blue"] = 0;
	}

	public void ResetTempStats()
	{
		TempAtk = 0;
		TempDef = 0;
	}
}
