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
		Lucky,        // 幸运：额外再掷一次
		// 红格（第二批）
		Scorch,       // 灼热：攻击 +3；红骰≥5 再 +5
		Fury,         // 狂怒：生命低于 50% 攻击 +10
		Resonance,    // 共鸣：红蓝骰点数相同 攻击 +20
		// 蓝格（第二批）
		Freeze,       // 冰封：怪物攻击永久 -2
		Thorns,       // 荆棘：受到怪物反击时反弹 10 点伤害
		Echo,         // 回响：蓝骰≥5 防御 +10
		// 白格（第二批）
		Blessing,     // 祝福：本回合攻击、防御各 +5
		Guidance,     // 指引：下一次投掷可重掷一个骰子
		Treasure,     // 宝箱：随机 3 个地块进度 +1
		Starlight,    // 星辉：生命上限 +2 并回复 2
		// 黑格
		Void,         // 虚空：攻防 +5 但受到 3 点伤害
		BlackMarket,  // 黑市：失去 2 血，本回合攻击 +5
		Curse,        // 诅咒：本回合攻击 +5 但怪物攻击 +3
		Shadow,       // 暗影：生命低于 25% 攻防 +15
		Sacrifice,    // 献祭：失去 3 血，随机 3 个地块进度 +1
		Convert       // 转化：升级后该黑地块变为随机颜色并直接升级
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
