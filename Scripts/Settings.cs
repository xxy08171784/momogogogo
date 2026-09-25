using Godot;

public partial class Settings : Control
{
	private Button backButton;
	private HSlider volumeSlider;
	public override void _Ready()
	{
		backButton = GetNode<Button>("BackButton");

		backButton.Pressed += OnBackPressed;
		 volumeSlider = GetNode<HSlider>("VolumeSlider");

		// 获取当前 Master 音量
		float db = AudioServer.GetBusVolumeDb(
			AudioServer.GetBusIndex("Master")
		);

		// dB 转成 0~100
		volumeSlider.Value = Mathf.DbToLinear(db) * 100;

		volumeSlider.ValueChanged += OnVolumeChanged;
	}

	private void OnBackPressed()
	{
		GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
	}
	private void OnVolumeChanged(double value)
	{
		float volume = (float)value / 100.0f;

		// 0~1 转成 dB
		float db = Mathf.LinearToDb(volume);

		int busIndex = AudioServer.GetBusIndex("Master");

		AudioServer.SetBusVolumeDb(busIndex, db);
	}
		public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent)
		{
			if (keyEvent.Keycode == Key.Escape && keyEvent.Pressed)
			{
				OnBackPressed();
			}
		}
	}
}
