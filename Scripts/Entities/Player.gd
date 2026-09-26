extends CharacterBody2D

@onready var tile_points = get_node("../../TilePoints") 

func _ready() -> void:
	global_position = $"../../TilePoints/Marker2D0".global_position

#角色攻击
func attack():
	pass
	
#根据步数计算目标格，并完成逐格移动
func move_by_steps(steps: int) -> int:
	var target_tile = posmod(
		GameState.player_position + steps,
		GameState.TILE_COUNT
	)
	await move_to_position(target_tile)
	return target_tile

#视觉移动
func move_to_position(target_tile: int) -> void:
	var my_pos = GameState.player_position
	var total_steps = get_forward_steps(my_pos, target_tile, GameState.TILE_COUNT)
	for i in range(total_steps):
		# 计算当前要走到哪个格子
		var next_tile = (my_pos + i + 1) % GameState.TILE_COUNT
		var marker_name = "Marker2D" + str(next_tile)
		var target_node = tile_points.get_node(marker_name)
		if target_node:
			var target_position = target_node.global_position
			var tween = create_tween()
			# 每格走 0.2 秒
			tween.tween_property(self, "global_position", target_position, 0.2)
			# 等待这一格的动画播完，再播下一格
			await tween.finished        
	# 走完之后，更新玩家真正的全局位置
	GameState.player_position = target_tile

#视觉移动辅助函数
func get_forward_steps(from_pos: int, to_pos: int, TILE_COUNT: int) -> int:
	return (to_pos - from_pos + TILE_COUNT) % TILE_COUNT


#角色升级
func upgrade(choice: String, amount: int = 1) -> bool:
	if not pick_upgrade(choice, amount):
		return false
	GameState.player_level += 1
	return true

#升级选择接口。当前 amount 默认 +1，具体数值后续可按策划规则调整。
func pick_upgrade(choice: String, amount: int = 1) -> bool:
	match choice:
		"hp":
			GameState.player_hp += amount
		"atk":
			GameState.player_atk += amount
		"def":
			GameState.player_def += amount
		_:
			return false
	return true
