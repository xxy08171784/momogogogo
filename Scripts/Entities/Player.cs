
using Godot;
using System.Threading.Tasks;

public partial class Player : CharacterBody2D
{
	private Node2D tilePoints;
	private AnimatedSprite2D animatedSprite;

	// =========================
	// 音效
	// =========================

	private AudioStreamPlayer walkSound;
	private AudioStreamPlayer attackSound;
	private AudioStreamPlayer hitSound;
	private AudioStreamPlayer defendSound;
	private AudioStreamPlayer healSound;
	private AudioStreamPlayer upgradeSound;


	// =========================
	// 角色方向
	// =========================

	private enum Direction
	{
		Down,
		Up,
		Left,
		Right
	}

	private Direction currentDirection = Direction.Down;


	// =========================
	// 初始化
	// =========================

	public override void _Ready()
	{
		// 获取棋盘格子
		tilePoints = GetNodeOrNull<Node2D>("../../TilePoints");

		if (tilePoints == null)
		{
			GD.PrintErr("Player：找不到 TilePoints！");
			return;
		}


		// 获取角色动画
		animatedSprite =
			GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");

		if (animatedSprite == null)
		{
			GD.PrintErr(
				"Player：找不到 AnimatedSprite2D！"
			);

			return;
		}


		// =========================
		// 获取音效节点
		// =========================

		walkSound =
			GetNodeOrNull<AudioStreamPlayer>("WalkSound");

		attackSound =
			GetNodeOrNull<AudioStreamPlayer>("AttackSound");

		hitSound =
			GetNodeOrNull<AudioStreamPlayer>("HitSound");

		defendSound =
			GetNodeOrNull<AudioStreamPlayer>("DefendSound");

		healSound =
			GetNodeOrNull<AudioStreamPlayer>("HealSound");

		upgradeSound =
			GetNodeOrNull<AudioStreamPlayer>("UpgradeSound");


		// =========================
		// 恢复玩家位置
		// =========================

		SnapToTile(
			GameState.Instance.PlayerPosition
		);


		// 游戏开始播放待机动画
		PlayIdleAnimation();
	}


	// =========================================================
	// 音效
	// =========================================================

	// 走路音效
	public void PlayWalkSound()
	{
		if (walkSound != null)
			walkSound.Play();
	}


	// 攻击音效
	public void PlayAttackSound()
	{
		if (attackSound != null)
			attackSound.Play();
	}


	// 受击音效
	public void PlayHitSound()
	{
		if (hitSound != null)
			hitSound.Play();
	}


	// 防御音效
	public void PlayDefendSound()
	{
		if (defendSound != null)
			defendSound.Play();
	}


	// 回血音效
	public void PlayHealSound()
	{
		if (healSound != null)
			healSound.Play();
	}


	// 升级音效
	public void PlayUpgradeSound()
	{
		if (upgradeSound != null)
			upgradeSound.Play();
	}


	// =========================
	// 待机动画
	// =========================

	private void PlayIdleAnimation()
	{
		if (animatedSprite == null)
			return;

		if (animatedSprite.SpriteFrames == null)
		{
			GD.PrintErr(
				"Player：AnimatedSprite2D 没有 SpriteFrames！"
			);

			return;
		}

		if (!animatedSprite.SpriteFrames.HasAnimation("idle"))
		{
			GD.PrintErr(
				"Player：找不到 idle 动画！"
			);

			return;
		}

		animatedSprite.Play("idle");
	}


	// =========================
	// 判断行走方向
	// =========================

	private void UpdateDirection(Vector2 targetPosition)
	{
		Vector2 direction =
			targetPosition - GlobalPosition;

		if (Mathf.Abs(direction.X) >
			Mathf.Abs(direction.Y))
		{
			if (direction.X > 0)
			{
				currentDirection = Direction.Right;
			}
			else
			{
				currentDirection = Direction.Left;
			}
		}
		else
		{
			if (direction.Y > 0)
			{
				currentDirection = Direction.Down;
			}
			else
			{
				currentDirection = Direction.Up;
			}
		}
	}


	// =========================
	// 行走动画
	// =========================

