using Godot;
using System;
using System.Threading.Tasks;

public partial class Goblin : Node2D
{
	[Signal]
	public delegate void HpChangedEventHandler(int currentHp, int maxHp);

	[Signal]
	public delegate void DiedEventHandler();

	[Export(PropertyHint.Range, "1,999,1")]
	public int MaxHp { get; set; } = 70;

	[Export(PropertyHint.Range, "0,999,1")]
	public int Atk { get; set; } = 5;

	[Export(PropertyHint.Range, "0,999,1")]
	public int Def { get; set; } = 0;

	public string DisplayName { get; private set; } = "哥布林兄弟";
	public int CurrentHp { get; private set; }
	public int CurrentShield { get; private set; }
	public int PowerBonus { get; private set; }
	public int CurrentActionIndex { get; private set; }
	public int NextAttackMultiplier { get; private set; } = 1;
	public int ActionCount => actions.Length;

	private ProgressBar hpBar;
	private Label hpLabel;
	private AnimationPlayer animationPlayer;
	private MonsterAction[] actions = Array.Empty<MonsterAction>();
	private int actionLoopStartIndex;

	public override void _Ready()
	{
		hpBar = GetNode<ProgressBar>("HealthBar");
		hpLabel = GetNode<Label>("HealthBar/HpLabel");
		animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");

		CurrentHp = MaxHp;
		UpdateHealthDisplay();
		animationPlayer.Play("idle");
	}

	public void ConfigureBoss(
		string displayName,
		int maxHp,
		int atk,
		int def,
		MonsterAction[] monsterActions,
		int loopStartIndex)
	{
		DisplayName = displayName;
		MaxHp = Mathf.Max(maxHp, 1);
		Atk = Mathf.Max(atk, 0);
		Def = Mathf.Max(def, 0);
		CurrentHp = MaxHp;
		CurrentShield = 0;
		PowerBonus = 0;
		actions = monsterActions ?? Array.Empty<MonsterAction>();
		actionLoopStartIndex = actions.Length == 0
			? 0
			: Mathf.Clamp(loopStartIndex, 0, actions.Length - 1);
		CurrentActionIndex = 0;
		NextAttackMultiplier = 1;

		UpdateHealthDisplay();
		EmitSignal(SignalName.HpChanged, CurrentHp, MaxHp);
		animationPlayer.Play("idle");
	}

	public MonsterAction GetCurrentAction()
	{
		if (actions.Length == 0)
		{
			return new MonsterAction
			{
				Name = "普通攻击",
				Description = $"造成{Atk}点伤害",
				Damage = Atk
			};
		}

		int safeIndex = PosMod(CurrentActionIndex, actions.Length);
		return actions[safeIndex];
	}

	public void AdvanceAction()
	{
		if (actions.Length == 0)
			return;

		CurrentActionIndex += 1;
		if (CurrentActionIndex >= actions.Length)
			CurrentActionIndex = actionLoopStartIndex;
	}

	public void SetNextAttackMultiplier(int multiplier)
	{
		NextAttackMultiplier = Mathf.Max(multiplier, 1);
	}

	public int ConsumeNextAttackMultiplier()
	{
		int multiplier = Mathf.Max(NextAttackMultiplier, 1);
		NextAttackMultiplier = 1;
		return multiplier;
	}

	public int Heal(int amount)
	{
		int safeAmount = Mathf.Max(amount, 0);
		int previousHp = CurrentHp;
		CurrentHp = Mathf.Min(CurrentHp + safeAmount, MaxHp);
		int actualHeal = CurrentHp - previousHp;

		UpdateHealthDisplay();
		EmitSignal(SignalName.HpChanged, CurrentHp, MaxHp);
		return actualHeal;
	}

	public int GainShield(int amount)
	{
		int gained = Mathf.Max(amount, 0);
		CurrentShield += gained;
		return gained;
	}

	public int GainPower(int amount)
	{
		int gained = Mathf.Max(amount, 0);
		PowerBonus += gained;
		return gained;
	}

	// ignoreShield：破甲等地块效果无视怪物护盾时传 true。
	public int TakeDamage(int damage, bool ignoreShield = false)
	{
		int safeDamage = Mathf.Max(damage, 0);
		int absorbedByShield = ignoreShield
			? 0
			: Mathf.Min(CurrentShield, safeDamage);
		CurrentShield -= absorbedByShield;
		int hpDamage = safeDamage - absorbedByShield;
		int previousHp = CurrentHp;
		CurrentHp = Mathf.Max(CurrentHp - hpDamage, 0);
		int actualDamage = previousHp - CurrentHp;

		UpdateHealthDisplay();
		EmitSignal(SignalName.HpChanged, CurrentHp, MaxHp);

		if (previousHp > 0 && CurrentHp == 0)
			EmitSignal(SignalName.Died);

		return absorbedByShield + actualDamage;
	}

	// 读档专用：恢复 Boss 剩余生命，不触发死亡动画。
	public void RestoreCurrentHp(int currentHp)
	{
		CurrentHp = Mathf.Clamp(currentHp, 0, MaxHp);
		UpdateHealthDisplay();
		EmitSignal(SignalName.HpChanged, CurrentHp, MaxHp);
	}

	public void RestoreCombatState(
		int actionIndex,
		int nextAttackMultiplier,
		int currentShield,
		int powerBonus)
	{
		if (actions.Length == 0)
		{
			CurrentActionIndex = 0;
		}
		else
		{
			CurrentActionIndex = PosMod(actionIndex, actions.Length);
		}

		NextAttackMultiplier = Mathf.Max(nextAttackMultiplier, 1);
		CurrentShield = Mathf.Max(currentShield, 0);
		PowerBonus = Mathf.Max(powerBonus, 0);
	}

	public bool IsDead() => CurrentHp <= 0;

	public async Task PlayAttackAnimation()
	{
		animationPlayer.Play("attack");
		await ToSignal(GetTree().CreateTimer(0.72), SceneTreeTimer.SignalName.Timeout);
		if (!IsDead())
			animationPlayer.Play("idle");
	}

	public async Task PlayHitAnimation()
	{
		if (IsDead())
			return;

		animationPlayer.Play("hit");
		await ToSignal(GetTree().CreateTimer(0.24), SceneTreeTimer.SignalName.Timeout);
		animationPlayer.Play("idle");
	}

	public async Task PlayDeathAnimation()
	{
		animationPlayer.Play("death");
		await ToSignal(GetTree().CreateTimer(0.72), SceneTreeTimer.SignalName.Timeout);
	}

	private void UpdateHealthDisplay()
	{
		hpBar.MaxValue = MaxHp;
		hpBar.Value = CurrentHp;
		hpLabel.Text = $"{CurrentHp} / {MaxHp}";
	}

	private static int PosMod(int value, int modulus)
	{
		int result = value % modulus;
		return result < 0 ? result + modulus : result;
	}
}
