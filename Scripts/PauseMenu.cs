
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

	// 按钮音效
	private AudioStreamPlayer buttonSound;

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

		// 获取按钮音效
		buttonSound = GetNode<AudioStreamPlayer>("ButtonSound");


		// =========================
		// 按下 / 松开效果
		// =========================

		continueButton.ButtonDown += () =>
			ButtonDownEffect(continueButton);

		continueButton.ButtonUp += () =>
			ButtonUpEffect(continueButton);


		settingsButton.ButtonDown += () =>
			ButtonDownEffect(settingsButton);

		settingsButton.ButtonUp += () =>
			ButtonUpEffect(settingsButton);


		mainMenuButton.ButtonDown += () =>
			ButtonDownEffect(mainMenuButton);

		mainMenuButton.ButtonUp += () =>
			ButtonUpEffect(mainMenuButton);


		backButton.ButtonDown += () =>
			ButtonDownEffect(backButton);

		backButton.ButtonUp += () =>
			ButtonUpEffect(backButton);


		// =========================
		// 点击事件
		// =========================

		continueButton.Pressed += OnContinuePressed;

		settingsButton.Pressed += OnSettingsPressed;

		mainMenuButton.Pressed += OnMainMenuPressed;

		backButton.Pressed += OnSettingsBackPressed;


		// =========================
		// 音量设置
		// =========================

		int busIndex = AudioServer.GetBusIndex("Master");

		float db = AudioServer.GetBusVolumeDb(busIndex);

		volumeSlider.Value =
			Mathf.DbToLinear(db) * 100;

		volumeSlider.ValueChanged += OnVolumeChanged;
	}


	// =========================
	// 按钮按下效果
	// =========================

	private void ButtonDownEffect(TextureButton button)
	{
		// 播放按钮音效
		buttonSound.Play();

		// 按钮下沉
		button.Position += new Vector2(0, 3);

		// 按钮缩小
		button.Scale = new Vector2(0.97f, 0.97f);
	}


	// =========================
	// 按钮松开效果
	// =========================

	private void ButtonUpEffect(TextureButton button)
	{
		// 恢复位置
		button.Position -= new Vector2(0, 3);

		// 恢复大小
		button.Scale = new Vector2(1.0f, 1.0f);
	}


	// =========================
	// Esc
	// =========================

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


	// =========================
	// 显示暂停菜单
	// =========================

	public void ShowPauseMenu()
	{
		Show();

		pausePanel.Show();
		settingsPanel.Hide();

		GetTree().Paused = true;
	}


	// =========================
	// 隐藏暂停菜单
	// =========================

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
	// 设置返回
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
		Map map = GetTree()
			.CurrentScene?
			.GetNodeOrNull<Map>("Map");

		map?.SaveIfStable();

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

		int busIndex =
			AudioServer.GetBusIndex("Master");

		AudioServer.SetBusVolumeDb(
			busIndex,
			db
		);
	}
}
