using Godot;

public partial class Plot1 : Control
{
	private TextureRect g1;
	private TextureRect g2;
	private TextureRect g3;
	private TextureRect g4;

	private Timer timer;

	private int current = 1;

	public override void _Ready()
	{
		g1 = GetNode<TextureRect>("G1");
		g2 = GetNode<TextureRect>("G2");
		g3 = GetNode<TextureRect>("G3");
		g4 = GetNode<TextureRect>("G4");

		timer = GetNode<Timer>("Timer");

		// 开始时只显示第一格
		g1.Visible = true;
		g2.Visible = false;
		g3.Visible = false;
		g4.Visible = false;

		// 2秒自动显示下一格
		timer.WaitTime = 2.0f;
		timer.OneShot = false;
		timer.Start();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		// 空格 / Enter
		if (@event is InputEventKey key)
		{
			if (key.Pressed && !key.Echo &&
				(key.Keycode == Key.Space ||
				 key.Keycode == Key.Enter))
			{
				NextPanel();
			}
		}

		// 鼠标点击也可以下一格
		if (@event is InputEventMouseButton mouse)
		{
			if (mouse.Pressed)
			{
				NextPanel();
			}
		}
	}

	private void NextPanel()
	{
		current++;

		// 玩家手动跳过后，重新计算2秒
		timer.Start();

		switch (current)
		{
			case 2:
				g2.Visible = true;
				break;

			case 3:
				g3.Visible = true;
				break;

			case 4:
				g4.Visible = true;
				break;

			case 5:
				StartGame();
				break;
		}
	}

	private void _on_timer_timeout()
	{
		NextPanel();
	}

	private void StartGame()
	{
		timer.Stop();

		GetTree().ChangeSceneToFile(
            "res://Scenes/Game.tscn"
		);
	}
}
