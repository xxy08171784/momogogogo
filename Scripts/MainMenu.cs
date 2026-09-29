using Godot;

public partial class MainMenu : Control
{
	private TextureButton continueButton;
	private TextureButton newGameButton;
	private TextureButton settingsButton;
	private TextureButton quitButton;

	private AudioStreamPlayer buttonSound;

	public override void _Ready()
	{
		continueButton = GetNode<TextureButton>(
			"VBoxContainer/ContinueButton"
		);

		newGameButton = GetNode<TextureButton>(
			"VBoxContainer/NewGameButton"
		);

		settingsButton = GetNode<TextureButton>(
			"VBoxContainer/SettingsButton"
		);

		quitButton = GetNode<TextureButton>(
			"VBoxContainer/QuitButton"
		);

		// 获取按钮音效节点
		buttonSound = GetNode<AudioStreamPlayer>("ButtonSound");

		// 有存档才显示继续游戏
		continueButton.Visible = SaveManager.Instance.HasSave();

		// 按下 / 松开
		continueButton.ButtonDown += () => ButtonDownEffect(continueButton);
		continueButton.ButtonUp += () => ButtonUpEffect(continueButton);

		newGameButton.ButtonDown += () => ButtonDownEffect(newGameButton);
		newGameButton.ButtonUp += () => ButtonUpEffect(newGameButton);

		settingsButton.ButtonDown += () => ButtonDownEffect(settingsButton);
		settingsButton.ButtonUp += () => ButtonUpEffect(settingsButton);

		quitButton.ButtonDown += () => ButtonDownEffect(quitButton);
		quitButton.ButtonUp += () => ButtonUpEffect(quitButton);

		// 点击事件
		continueButton.Pressed += OnContinuePressed;
		newGameButton.Pressed += OnNewGamePressed;
		settingsButton.Pressed += OnSettingsPressed;
		quitButton.Pressed += OnQuitPressed;
	}

	private void ButtonDownEffect(TextureButton button)
	{
		// 播放按钮音效
		buttonSound.Play();

		// 按下效果
		button.Position += new Vector2(0, 3);
		button.Scale = new Vector2(0.97f, 0.97f);
	}

	private void ButtonUpEffect(TextureButton button)
	{
		// 松开效果
		button.Position -= new Vector2(0, 3);
		button.Scale = new Vector2(1.0f, 1.0f);
	}

	private void OnContinuePressed()
	{
		SaveManager.Instance.RequestContinue();
		GetTree().ChangeSceneToFile("res://Scenes/Game.tscn");
	}

	private void OnNewGamePressed()
	{
		SaveManager.Instance.StartNewGame();
		GetTree().ChangeSceneToFile("res://Scenes/Plot1.tscn");
	}

	private void OnSettingsPressed()
	{
		GetTree().ChangeSceneToFile("res://Scenes/Settings.tscn");
	}

	private void OnQuitPressed()
	{
		GetTree().Quit();
	}
}
