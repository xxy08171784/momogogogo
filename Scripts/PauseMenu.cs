using Godot;

public partial class PauseMenu : Control
{
	private Control pausePanel;
	private Control settingsPanel;

	private TextureButton continueButton;
	private TextureButton settingsButton;
	private TextureButton mainMenuButton;
	private TextureButton backButton;

	private HSlider volumeSlider;

	public override void _Ready()
	{
		pausePanel = GetNode<Control>("PausePanel");
		settingsPanel = GetNode<Control>("SettingsPanel");

		continueButton = GetNode<TextureButton>(
			"PausePanel/ContinueButton"
		);

		settingsButton = GetNode<TextureButton>(
			"PausePanel/SettingsButton"
		);

		mainMenuButton = GetNode<TextureButton>(
			"PausePanel/MainMenuButton"
		);

		backButton = GetNode<TextureButton>(
			"SettingsPanel/BackButton"
		);

		volumeSlider = GetNode<HSlider>(
			"SettingsPanel/VolumeSlider"
		);

		// 按下 / 松开
		continueButton.ButtonDown += () => ButtonDownEffect(continueButton);
		continueButton.ButtonUp += () => ButtonUpEffect(continueButton);

		settingsButton.ButtonDown += () => ButtonDownEffect(settingsButton);
		settingsButton.ButtonUp += () => ButtonUpEffect(settingsButton);

		mainMenuButton.ButtonDown += () => ButtonDownEffect(mainMenuButton);
		mainMenuButton.ButtonUp += () => ButtonUpEffect(mainMenuButton);

		backButton.ButtonDown += () => ButtonDownEffect(backButton);
		backButton.ButtonUp += () => ButtonUpEffect(backButton);

		// 点击事件
		continueButton.Pressed += OnContinuePressed;
		settingsButton.Pressed += OnSettingsPressed;
		mainMenuButton.Pressed += OnMainMenuPressed;
		backButton.Pressed += OnSettingsBackPressed;

		// 获取当前 Master 音量
		int busIndex = AudioServer.GetBusIndex("Master");

		float db = AudioServer.GetBusVolumeDb(busIndex);

		// dB 转成 0~100
		volumeSlider.Value = Mathf.DbToLinear(db) * 100;

		volumeSlider.ValueChanged += OnVolumeChanged;
	}

	// 按下效果
	private void ButtonDownEffect(TextureButton button)
	{
		button.Position += new Vector2(0, 3);
		button.Scale = new Vector2(0.97f, 0.97f);
	}

	// 松开效果
	private void ButtonUpEffect(TextureButton button)
	{
		button.Position -= new Vector2(0, 3);
		button.Scale = new Vector2(1.0f, 1.0f);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent &&
			keyEvent.Keycode == Key.Escape &&
			keyEvent.Pressed)
		{
			if (settingsPanel.Visible)
			{
				HideSettings();
			}
			else
			{
				HidePauseMenu();
			}

			GetViewport().SetInputAsHandled();
		}
	}

	public void ShowPauseMenu()
	{
		Show();

		pausePanel.Show();
		settingsPanel.Hide();

		GetTree().Paused = true;
	}

	public void HidePauseMenu()
	{
		Hide();

		GetTree().Paused = false;
	}

	private void OnContinuePressed()
	{
		HidePauseMenu();
	}

	private void OnSettingsPressed()
	{
		pausePanel.Hide();
		settingsPanel.Show();
	}

	private void OnSettingsBackPressed()
	{
		HideSettings();
	}

	private void HideSettings()
	{
		settingsPanel.Hide();
		pausePanel.Show();
	}

	private void OnMainMenuPressed()
	{
		Map map = GetTree().CurrentScene?.GetNodeOrNull<Map>("Map");
		map?.SaveIfStable();

		GetTree().Paused = false;

		GetTree().ChangeSceneToFile(
			"res://Scenes/MainMenu.tscn"
		);
	}

	private void OnVolumeChanged(double value)
	{
		float volume = (float)value / 100.0f;

		float db = Mathf.LinearToDb(volume);

		int busIndex = AudioServer.GetBusIndex("Master");

		AudioServer.SetBusVolumeDb(busIndex, db);
	}
}
