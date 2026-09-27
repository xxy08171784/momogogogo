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
