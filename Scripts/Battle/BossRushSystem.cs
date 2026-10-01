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
		public int LoopStartIndex { get; }

		public BossDefinition(
			string displayName,
			int atk,
			int def,
			int maxHp,
			MonsterAction[] actions,
			int loopStartIndex = 0)
		{
			DisplayName = displayName;
			Atk = atk;
			Def = def;
			MaxHp = maxHp;
			Actions = actions;
			LoopStartIndex = loopStartIndex;
		}
	}

	// 怪物行动严格按数组顺序循环。当前均使用 Goblin.tscn 作为占位模型。
	private static readonly BossDefinition[] Bosses =
	{
		new(
			"史莱姆",
			3,
			0,
			15,
			new MonsterAction[]
			{
				new() { Name = "黏液撞击", Description = "造成4点伤害", Damage = 4 },
				new() { Name = "软体收缩", Description = "获得4点护盾", ShieldGain = 4 }
			}
		),
		new(
			"野猪",
			4,
			0,
			30,
			new MonsterAction[]
			{
				new() { Name = "獠牙冲撞", Description = "造成5点伤害", Damage = 5 },
				new() { Name = "蛮力蓄势", Description = "不造成伤害；下一次攻击翻倍", ChargeMultiplier = 2 },
				new() { Name = "狂暴践踏", Description = "造成5点伤害；蓄力后翻倍", Damage = 5 },
				new() { Name = "泥浆翻滚", Description = "获得6点护盾", ShieldGain = 6 }
			}
		),
		new(
			"石头怪",
			5,
			0,
			60,
			new MonsterAction[]
			{
				new() { Name = "岩拳粉碎", Description = "造成4点伤害", Damage = 4 },
				new()
				{
					Name = "坚如磐石",
					Description = "本回合受到的伤害减少1点，并反弹2点真实伤害",
					DamageReduction = 1,
					ReflectTrueDamage = 2
				},
				new() { Name = "地脉滋养", Description = "恢复自身3点生命", Heal = 3 }
			}
		),
		new(
			"哥布林兄弟",
			6,
			0,
			40,
			new MonsterAction[]
			{
				new() { Name = "哥布林A背刺", Description = "造成10点伤害", Damage = 10 },
				new()
				{
					Name = "哥布林B掩护",
					Description = "获得6点护盾，并恢复4点生命",
					ShieldGain = 6,
					Heal = 4
				}
			}
		),
		new(
			"Boss 黑化莫小洛",
			7,
			0,
			70,
			new MonsterAction[]
			{
				new()
				{
					Name = "黑化领域",
					Description = "玩家防御和生命回复效果永久降低50%",
					ApplyBlackDomain = true
				},
				new() { Name = "暗黑斩击", Description = "造成15点伤害", Damage = 15 },
				new()
				{
					Name = "冲刺",
					Description = "造成10点伤害，并获得10点护盾",
					Damage = 10,
					ShieldGain = 10
				},
				new()
				{
					Name = "虚空汲取",
					Description = "获得3点力量，以后造成的伤害永久+3",
					PowerGain = 3
				}
			},
			1
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
		int nextAttackMultiplier,
		int currentShield,
		int powerBonus)
	{
		if (bossIndex < 0 || bossIndex >= Bosses.Length || entities == null || goblinScene == null)
			return null;

		Goblin boss = SpawnBossAt(bossIndex);
		boss?.RestoreCurrentHp(currentHp);
		boss?.RestoreCombatState(
			actionIndex,
			nextAttackMultiplier,
			currentShield,
			powerBonus
		);
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
		boss.ConfigureBoss(
			data.DisplayName,
			data.MaxHp,
			data.Atk,
			data.Def,
			data.Actions,
			data.LoopStartIndex
		);

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
