extends Area2D

@export var dice_color: String = "red"

@onready var sprite: Sprite2D = $Sprite2D

var is_rolling := false


func play_roll_animation(result: int) -> void:
	is_rolling = true

	for i in range(10):
		$Sprite2D.frame = randi_range(0, 5)
		await get_tree().create_timer(0.06).timeout

	$Sprite2D.frame = result - 1

	is_rolling = false
