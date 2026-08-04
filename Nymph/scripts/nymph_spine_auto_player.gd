extends Node

@export var animation_name := "Idle"


func _ready() -> void:
	start_animation.call_deferred()


func start_animation() -> void:
	var sprite := get_parent()
	while is_instance_valid(sprite) and sprite.is_inside_tree():
		var animation_state: Variant = sprite.get_animation_state()
		if animation_state != null:
			animation_state.set_animation(animation_name, true, 0)
			return
		await get_tree().process_frame
