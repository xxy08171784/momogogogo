using Godot;
using System.Threading.Tasks;

public partial class Dice : Area2D
{
	[Signal]
	public delegate void SelectedEventHandler(string color);

	[Export]
	public string DiceColor { get; set; } = "red";

	public bool IsRolling { get; private set; }

	private Sprite2D sprite;

	public override void _Ready()
	{
		sprite = GetNode<Sprite2D>("Sprite2D");
	}

	public override void _InputEvent(Viewport viewport, InputEvent @event, int shapeIdx)
	{
		if (@event is InputEventMouseButton mouseEvent &&
			mouseEvent.ButtonIndex == MouseButton.Left &&
			mouseEvent.Pressed)
		{
			EmitSignal(SignalName.Selected, DiceColor);
		}
	}

	public async Task PlayRollAnimation(int result)
	{
		IsRolling = true;

		for (int i = 0; i < 10; i++)
		{
			sprite.Frame = GD.RandRange(0, 5);
			await ToSignal(
				GetTree().CreateTimer(0.06),
				SceneTreeTimer.SignalName.Timeout
			);
		}

		sprite.Frame = result - 1;
		IsRolling = false;
	}
}