	private void PlayWalkAnimation()
	{
		if (animatedSprite == null)
			return;

		string animationName = "";

		switch (currentDirection)
		{
			case Direction.Down:
				animationName = "walk_down";
				break;

			case Direction.Up:
				animationName = "walk_up";
				break;

			case Direction.Left:
				animationName = "walk_left";
				break;

			case Direction.Right:
				animationName = "walk_right";
				break;
		}

		if (animatedSprite.SpriteFrames == null)
		{
			GD.PrintErr(
				"Player：没有 SpriteFrames！"
			);

			return;
		}

		if (!animatedSprite.SpriteFrames.HasAnimation(
			animationName))
		{
			GD.PrintErr(
				$"Player：找不到动画 {animationName}"
			);

			return;
		}

		animatedSprite.Play(animationName);
	}


	// =========================
	// 初始化 / 读档
	// =========================

	public void SnapToTile(int targetTile)
	{
		if (tilePoints == null)
		{
			GD.PrintErr(
				"Player：TilePoints 没有初始化！"
			);

			return;
		}

		int normalizedTile =
			PosMod(
				targetTile,
				GameState.TileCount
			);

		Node2D targetNode =
			tilePoints.GetNodeOrNull<Node2D>(
				$"Tile{normalizedTile}"
			);

		if (targetNode == null)
		{
			GD.PrintErr(
				$"找不到 Tile{normalizedTile}，无法恢复玩家位置"
			);

			return;
		}

		GlobalPosition =
			targetNode.GlobalPosition;

		GameState.Instance.PlayerPosition =
			normalizedTile;
	}


	// =========================
	// 攻击动画
	// =========================

	public async Task PlayAttackAnimation()
	{
		if (animatedSprite == null)
		{
			GD.PrintErr(
				"Player：无法播放攻击动画，AnimatedSprite2D 为 null！"
			);

			return;
		}

		if (animatedSprite.SpriteFrames == null)
		{
			GD.PrintErr(
				"Player：无法播放攻击动画，没有 SpriteFrames！"
			);

			return;
		}

		if (!animatedSprite.SpriteFrames.HasAnimation("attack"))
		{
			GD.PrintErr(
				"Player：找不到 attack 动画！"
			);

			return;
		}

		// 播放攻击音效
		PlayAttackSound();

		// 播放攻击动画
		animatedSprite.Play("attack");

		// 等待攻击动画播放完成
		await ToSignal(
			GetTree().CreateTimer(0.24),
			SceneTreeTimer.SignalName.Timeout
		);

		// 攻击结束回到待机
		PlayIdleAnimation();
	}


	// =========================
	// 受击动画
	// =========================

	public async Task PlayHitAnimation()
	{
		if (animatedSprite == null)
		{
			GD.PrintErr(
				"Player：无法播放受击动画，AnimatedSprite2D 为 null！"
			);

			return;
		}

		if (animatedSprite.SpriteFrames == null)
		{
			GD.PrintErr(
				"Player：无法播放受击动画，没有 SpriteFrames！"
			);

			return;
		}

		if (!animatedSprite.SpriteFrames.HasAnimation("hit"))
		{
			GD.PrintErr(
				"Player：找不到 hit 动画！"
			);

			return;
		}

		// 播放受击音效
		PlayHitSound();

		// 播放受击动画
		animatedSprite.Play("hit");

		// 等待受击动画
		await ToSignal(
			GetTree().CreateTimer(0.24),
			SceneTreeTimer.SignalName.Timeout
		);

		// 受击结束回到待机
		PlayIdleAnimation();
	}


	// =========================
	// 受到伤害
	// =========================

	public int TakeDamage(int damage, out int totalAbsorbed)
	{
		int safeDamage =
			Mathf.Max(damage, 0);

		int absorbedByTurnShield =
			Mathf.Min(
				GameState.Instance.PlayerTurnShield,
				safeDamage
			);

		GameState.Instance.PlayerTurnShield -=
			absorbedByTurnShield;

		int remainingAfterTurnShield =
			safeDamage - absorbedByTurnShield;

		int absorbedByShield =
			Mathf.Min(
				GameState.Instance.PlayerShield,
				remainingAfterTurnShield
			);

		GameState.Instance.PlayerShield -=
			absorbedByShield;

		int remainingDamage =
			remainingAfterTurnShield - absorbedByShield;

		int previousHp =
			GameState.Instance.PlayerHp;

		GameState.Instance.PlayerHp =
			Mathf.Max(
				previousHp - remainingDamage,
				0
			);

		totalAbsorbed = absorbedByTurnShield + absorbedByShield;
		if (totalAbsorbed > 0)
		{
			GD.Print(
				$"护盾抵消 {totalAbsorbed} 点伤害，剩余总护盾 {GameState.Instance.TotalPlayerShield}"
			);
		}

		return previousHp -
			   GameState.Instance.PlayerHp;
	}

