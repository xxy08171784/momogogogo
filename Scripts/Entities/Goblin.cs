using Godot;
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

	private ProgressBar hpBar;
	private Label hpLabel;
	private AnimationPlayer animationPlayer;

	public override void _Ready()
	{
		hpBar = GetNode<ProgressBar>("HealthBar");
		hpLabel = GetNode<Label>("HealthBar/HpLabel");
		animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");

		CurrentHp = MaxHp;
		UpdateHealthDisplay();
		animationPlayer.Play("idle");
	}

	public void ConfigureBoss(string displayName, int maxHp, int atk, int def)
	{
		DisplayName = displayName;
		MaxHp = Mathf.Max(maxHp, 1);
		Atk = Mathf.Max(atk, 0);
		Def = Mathf.Max(def, 0);
		CurrentHp = MaxHp;

		UpdateHealthDisplay();
		EmitSignal(SignalName.HpChanged, CurrentHp, MaxHp);
		animationPlayer.Play("idle");
	}

	public int TakeDamage(int damage)
	{
		int safeDamage = Mathf.Max(damage, 0);
		int previousHp = CurrentHp;
		CurrentHp = Mathf.Max(CurrentHp - safeDamage, 0);
		int actualDamage = previousHp - CurrentHp;

		UpdateHealthDisplay();
		EmitSignal(SignalName.HpChanged, CurrentHp, MaxHp);

		if (previousHp > 0 && CurrentHp == 0)
			EmitSignal(SignalName.Died);

		return actualDamage;
	}

	// 读档专用：恢复 Boss 剩余生命，不触发死亡动画。
	public void RestoreCurrentHp(int currentHp)
	{
		CurrentHp = Mathf.Clamp(currentHp, 0, MaxHp);
		UpdateHealthDisplay();
		EmitSignal(SignalName.HpChanged, CurrentHp, MaxHp);
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
}
