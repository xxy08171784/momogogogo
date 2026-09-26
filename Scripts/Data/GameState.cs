using Godot;
using System.Collections.Generic;

public partial class GameState : Node
{
	public static GameState Instance { get; private set; }

	public const int TileCount = 12;

	public enum TurnState
	{
		ReadyToRoll,
		Rolling,
		WaitingForDiceSelection,
		Moving,
		Resolving
	}

	// 角色当前面板
	public int PlayerLevel { get; set; }
	public int PlayerHp { get; set; }
	public int PlayerAtk { get; set; }
	public int PlayerDef { get; set; }

	// 临时属性：用于地块等临时效果，不直接修改基础面板
	public int TempAtk { get; set; }
	public int TempDef { get; set; }

	// 角色所处位置
	public int PlayerPosition { get; set; }

	// 当前回合所处阶段
	public TurnState CurrentTurnState { get; set; } = TurnState.ReadyToRoll;

	// 本回合玩家选择的骰子颜色
	public string DiceColor { get; set; } = "red";

	// 红蓝骰本回合的点数
	public Dictionary<string, int> Dice { get; } = new()
	{
		["red"] = 0,
		["blue"] = 0
	};

	public override void _Ready()
	{
		Instance = this;

		PlayerLevel = 1;
		PlayerHp = 30;
		PlayerAtk = 3;
		PlayerDef = 1;
		TempAtk = 0;
		TempDef = 0;
		PlayerPosition = 0;
		CurrentTurnState = TurnState.ReadyToRoll;
	}

	public void ResetTempStats()
	{
		TempAtk = 0;
		TempDef = 0;
	}
}
