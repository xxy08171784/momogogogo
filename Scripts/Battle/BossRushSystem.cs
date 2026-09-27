using Godot;

public partial class BossRushSystem : Node
{
	private sealed class BossDefinition
	{
		public string DisplayName { get; }
		public int Atk { get; }
		public int Def { get; }
		public int MaxHp { get; }

		public BossDefinition(string displayName, int atk, int def, int maxHp)
		{
			DisplayName = displayName;
			Atk = atk;
			Def = def;
			MaxHp = maxHp;
		}
	}

	// 对应策划表：攻击、防御、血量。当前均使用 Goblin.tscn 作为占位模型。
	private static readonly BossDefinition[] Bosses =
	{
		new("史莱姆", 2, 0, 15),
		new("野猪", 3, 0, 30),
		new("石头怪", 4, 0, 50),
		new("哥布林兄弟", 5, 0, 70),
		new("Boss（黑化莫小洛）", 6, 0, 90)
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

	public Goblin RestoreBoss(int bossIndex, int currentHp)
	{
		if (bossIndex < 0 || bossIndex >= Bosses.Length || entities == null || goblinScene == null)
			return null;

		Goblin boss = SpawnBossAt(bossIndex);
		boss?.RestoreCurrentHp(currentHp);
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
		boss.ConfigureBoss(data.DisplayName, data.MaxHp, data.Atk, data.Def);

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
