using Godot;

public partial class Game : Node2D
{
	private PauseMenu pauseMenu;

	public override void _Ready()
	{
		pauseMenu = GetNode<PauseMenu>("PauseMenu");
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent &&
			keyEvent.Keycode == Key.Escape &&
			keyEvent.Pressed)
		{
			if (!GetTree().Paused)
			{
				pauseMenu.ShowPauseMenu();
			}

			GetViewport().SetInputAsHandled();
		}
	}
}
