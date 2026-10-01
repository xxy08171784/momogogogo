using Godot;

// 由地块数据生成悬浮说明文本。
// 第一期用静态文案表（对应当前代码里的实际行为）。
// 等"地块效果系统"（TileUpgradeEffect / 注册表）落地后，这里改为向注册表取
// DisplayName / Description 即可，Build() 的签名与调用方保持不变。
public static class TileDescription
{
	public static string ColorName(MapTileData.TileColor color)
	{
		return color switch
		{
			MapTileData.TileColor.Red => "红色地块",
			MapTileData.TileColor.Blue => "蓝色地块",
			MapTileData.TileColor.White => "白色地块",
			_ => "黑色地块"
		};
	}

	private static string TriggerText(MapTileData.TileColor color)
	{
		return color switch
		{
			MapTileData.TileColor.Red => "触发条件：骰子颜色与地块颜色一致",
			MapTileData.TileColor.Blue => "触发条件：骰子颜色与地块颜色一致",
			_ => "触发条件：踩中即触发"
		};
	}

	private static string BaseEffectText(MapTileData tile)
	{
		switch (tile.Color)
		{
			case MapTileData.TileColor.Red:
				return $"基础：骰色匹配时，本回合攻击 +{tile.Value}";

			case MapTileData.TileColor.Blue:
				return $"基础：骰色匹配时，本回合防御 +{tile.Value}";

			case MapTileData.TileColor.White:
				return "基础：首次踩中激活；之后回血，满血时转为护盾";

			default:
				return "基础：暂无效果";
		}
	}

	// 升级名称/描述取自效果注册表，与效果系统同源，避免维护两份文案。
	public static string UpgradeName(MapTileData.TileUpgrade upgrade)
	{
		return TileUpgradeRegistry.Get(upgrade)?.DisplayName ?? "";
	}

	public static string UpgradeDescription(MapTileData.TileUpgrade upgrade)
	{
		return TileUpgradeRegistry.Get(upgrade)?.Description ?? "";
	}

	public static string Build(MapTileData tile)
	{
		if (tile == null)
			return "";

		string text =
			$"{ColorName(tile.Color)}\n" +
			$"{TriggerText(tile.Color)}\n" +
			$"{ProgressText(tile)}\n" +
			"\n" +
			BaseEffectText(tile);

		if (tile.UpgradeChoice != MapTileData.TileUpgrade.None)
		{
			text +=
				$"\n\n已升级：{UpgradeName(tile.UpgradeChoice)}\n" +
				$"→ {UpgradeDescription(tile.UpgradeChoice)}";
		}

		return text;
	}

	private static string ProgressText(MapTileData tile)
	{
		if (tile.Color == MapTileData.TileColor.Black)
			return "升级：当前不可升级";

		return $"升级进度：{tile.HitCount} / {MapTileData.UpgradeThreshold}";
	}
}
