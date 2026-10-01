using System.Collections.Generic;

// 纯存档数据，不保存任何 Godot Node 引用。
public sealed class SaveGameData
{
	public int Version { get; set; } = 5;
	public long SavedAtUnixTime { get; set; }
	public PlayerSaveData Player { get; set; } = new();
	public DiceSaveData Dice { get; set; } = new();
	public BossSaveData Boss { get; set; } = new();
	public List<TileSaveData> Tiles { get; set; } = new();
}

public sealed class PlayerSaveData
{
	public int Level { get; set; }
	public int MaxHp { get; set; }
	public int Hp { get; set; }
	public int Atk { get; set; }
	public int Def { get; set; }
	public int Shield { get; set; }
	public int TempAtk { get; set; }
	public int TempDef { get; set; }
	public int Position { get; set; }
	public bool SkipMovementNextTurn { get; set; }
	public bool ChargeActive { get; set; }
	public int FreeActionsPending { get; set; }
	public int RedDiceAtkBonus { get; set; }
	public int BlueDiceDefBonus { get; set; }
	public int DoubleDiceResonanceBonus { get; set; }
	public int LifestealHealAmount { get; set; }
	public int TurnStartShieldAmount { get; set; }
	public bool BlackDomainApplied { get; set; }
}

public sealed class DiceSaveData
{
	public string SelectedColor { get; set; } = "red";
	public int RedValue { get; set; }
	public int BlueValue { get; set; }
}

public sealed class BossSaveData
{
	public int Index { get; set; }
	public int CurrentHp { get; set; }
	public int ActionIndex { get; set; }
	public int NextAttackMultiplier { get; set; } = 1;
	public int CurrentShield { get; set; }
	public int PowerBonus { get; set; }
	// 冰封：当前 Boss 的攻击削减，读档恢复。
	public int EnemyDamageDebuff { get; set; }
}

public sealed class TileSaveData
{
	public int Color { get; set; }
	public int Level { get; set; }
	public int Value { get; set; }
	// 新增（旧存档缺省为 0/None，自动兼容）
	public int HitCount { get; set; }
	public int UpgradeChoice { get; set; }
}
