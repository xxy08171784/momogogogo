using Godot;

public partial class BossRushSystem : Node
{
	private sealed class BossDefinition
	{
		public string DisplayName { get; }
		public int Atk { get; }
		public int Def { get; }
		public int MaxHp { get; }
		public MonsterAction[] Actions { get; }

		public BossDefinition(
			string displayName,
			int atk,
			int def,
			int maxHp,
			MonsterAction[] actions)
		{
			DisplayName = displayName;
			Atk = atk;
			Def = def;
			MaxHp = maxHp;
			Actions = actions;
		}
	}

	// 怪物行动严格按数组顺序循环。当前均使用 Goblin.tscn 作为占位模型。
	private static readonly BossDefinition[] Bosses =
	{
		new(
			"史莱姆",
			2,
			0,
			15,
			new MonsterAction[]
			{
				new() { Name = "黏液撞击", Description = "造成2点伤害", Damage = 2 },
				new() { Name = "软体收缩", Description = "本回合受到的伤害减少1点", DamageReduction = 1 },
				new() { Name = "汲取养分", Description = "恢复自身1点生命", Heal = 1 }
			}
		),
		new(
			"野猪",
			3,
			0,
			30,
			new MonsterAction[]
			{
				new() { Name = "獠牙冲撞", Description = "造成3点伤害", Damage = 3 },
				new() { Name = "蓄力姿势", Description = "下一次攻击伤害翻倍", ChargeMultiplier = 2 },
				new() { Name = "狂暴践踏", Description = "造成4点伤害；若已蓄力则翻倍", Damage = 4 },
				new() { Name = "泥浆翻滚", Description = "本回合受到的伤害减少2点", DamageReduction = 2 }
			}
		),
		new(
			"石头怪",
			4,
			0,
			50,
			new MonsterAction[]
			{
				new() { Name = "岩拳粉碎", Description = "造成4点伤害", Damage = 4 },
				new()
				{
					Name = "坚如磐石",
					Description = "本回合减伤3点；被攻击后反弹1点真实伤害",
					DamageReduction = 3,
					ReflectTrueDamage = 1
				},
				new() { Name = "地脉滋养", Description = "恢复自身2点生命", Heal = 2 }
			}
		),
		new(
			"哥布林兄弟",
			4,
			0,
			70,
			new MonsterAction[]
			{
				new() { Name = "哥布林A背刺", Description = "造成4点伤害", Damage = 4 },
				new()
				{
					Name = "哥布林B掩护",
					Description = "本回合减伤3点，并恢复自身2点生命",
					DamageReduction = 3,
					Heal = 2
				},
				new() { Name = "哥布林A背刺", Description = "造成4点伤害", Damage = 4 },
				new()
				{
					Name = "哥布林B掩护",
					Description = "本回合减伤3点，并恢复自身2点生命",
					DamageReduction = 3,
					Heal = 2
				}
			}
		),
		new(
			"Boss 黑化莫小洛",
			6,
			0,
			90,
			new MonsterAction[]
			{
				new() { Name = "暗影斩击", Description = "造成6点伤害", Damage = 6 },
				new()
				{
					Name = "虚空壁垒",
					Description = "本回合减伤4点；被攻击后反弹1点真实伤害",
					DamageReduction = 4,
					ReflectTrueDamage = 1
				},
				new()
				{
					Name = "黑化领域",
					Description = "玩家下回合无法移动，只能以基础攻防原地战斗",
					LockPlayerNextTurn = true
				},
				new()
				{
					Name = "虚空汲取",
					Description = "恢复自身3点生命；玩家HP低于10时翻倍",
					Heal = 3,
					DoubleHealWhenPlayerLow = true
				}
			}
		)
	};

	private PackedScene goblinScene;
	private Node2D entities;
	private Vector2 bossPosition;

	public int CurrentBossIndex { get; private set; } = -1;
	public int BossCount => Bosses.Length;
	public int CurrentBossNumber => CurrentBossIndex + 1;
	public bool HasNextBoss => CurrentBossIndex + 1 < Bosses.Length;
	public Goblin CurrentBoss { get; private set; }

	public void Setup(Node2D entitiesNode, Marker2D spawnPoint)
	{
		entities = entitiesNode;
		bossPosition = spawnPoint.Position;
		goblinScene = GD.Load<PackedScene>("res://Scenes/Entities/Goblin.tscn");
	}

	public Goblin SpawnNextBoss()
	{
		if (!HasNextBoss || entities == null || goblinScene == null)
			return null;

		return SpawnBossAt(CurrentBossIndex + 1);
	}

	public Goblin RestoreBoss(
		int bossIndex,
		int currentHp,
		int actionIndex,
		int nextAttackMultiplier)
	{
		if (bossIndex < 0 || bossIndex >= Bosses.Length || entities == null || goblinScene == null)
			return null;

		Goblin boss = SpawnBossAt(bossIndex);
		boss?.RestoreCurrentHp(currentHp);
		boss?.RestoreCombatState(actionIndex, nextAttackMultiplier);
		return boss;
	}

	private Goblin SpawnBossAt(int bossIndex)
	{
		ClearCurrentBoss();
		CurrentBossIndex = bossIndex;

		BossDefinition data = Bosses[CurrentBossIndex];
		Goblin boss = goblinScene.Instantiate<Goblin>();
		boss.Name = $"Boss{CurrentBossNumber}";
		entities.AddChild(boss);
		boss.Position = bossPosition;
		boss.ConfigureBoss(data.DisplayName, data.MaxHp, data.Atk, data.Def, data.Actions);

		CurrentBoss = boss;
		GD.Print($"刷新第 {CurrentBossNumber}/{BossCount} 只 Boss：{data.DisplayName}，ATK={data.Atk}，DEF={data.Def}，HP={data.MaxHp}");
		return boss;
	}

	public void ClearCurrentBoss()
	{
		if (CurrentBoss != null && IsInstanceValid(CurrentBoss))
			CurrentBoss.QueueFree();

		CurrentBoss = null;
	}
}
