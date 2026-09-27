using Godot;
using System.Threading.Tasks;

public partial class Player : CharacterBody2D
{
	private Node2D tilePoints;
	private AnimationPlayer animationPlayer;

	public override void _Ready()
	{
		tilePoints = GetNode<Node2D>("../../TilePoints");
		animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		SnapToTile(GameState.Instance.PlayerPosition);
	}

	// 初始化或读档时直接放到目标格，不播放移动动画。
	public void SnapToTile(int targetTile)
	{
		int normalizedTile = PosMod(targetTile, GameState.TileCount);
		Marker2D targetNode = tilePoints.GetNodeOrNull<Marker2D>($"Marker2D{normalizedTile}");

		if (targetNode == null)
		{
			GD.PrintErr($"找不到 Marker2D{normalizedTile}，无法恢复玩家位置");
			return;
		}

		GlobalPosition = targetNode.GlobalPosition;
		GameState.Instance.PlayerPosition = normalizedTile;
	}

	// 原地攻击动画由 AnimationPlayer 驱动，不改变角色位置。
	public async Task PlayAttackAnimation()
	{
		animationPlayer.Play("attack");
		await ToSignal(GetTree().CreateTimer(0.24), SceneTreeTimer.SignalName.Timeout);
	}

	// 原地受击动画由 AnimationPlayer 驱动。
	public async Task PlayHitAnimation()
	{
		animationPlayer.Play("hit");
		await ToSignal(GetTree().CreateTimer(0.24), SceneTreeTimer.SignalName.Timeout);
	}

	// 扣除已经由 BattleSystem 计算好的最终伤害。
	public int TakeDamage(int damage)
	{
		int safeDamage = Mathf.Max(damage, 0);
		int absorbedByShield = Mathf.Min(GameState.Instance.PlayerShield, safeDamage);

		GameState.Instance.PlayerShield -= absorbedByShield;
		int remainingDamage = safeDamage - absorbedByShield;

		int previousHp = GameState.Instance.PlayerHp;
		GameState.Instance.PlayerHp = Mathf.Max(previousHp - remainingDamage, 0);

		if (absorbedByShield > 0)
			GD.Print($"护盾抵消 {absorbedByShield} 点伤害，剩余护盾 {GameState.Instance.PlayerShield}/{GameState.MaxShield}");

		return previousHp - GameState.Instance.PlayerHp;
	}

	// 根据步数计算目标格，并完成逐格移动
	public async Task<int> MoveBySteps(int steps)
	{
		int targetTile = PosMod(
			GameState.Instance.PlayerPosition + steps,
			GameState.TileCount
		);

		await MoveToPosition(targetTile);
		return targetTile;
	}

	// 视觉移动
	public async Task MoveToPosition(int targetTile)
	{
		int startPosition = GameState.Instance.PlayerPosition;
		int totalSteps = GetForwardSteps(
			startPosition,
			targetTile,
			GameState.TileCount
		);

		for (int i = 0; i < totalSteps; i++)
		{
			int nextTile = (startPosition + i + 1) % GameState.TileCount;
			Marker2D targetNode = tilePoints.GetNodeOrNull<Marker2D>($"Marker2D{nextTile}");

			if (targetNode == null)
				continue;

			Tween tween = CreateTween();
			tween.TweenProperty(
				this,
				"global_position",
				targetNode.GlobalPosition,
				0.2
			);

			await ToSignal(tween, Tween.SignalName.Finished);
		}

		// 走完后再更新逻辑位置
		GameState.Instance.PlayerPosition = targetTile;
	}

	private static int GetForwardSteps(int fromPosition, int toPosition, int tileCount)
	{
		return PosMod(toPosition - fromPosition, tileCount);
	}

	private static int PosMod(int value, int modulus)
	{
		int result = value % modulus;
		return result < 0 ? result + modulus : result;
	}

	// 角色升级
	public bool Upgrade(string choice, int amount = 1)
	{
		if (!PickUpgrade(choice, amount))
			return false;

		GameState.Instance.PlayerLevel += 1;
		return true;
	}

	// 升级选择接口。具体成长数值后续按策划规则调整。
	public bool PickUpgrade(string choice, int amount = 1)
	{
		switch (choice)
		{
			case "hp":
				GameState.Instance.PlayerMaxHp += amount;
				GameState.Instance.PlayerHp = Mathf.Min(
					GameState.Instance.PlayerHp + amount,
					GameState.Instance.PlayerMaxHp
				);
				break;
			case "atk":
				GameState.Instance.PlayerAtk += amount;
				break;
			case "def":
				GameState.Instance.PlayerDef += amount;
				break;
			default:
				return false;
		}

		return true;
	}
}
