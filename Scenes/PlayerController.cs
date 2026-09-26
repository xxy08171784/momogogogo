using Godot;

public partial class PlayerController : Node
{
	private PlayerModel model;
	private PlayerView view;

	// 当前所在地块
	private int currentTile = 0;

	public override void _Ready()
	{
		model = GetParent().GetNode<PlayerModel>("PlayerModel");
		view = GetParent().GetNode<PlayerView>("PlayerView");
	}

	// 外部系统调用：移动指定格数
	public void MoveSteps(int steps)
	{
		GD.Print("玩家准备移动 " + steps + " 格");

		for (int i = 0; i < steps; i++)
		{
			MoveOneTile();
		}
	}

	private void MoveOneTile()
	{
		currentTile++;

		// 12格循环
		if (currentTile >= 12)
		{
			currentTile = 0;
		}

		GD.Print("玩家现在位于第 " + currentTile + " 格");
	}
}
