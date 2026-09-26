extends Node2D

@onready var player = $Entities/Player
@onready var red_dice = $Entities/RedDice
@onready var blue_dice = $Entities/BlueDice
@onready var roll_button = $UI/RollButton


const TileColor = MapTileData.TileColor
#地图地块数量大小
const TILE_COUNT = 12
# 预先定义好12个格子的颜色
const TILE_COLORS = [
	MapTileData.TileColor.WHITE, # 0
	MapTileData.TileColor.RED,   # 1
	MapTileData.TileColor.BLUE,  # 2
	MapTileData.TileColor.WHITE, # 3
	MapTileData.TileColor.RED,   # 4
	MapTileData.TileColor.BLUE,  # 5
	MapTileData.TileColor.BLACK, # 6 
	MapTileData.TileColor.RED,   # 7
	MapTileData.TileColor.BLUE,  # 8
	MapTileData.TileColor.WHITE, # 9
	MapTileData.TileColor.RED,   # 10
	MapTileData.TileColor.WHITE  # 11
]
var all_tile : Array[MapTileData]

func _ready() -> void:
	create_map()  #游戏开始创建地图生成地块
	GameState.player_position = 0
	set_turn_state(GameState.TurnState.READY_TO_ROLL)
#地图生成
func create_map() -> void: 
	for i in range(TILE_COUNT):
		var color_type = TILE_COLORS[i]
		var new_tile = create_tile(color_type)
		all_tile.append(new_tile)
#地块生成
func create_tile(tile_color: MapTileData.TileColor) ->MapTileData:
	var new_tile := MapTileData.new()
	new_tile.level = 1
	new_tile.color = tile_color
	match tile_color:
		MapTileData.TileColor.WHITE:
			new_tile.value = 0
		MapTileData.TileColor.BLACK, MapTileData.TileColor.RED, MapTileData.TileColor.BLUE:
			new_tile.value = 1
		_: 
			print("插入出错")
	
	return new_tile

#掷骰子
func roll_dice() :
	GameState.dice["red"] = randi_range(1, 6)
	GameState.dice["blue"] = randi_range(1, 6)

#玩家选择骰子颜色
func select_dice_color(color: String) -> bool:
	if color != "red" and color != "blue":
		return false
	GameState.dice_color = color
	return true

#给 UI / Controller 调用的接口：
#玩家点击某颗骰子后，把 "red" 或 "blue" 传进来。
func handle_dice_selected(color: String) -> bool:
	if GameState.turn_state != GameState.TurnState.WAITING_FOR_DICE_SELECTION:
		return false
	if not select_dice_color(color):
		return false

	set_turn_state(GameState.TurnState.MOVING)
	var steps: int = GameState.dice[color]
	await player.move_by_steps(steps)

	set_turn_state(GameState.TurnState.RESOLVING)
	resolve_tile_effect()

	set_turn_state(GameState.TurnState.READY_TO_ROLL)
	return true

#玩家行走后地格结算
func resolve_tile_effect() -> void:
	var tile: MapTileData = all_tile[GameState.player_position]

	#当前已明确的临时属性效果：
	#红格提供临时攻击，蓝格提供临时防御。
	match tile.color:
		TileColor.RED:
			GameState.temp_atk += tile.value
		TileColor.BLUE:
			GameState.temp_def += tile.value
		TileColor.WHITE:
			pass
		TileColor.BLACK:
			pass

	#保留原先的地块成长逻辑：地块满级后不再继续变化。
	if tile.level < MapTileData.MAX_UPGRADE:
		tile.level += 1

		if tile.color in [TileColor.WHITE, TileColor.RED, TileColor.BLUE]:
			if tile.color == TileColor.WHITE and tile.value == 0:
				tile.level -= 1
			tile.value += 1
		elif tile.color == TileColor.BLACK:
			tile.value -= 1

#统一修改回合阶段，并同步控制掷骰按钮是否可用。
func set_turn_state(new_state: GameState.TurnState) -> void:
	GameState.turn_state = new_state
	roll_button.disabled = new_state != GameState.TurnState.READY_TO_ROLL

func _on_roll_button_pressed() -> void:
	if GameState.turn_state != GameState.TurnState.READY_TO_ROLL:
		return
	set_turn_state(GameState.TurnState.ROLLING)
	roll_dice()
	red_dice.play_roll_animation(GameState.dice["red"])
	blue_dice.play_roll_animation(GameState.dice["blue"])
	await get_tree().create_timer(0.7).timeout
	set_turn_state(GameState.TurnState.WAITING_FOR_DICE_SELECTION)
