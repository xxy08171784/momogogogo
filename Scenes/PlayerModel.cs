using Godot;

public partial class PlayerModel : Node
{
	// 角色基础属性
	public int MaxHp { get; private set; } = 50;
	public int Hp { get; private set; } = 50;

	public int BaseAttack { get; private set; } = 6;
	public int BaseDefense { get; private set; } = 3;
}
