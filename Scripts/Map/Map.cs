using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class Map : Node2D
{
	private Player player;
	private Dice redDice;
	private Dice blueDice;
	private Button rollButton;

	private static readonly MapTileData.TileColor[] TileColors =
	{
		MapTileData.TileColor.White, // 0
		MapTileData.TileColor.Red,   // 1
		MapTileData.TileColor.Blue,  // 2
		MapTileData.TileColor.White, // 3
		MapTileData.TileColor.Red,   // 4
		MapTileData.TileColor.Blue,  // 5
		MapTileData.TileColor.Black, // 6
		MapTileData.TileColor.Red,   // 7
		MapTileData.TileColor.Blue,  // 8
		MapTileData.TileColor.White, // 9
		MapTileData.TileColor.Red,   // 10
		MapTileData.TileColor.White  // 11
	};

	private readonly List<MapTileData> allTiles = new();

	public override void _Ready()
	{
		player = GetNode<Player>("Entities/Player");
		redDice = GetNode<Dice>("Entities/RedDice");
		blueDice = GetNode<Dice>("Entities/BlueDice");
		rollButton = GetNode<Button>("UI/RollButton");

		rollButton.Pressed += OnRollButtonPressed;

		CreateMap();
		GameState.Instance.PlayerPosition = 0;
		SetTurnState(GameState.TurnState.ReadyToRoll);
	}

	// 地图生成
	private void CreateMap()
	{
		allTiles.Clear();

		foreach (MapTileData.TileColor color in TileColors)
			allTiles.Add(CreateTile(color));
	}

	// 单个地块生成
	private static MapTileData CreateTile(MapTileData.TileColor tileColor)
	{
		MapTileData newTile = new()
		{
			Level = 1,
			Color = tileColor
		};

		switch (tileColor)
		{
			case MapTileData.TileColor.White:
				newTile.Value = 0;
				break;
			case MapTileData.TileColor.Black:
			case MapTileData.TileColor.Red:
			case MapTileData.TileColor.Blue:
				newTile.Value = 1;
				break;
			default:
				GD.PrintErr("地块颜色初始化出错");
				break;
		}

		return newTile;
	}

	// 掷骰子
	public void RollDice()
	{
		GameState.Instance.Dice["red"] = GD.RandRange(1, 6);
		GameState.Instance.Dice["blue"] = GD.RandRange(1, 6);
	}

	// 玩家选择骰子颜色
	public bool SelectDiceColor(string color)
	{
		if (color != "red" && color != "blue")
			return false;

		GameState.Instance.DiceColor = color;
		return true;
	}

	// UI / Controller 的主要入口：
	// 玩家点击某颗骰子后传入 "red" 或 "blue"。
	public async Task<bool> HandleDiceSelected(string color)
	{
		if (GameState.Instance.CurrentTurnState != GameState.TurnState.WaitingForDiceSelection)
			return false;

		if (!SelectDiceColor(color))
			return false;

		SetTurnState(GameState.TurnState.Moving);

		int steps = GameState.Instance.Dice[color];
		await player.MoveBySteps(steps);

		SetTurnState(GameState.TurnState.Resolving);
		ResolveTileEffect();

		SetTurnState(GameState.TurnState.ReadyToRoll);
		return true;
	}

	// 玩家行走后的地块结算
	public void ResolveTileEffect()
	{
		MapTileData tile = allTiles[GameState.Instance.PlayerPosition];

		// 先按照当前 value 结算玩家临时属性
		switch (tile.Color)
		{
			case MapTileData.TileColor.Red:
				GameState.Instance.TempAtk += tile.Value;
				break;
			case MapTileData.TileColor.Blue:
				GameState.Instance.TempDef += tile.Value;
				break;
			case MapTileData.TileColor.White:
			case MapTileData.TileColor.Black:
				// 具体玩家效果尚未确定
				break;
		}

		// 再结算地块自身成长
		if (tile.Level >= MapTileData.MaxUpgrade)
			return;

		tile.Level += 1;

		switch (tile.Color)
		{
			case MapTileData.TileColor.White:
				if (tile.Value == 0)
					tile.Level -= 1;
				tile.Value += 1;
				break;
			case MapTileData.TileColor.Red:
			case MapTileData.TileColor.Blue:
				tile.Value += 1;
				break;
			case MapTileData.TileColor.Black:
				tile.Value -= 1;
				break;
		}
	}

	// 统一修改回合阶段，并同步控制掷骰按钮
	public void SetTurnState(GameState.TurnState newState)
	{
		GameState.Instance.CurrentTurnState = newState;
		rollButton.Disabled = newState != GameState.TurnState.ReadyToRoll;
	}

	private async void OnRollButtonPressed()
	{
		if (GameState.Instance.CurrentTurnState != GameState.TurnState.ReadyToRoll)
			return;

		SetTurnState(GameState.TurnState.Rolling);
		RollDice();

		Task redAnimation = redDice.PlayRollAnimation(GameState.Instance.Dice["red"]);
		Task blueAnimation = blueDice.PlayRollAnimation(GameState.Instance.Dice["blue"]);
		await Task.WhenAll(redAnimation, blueAnimation);

		SetTurnState(GameState.TurnState.WaitingForDiceSelection);
	}
}
