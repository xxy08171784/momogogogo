using Godot;

public partial class PauseMenu : Control
{
	private Control pausePanel;
	private Control settingsPanel;

	private Button continueButton;
	private Button settingsButton;
	private Button mainMenuButton;
	private Button backButton;

	private HSlider volumeSlider;

	public override void _Ready()
	{
		pausePanel = GetNode<Control>("PausePanel");
		settingsPanel = GetNode<Control>("SettingsPanel");

		continueButton = GetNode<Button>("PausePanel/ContinueButton");
		settingsButton = GetNode<Button>("PausePanel/SettingsButton");
		mainMenuButton = GetNode<Button>("PausePanel/MainMenuButton");

		backButton = GetNode<Button>("SettingsPanel/BackButton");
		volumeSlider = GetNode<HSlider>("SettingsPanel/VolumeSlider");

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

	// =========================
	// ESC
	// =========================

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent &&
			keyEvent.Keycode == Key.Escape &&
			keyEvent.Pressed)
		{
			if (settingsPanel.Visible)
			{
				// 如果正在设置界面
				// ESC 返回暂停菜单
				HideSettings();
			}
			else
			{
				// 如果正在暂停菜单
				// ESC 继续游戏
				HidePauseMenu();
			}

			GetViewport().SetInputAsHandled();
		}
	}

	// =========================
	// 暂停菜单
	// =========================

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

	// =========================
	// 继续游戏
	// =========================

	private void OnContinuePressed()
	{
		HidePauseMenu();
	}

	// =========================
	// 打开设置
	// =========================

	private void OnSettingsPressed()
	{
		pausePanel.Hide();
		settingsPanel.Show();
	}

	// =========================
	// 设置界面返回
	// =========================

	private void OnSettingsBackPressed()
	{
		HideSettings();
	}

	private void HideSettings()
	{
		settingsPanel.Hide();
		pausePanel.Show();
	}

	// =========================
	// 返回主菜单
	// =========================

	private void OnMainMenuPressed()
	{
		GetTree().Paused = false;

		GetTree().ChangeSceneToFile(
			"res://Scenes/MainMenu.tscn"
		);
	}

	// =========================
	// 音量
	// =========================

	private void OnVolumeChanged(double value)
	{
		float volume = (float)value / 100.0f;

		float db = Mathf.LinearToDb(volume);

		int busIndex = AudioServer.GetBusIndex("Master");

		AudioServer.SetBusVolumeDb(busIndex, db);
	}
}
