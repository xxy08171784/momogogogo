using Godot;
using System;
using System.Collections.Generic;

// 地块升级注册表：稳定 ID（枚举）→ 效果实例。
// 新增一个升级：写一个 TileUpgradeEffect 子类，在 AllEffects() 里加一行即可。
public static class TileUpgradeRegistry
{
	// 下标 = 枚举值，O(1) 查表。
	private static readonly TileUpgradeEffect[] ById = BuildById();

	// 颜色 → 该色池（用于升级面板抽选项）。
	private static readonly Dictionary<MapTileData.TileColor, TileUpgradeEffect[]> PoolByColor =
		BuildPools();

	private static TileUpgradeEffect[] AllEffects()
	{
		return new TileUpgradeEffect[]
		{
			// 第一批
			new ArmorBreak(),
			new Bloodthirst(),
			new Charge(),
			new Bastion(),
			new Counter(),
			new Ward(),
			new Healing(),
			new Lucky(),
			// 第二批
			new Scorch(),
			new Fury(),
			new Resonance(),
			new Freeze(),
			new Thorns(),
			new Echo(),
			new Blessing(),
			new Guidance(),
			new Treasure(),
			new Starlight(),
			new Void(),
			new BlackMarket(),
			new Curse(),
			new Shadow(),
			new Sacrifice(),
			new Convert(),
		};
	}

	private static TileUpgradeEffect[] BuildById()
	{
		int count = Enum.GetValues<MapTileData.TileUpgrade>().Length;
		TileUpgradeEffect[] byId = new TileUpgradeEffect[count];

		foreach (TileUpgradeEffect effect in AllEffects())
		{
			int index = (int)effect.Id;

			if (index < 0 || index >= count)
			{
				GD.PrintErr($"TileUpgradeRegistry：{effect.GetType().Name} 的 Id 越界");
				continue;
			}

			if (byId[index] != null)
				GD.PrintErr($"TileUpgradeRegistry：Id {effect.Id} 重复注册");

			byId[index] = effect;
		}

		return byId;
	}

	private static Dictionary<MapTileData.TileColor, TileUpgradeEffect[]> BuildPools()
	{
		Dictionary<MapTileData.TileColor, List<TileUpgradeEffect>> collecting = new();

		foreach (TileUpgradeEffect effect in AllEffects())
		{
			if (!collecting.TryGetValue(effect.Color, out List<TileUpgradeEffect> list))
			{
				list = new List<TileUpgradeEffect>();
				collecting[effect.Color] = list;
			}

			list.Add(effect);
		}

		Dictionary<MapTileData.TileColor, TileUpgradeEffect[]> pools = new();

		foreach (KeyValuePair<MapTileData.TileColor, List<TileUpgradeEffect>> pair in collecting)
			pools[pair.Key] = pair.Value.ToArray();

		return pools;
	}

	public static TileUpgradeEffect Get(MapTileData.TileUpgrade id)
	{
		int index = (int)id;

		if (index < 0 || index >= ById.Length)
			return null;

		return ById[index];
	}

	public static IReadOnlyList<TileUpgradeEffect> Pool(MapTileData.TileColor color)
	{
		if (PoolByColor.TryGetValue(color, out TileUpgradeEffect[] pool))
			return pool;

		return Array.Empty<TileUpgradeEffect>();
	}

	// 按 Weight 不放回加权抽样，抽 n 个给升级面板。
	public static TileUpgradeEffect[] DrawWeighted(MapTileData.TileColor color, int n)
	{
		IReadOnlyList<TileUpgradeEffect> pool = Pool(color);

		if (pool.Count == 0)
			return Array.Empty<TileUpgradeEffect>();

		n = Mathf.Min(n, pool.Count);

		List<TileUpgradeEffect> remaining = new(pool);
		List<TileUpgradeEffect> picked = new(n);
		Random rng = new();

		while (picked.Count < n)
		{
			int totalWeight = 0;

			foreach (TileUpgradeEffect effect in remaining)
				totalWeight += Mathf.Max(effect.Weight, 1);

			int roll = rng.Next(totalWeight);
			int accumulated = 0;
			int chosenIndex = remaining.Count - 1;

			for (int i = 0; i < remaining.Count; i++)
			{
				accumulated += Mathf.Max(remaining[i].Weight, 1);

				if (roll < accumulated)
				{
					chosenIndex = i;
					break;
				}
			}

			picked.Add(remaining[chosenIndex]);
			remaining.RemoveAt(chosenIndex);
		}

		return picked.ToArray();
	}
}
