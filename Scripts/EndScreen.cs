using Godot;

// 胜利/失败场景共用逻辑。
public partial class EndScreen : Control
{
	private Button returnButton;

	public override void _Ready()
	{
		returnButton = GetNode<Button>("ReturnButton");
		returnButton.Pressed += OnReturnToMainMenuPressed;

		// 一局已经结束，立即清掉旧存档，避免重启游戏后还能“继续”已结束的局。
		SaveManager.Instance.ClearRun();
	}

	private void OnReturnToMainMenuPressed()
	{
		// 再清一次是幂等保护，确保返回主菜单时一定没有残留存档。
		SaveManager.Instance.ClearRun();
		GetTree().Paused = false;
		GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
	}
}
