extends Node

const TILE_COUNT = 12

enum TurnState {
	READY_TO_ROLL,
	ROLLING,
	WAITING_FOR_DICE_SELECTION,
	MOVING,
	RESOLVING
}

#角色当前面板
var player_level :int
var player_hp   : int
var player_atk  : int
var player_def  : int

#临时属性：用于地块等临时效果，不直接修改基础面板
var temp_atk : int = 0
var temp_def : int = 0

#角色所处位置及当前位置的效果
var player_position : int

#当前回合所处阶段
var turn_state: TurnState = TurnState.READY_TO_ROLL

#本回合玩家选择的骰子颜色
var dice_color : String = "red"
#掷出的骰子点数：(0,0),前面是红色骰子掷出的数，后面是蓝色骰子掷出的数
var dice : Dictionary = {
	"red" : 0,
	"blue": 0
}

func _ready() -> void:
	player_level = 1
	player_hp = 30
	player_atk = 3
	player_def = 1

func reset_temp_stats() -> void:
	temp_atk = 0
	temp_def = 0