	// 真实伤害：直接扣生命，不经过防御和护盾。
	public int TakeTrueDamage(int damage)
	{
		int safeDamage = Mathf.Max(damage, 0);
		int previousHp = GameState.Instance.PlayerHp;

		GameState.Instance.PlayerHp =
			Mathf.Max(previousHp - safeDamage, 0);

		return previousHp - GameState.Instance.PlayerHp;
	}


	// =========================
	// 根据步数移动
	// =========================

	public async Task<int> MoveBySteps(int steps)
	{
		int targetTile =
			PosMod(
				GameState.Instance.PlayerPosition + steps,
				GameState.TileCount
			);

		await MoveToPosition(targetTile);

		return targetTile;
	}


	// =========================
	// 逐格移动
	// =========================

	public async Task MoveToPosition(int targetTile)
	{
		if (tilePoints == null)
		{
			GD.PrintErr(
				"Player：无法移动，TilePoints 为 null！"
			);

			return;
		}

		int startPosition =
			GameState.Instance.PlayerPosition;

		int totalSteps =
			GetForwardSteps(
				startPosition,
				targetTile,
				GameState.TileCount
			);

		for (int i = 0; i < totalSteps; i++)
		{
			int nextTile =
				(startPosition + i + 1)
				% GameState.TileCount;

			Node2D targetNode =
				tilePoints.GetNodeOrNull<Node2D>(
					$"Tile{nextTile}"
				);

			if (targetNode == null)
			{
				GD.PrintErr(
					$"找不到 Tile{nextTile}"
				);

				continue;
			}

			// 判断这一格的移动方向
			UpdateDirection(
				targetNode.GlobalPosition
			);

			// 播放对应方向的行走动画
			PlayWalkAnimation();

			// 播放走路音效
			PlayWalkSound();

			// 移动到下一格
			Tween tween =
				CreateTween();

			tween.TweenProperty(
				this,
				"global_position",
				targetNode.GlobalPosition,
				0.5
			);

			await ToSignal(
				tween,
				Tween.SignalName.Finished
			);

			await ToSignal(
				GetTree().CreateTimer(0.2),
				SceneTreeTimer.SignalName.Timeout
			);
		}

		// 更新逻辑位置
		GameState.Instance.PlayerPosition =
			targetTile;

		// 移动结束后回到待机
		PlayIdleAnimation();
	}


	// =========================
	// 环形棋盘距离
	// =========================

	private static int GetForwardSteps(
		int fromPosition,
		int toPosition,
		int tileCount)
	{
		return PosMod(
			toPosition - fromPosition,
			tileCount
		);
	}


	// =========================
	// 正数取模
	// =========================

	private static int PosMod(
		int value,
		int modulus)
	{
		int result =
			value % modulus;

		return result < 0
			? result + modulus
			: result;
	}


	// =========================
	// 角色升级
	// =========================

	public bool Upgrade(
		string choice,
		int amount = 1)
	{
		if (!PickUpgrade(choice, amount))
			return false;

		GameState.Instance.PlayerLevel += 1;

		// 播放升级音效
		PlayUpgradeSound();

		return true;
	}


	// =========================
	// 选择升级
	// =========================

	public bool PickUpgrade(
		string choice,
		int amount = 1)
	{
		switch (choice)
		{
			case "hp":

				GameState.Instance.PlayerMaxHp +=
					amount;

				GameState.Instance.HealPlayer(amount);

				// 播放回血音效
				PlayHealSound();

				break;


			case "atk":

				GameState.Instance.PlayerAtk +=
					amount;

				break;


			case "def":

				GameState.Instance.PlayerDef +=
					amount;

				// 播放防御音效
				PlayDefendSound();

				break;


			default:

				return false;
		}

		return true;
	}
}
