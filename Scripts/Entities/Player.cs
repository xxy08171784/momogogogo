using Godot;
using System.Threading.Tasks;

public partial class Player : CharacterBody2D
{
	private Node2D tilePoints;

	public override void _Ready()
	{
		tilePoints = GetNode<Node2D>("../../TilePoints");
		GlobalPosition = GetNode<Marker2D>("../../TilePoints/Marker2D0").GlobalPosition;
	}

	// 角色攻击：当前阶段暂不实现
	public void Attack()
	{
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
				GameState.Instance.PlayerHp += amount;
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
