using Godot;

[GlobalClass]
public partial class MapTileData : Resource
{
	public enum TileColor
	{
		White,
		Black,
		Red,
		Blue
	}

	// 地块升级选项：每格踩满 UpgradeThreshold 次后手动三选一，永久生效。
	public enum TileUpgrade
	{
		None,
		// 红格
		ArmorBreak,   // 破甲：攻击 +2
		Bloodthirst,  // 嗜血：攻击 20% 转回血
		Charge,       // 蓄力：本回合不攻击，下回合攻击 ×120%
		// 蓝格
		Bastion,      // 坚守：防御 +2
		Counter,      // 反击：伤害 +3
		Ward,         // 守护：防御 20% 转回血
		// 白格
		Healing,      // 治疗：回血 +4
		Lucky         // 幸运：额外再掷一次
	}

	// 踩满这个次数即可手动升级（每格最多一次）。
	public const int UpgradeThreshold = 3;

	public const int MaxUpgrade = 2;

	[Export]
	public TileColor Color { get; set; } = TileColor.White;

	[Export]
	public int Level { get; set; }

	[Export]
	public int Value { get; set; }

	// 踩踏计数 0..UpgradeThreshold；达到后且未升级时可手动升级。
	public int HitCount { get; set; }

	// 已选升级；None 表示未升级。
	public TileUpgrade UpgradeChoice { get; set; } = TileUpgrade.None;
}
