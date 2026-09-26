using Godot;

public partial class PlayerView : Node2D
{
	private AnimatedSprite2D animatedSprite;

	public override void _Ready()
	{
		animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
	}

	// 播放待机
	public void PlayIdle(Vector2 direction)
	{
		animatedSprite.Play(GetDirectionAnimation("idle", direction));
	}

	// 播放行走
	public void PlayWalk(Vector2 direction)
	{
		animatedSprite.Play(GetDirectionAnimation("walk", direction));
	}

	// 停止动画
	public void Stop()
	{
		animatedSprite.Stop();
	}

	private string GetDirectionAnimation(string action, Vector2 direction)
	{
		if (Mathf.Abs(direction.X) > Mathf.Abs(direction.Y))
		{
			return direction.X > 0
				? action + "_right"
				: action + "_left";
		}

		return direction.Y > 0
			? action + "_down"
			: action + "_up";
	}
}
