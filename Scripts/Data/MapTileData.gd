class_name MapTileData
extends Resource

enum TileColor {
	WHITE,  # 白色
	BLACK, #黑色
	RED,   # 红色
	BLUE   # 蓝色
}

@export var color: TileColor = TileColor.WHITE

const MAX_UPGRADE := 2
# 地块等级
@export var level: int = 0

# 地块属性值
@export var value: int = 0



# 一个方便初始化生成的静态函数（这样主逻辑里调用一句就能生成随机地块）
#static func create_random_tile() -> TileData:
	#var tile = TileData.new()
	#var roll = randf()
	#
	#if roll < 0.4:
		#tile.type = TileType.REWARD
		#tile.color = TileColor.RED if randf() < 0.5 else TileColor.BLUE
	#elif roll < 0.7:
		#tile.type = TileType.PUNISHMENT
		#tile.punish_value = 1
	#else:
		#tile.type = TileType.BLANK
		#
	#return tile
