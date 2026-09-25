using Godot;

public partial class SaveManager : Node
{
	// 全局访问点，其他脚本通过 SaveManager.Instance 调用
	public static SaveManager Instance { get; private set; }

	// 存档路径，放在 user:// 避免权限问题
	private const string SavePath = "user://save.json";

	public override void _Ready()
	{
		// Autoload 启动时注册自己
		Instance = this;
	}

	// 判断是否存在存档
	public bool HasSave()
	{
		return FileAccess.FileExists(SavePath);
	}

	// 删除存档，新游戏时调用
	public void DeleteSave()
	{
		if (FileAccess.FileExists(SavePath))
			DirAccess.RemoveAbsolute(SavePath);
	}
}
