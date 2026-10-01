using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public partial class SaveManager : Node
{
	public const int CurrentSaveVersion = 5;

	// 全局访问点，其他脚本通过 SaveManager.Instance 调用
	public static SaveManager Instance { get; private set; }

	// 存档路径，放在 user:// 避免权限问题
	private const string SavePath = "user://save.json";
	private bool continueRequested;

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true
	};

	public override void _Ready()
	{
		// Autoload 启动时注册自己
		Instance = this;
	}

	// 判断是否存在存档
	public bool HasSave()
	{
		return FileAccess.FileExists(SavePath);
	}

	public void RequestContinue()
	{
		continueRequested = true;
	}

	public void StartNewGame()
	{
		ClearRun();
	}

	public void ClearRun()
	{
		continueRequested = false;
		DeleteSave();
		GameState.Instance?.ResetToDefaults();
	}

	public bool TryLoadRequestedGame(IList<MapTileData> tiles, out BossSaveData bossData)
	{
		bossData = null;
		if (!continueRequested)
			return false;

		continueRequested = false;
		return LoadGame(tiles, out bossData);
	}

	public bool SaveGame(
		IReadOnlyList<MapTileData> tiles,
		int bossIndex,
		int bossCurrentHp,
		int bossActionIndex,
		int bossNextAttackMultiplier,
		int bossCurrentShield,
		int bossPowerBonus)
	{
		if (GameState.Instance == null || bossIndex < 0 || bossCurrentHp <= 0)
			return false;

		try
		{
			GameState state = GameState.Instance;
			SaveGameData data = new()
			{
				Version = CurrentSaveVersion,
				SavedAtUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
				Player = new PlayerSaveData
				{
					Level = state.PlayerLevel,
					MaxHp = state.PlayerMaxHp,
					Hp = state.PlayerHp,
					Atk = state.PlayerAtk,
					Def = state.PlayerDef,
					Shield = state.PlayerShield,
					TempAtk = state.TempAtk,
					TempDef = state.TempDef,
					Position = state.PlayerPosition,
					SkipMovementNextTurn = state.SkipMovementNextTurn,
					ChargeActive = state.ChargeActive,
					FreeActionsPending = state.FreeActionsPending,
					RedDiceAtkBonus = state.RedDiceAtkBonus,
					BlueDiceDefBonus = state.BlueDiceDefBonus,
					DoubleDiceResonanceBonus = state.DoubleDiceResonanceBonus,
					LifestealHealAmount = state.LifestealHealAmount,
					TurnStartShieldAmount = state.TurnStartShieldAmount,
					BlackDomainApplied = state.BlackDomainApplied,
					UpgradeCounts = new Dictionary<string, int>(state.UpgradeCounts)
				},
				Dice = new DiceSaveData
				{
					SelectedColor = state.DiceColor,
					RedValue = state.Dice["red"],
					BlueValue = state.Dice["blue"]
				},
				Boss = new BossSaveData
				{
					Index = bossIndex,
					CurrentHp = bossCurrentHp,
					ActionIndex = bossActionIndex,
					NextAttackMultiplier = Math.Max(bossNextAttackMultiplier, 1),
					CurrentShield = Math.Max(bossCurrentShield, 0),
					PowerBonus = Math.Max(bossPowerBonus, 0),
					EnemyDamageDebuff = Math.Max(state.EnemyDamageDebuff, 0)
				}
			};

			foreach (MapTileData tile in tiles)
			{
				data.Tiles.Add(new TileSaveData
				{
					Color = (int)tile.Color,
					Level = tile.Level,
					Value = tile.Value,
					HitCount = tile.HitCount,
					UpgradeChoice = (int)tile.UpgradeChoice
				});
			}

			string json = JsonSerializer.Serialize(data, JsonOptions);
			using FileAccess file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
			if (file == null)
			{
				GD.PrintErr("保存失败：无法打开存档文件");
				return false;
			}

			file.StoreString(json);
			file.Flush();
			return true;
		}
		catch (Exception exception)
		{
			GD.PrintErr($"保存失败：{exception.Message}");
			return false;
		}
	}

	private bool LoadGame(IList<MapTileData> tiles, out BossSaveData bossData)
	{
		bossData = null;
		if (!HasSave())
			return false;

		try
		{
			using FileAccess file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
			if (file == null)
				return false;

			SaveGameData data = JsonSerializer.Deserialize<SaveGameData>(file.GetAsText(), JsonOptions);
			if (!ValidateSaveData(data, tiles.Count))
				return false;

			ApplySaveData(data, tiles);
			bossData = data.Boss;
			return true;
		}
		catch (Exception exception)
		{
			GD.PrintErr($"读档失败：{exception.Message}");
			return false;
		}
	}

	private static bool ValidateSaveData(SaveGameData data, int tileCount)
	{
		if (data == null || data.Player == null || data.Dice == null || data.Boss == null || data.Tiles == null)
			return false;

		// V4 开始重做了角色升级池、怪物行动循环和黑化领域。
		// 旧版本中的行动序号/状态含义已经不兼容，因此不继续迁移旧测试存档。
		if (data.Version != CurrentSaveVersion)
			return false;

		if (data.Tiles.Count != tileCount || data.Boss.Index < 0 || data.Boss.CurrentHp <= 0)
			return false;

		return true;
	}

	private static void ApplySaveData(SaveGameData data, IList<MapTileData> tiles)
	{
		GameState state = GameState.Instance;
		state.PlayerLevel = Math.Max(data.Player.Level, 1);
		state.PlayerMaxHp = Math.Max(data.Player.MaxHp, 1);
		state.PlayerHp = Math.Clamp(data.Player.Hp, 0, state.PlayerMaxHp);
		state.PlayerAtk = Math.Max(data.Player.Atk, 0);
		state.PlayerDef = Math.Max(data.Player.Def, 0);
		state.PlayerShield = Math.Clamp(data.Player.Shield, 0, GameState.MaxShield);
		state.PlayerTurnShield = 0;
		state.RedDiceAtkBonus = Math.Max(data.Player.RedDiceAtkBonus, 0);
		state.BlueDiceDefBonus = Math.Max(data.Player.BlueDiceDefBonus, 0);
		state.DoubleDiceResonanceBonus = Math.Max(data.Player.DoubleDiceResonanceBonus, 0);
		state.LifestealHealAmount = Math.Max(data.Player.LifestealHealAmount, 0);
		state.TurnStartShieldAmount = Math.Max(data.Player.TurnStartShieldAmount, 0);
		state.LifestealUsedThisTurn = false;
		state.BlackDomainApplied = data.Player.BlackDomainApplied;

		state.UpgradeCounts.Clear();
		if (data.Player.UpgradeCounts != null)
		{
			foreach (KeyValuePair<string, int> pair in data.Player.UpgradeCounts)
				state.UpgradeCounts[pair.Key] = pair.Value;
		}
		state.DefenseEffectMultiplier = state.BlackDomainApplied ? 0.5f : 1f;
		state.HealingEffectMultiplier = state.BlackDomainApplied ? 0.5f : 1f;
		state.TempAtk = 0;
		state.TempDef = 0;
		state.FlatDamageBonus = 0;
		state.SkipPlayerAttack = false;
		state.AttackMultiplier = 1f;
		state.ChargeActive = data.Player.ChargeActive;
		state.FreeActionsPending = Math.Max(data.Player.FreeActionsPending, 0);
		state.NoCounterThisBattle = false;
		state.ReflectShieldDamage = false;
		state.IgnoreEnemyShield = false;
		state.EnemyDamageBoostThisTurn = 0;
		state.ThornsReflectDamage = 0;
		state.RerollAvailable = false;
		state.HpAtSelection = 0;
		state.MaxHpAtSelection = 0;
		state.EnemyDamageDebuff = Math.Max(data.Boss.EnemyDamageDebuff, 0);
		state.PlayerPosition = PosMod(data.Player.Position, GameState.TileCount);
		state.DiceColor = data.Dice.SelectedColor == "blue" ? "blue" : "red";
		state.Dice["red"] = data.Dice.RedValue;
		state.Dice["blue"] = data.Dice.BlueValue;
		state.SkipMovementNextTurn = data.Player.SkipMovementNextTurn;
		state.CurrentTurnState = GameState.TurnState.ReadyToRoll;

		for (int i = 0; i < tiles.Count; i++)
		{
			TileSaveData savedTile = data.Tiles[i];
			if (Enum.IsDefined(typeof(MapTileData.TileColor), savedTile.Color))
				tiles[i].Color = (MapTileData.TileColor)savedTile.Color;

			tiles[i].Level = Math.Clamp(savedTile.Level, 0, MapTileData.MaxUpgrade);
			tiles[i].Value = savedTile.Value;
			tiles[i].HitCount = Math.Clamp(savedTile.HitCount, 0, MapTileData.UpgradeThreshold);
			tiles[i].UpgradeChoice =
				Enum.IsDefined(typeof(MapTileData.TileUpgrade), savedTile.UpgradeChoice)
					? (MapTileData.TileUpgrade)savedTile.UpgradeChoice
					: MapTileData.TileUpgrade.None;
		}
	}

	private static int PosMod(int value, int modulus)
	{
		int result = value % modulus;
		return result < 0 ? result + modulus : result;
	}

	// 删除存档，新游戏时调用
	public void DeleteSave()
	{
		if (FileAccess.FileExists(SavePath))
			DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(SavePath));
	}
}
