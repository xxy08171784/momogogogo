using Godot;

public partial class MainMenu : Control
{
	private Button continueButton;
	private Button newGameButton;
	private Button settingsButton;
	private Button quitButton;

	public override void _Ready()
	{
		continueButton = GetNode<Button>(
            "VBoxContainer/ContinueButton"
		);

		newGameButton = GetNode<Button>(
            "VBoxContainer/NewGameButton"
		);

		settingsButton = GetNode<Button>(
            "VBoxContainer/SettingsButton"
		);

		quitButton = GetNode<Button>(
            "VBoxContainer/QuitButton"
		);

		// 根据是否存在存档决定是否显示继续游戏
		continueButton.Visible = SaveManager.Instance.HasSave();
		// 连接按钮事件
		continueButton.Pressed += OnContinuePressed;
		newGameButton.Pressed += OnNewGamePressed;
		settingsButton.Pressed += OnSettingsPressed;
		quitButton.Pressed += OnQuitPressed;
	}

	private void OnContinuePressed()
	{
		GD.Print("继续游戏");

		GetTree().ChangeSceneToFile(
            "res://Scenes/Game.tscn"
		);
	}

private void OnNewGamePressed()
{
	GD.Print("开始新游戏");

	SaveManager.Instance.DeleteSave();

	GetTree().ChangeSceneToFile(
        "res://Scenes/Plot1.tscn"
	);
}

	private void OnSettingsPressed()
	{
		GD.Print("打开游戏设置");

		GetTree().ChangeSceneToFile(
            "res://Scenes/Settings.tscn"
		);
	}

	private void OnQuitPressed()
	{
		GD.Print("退出游戏");

		GetTree().Quit();
	}
}
