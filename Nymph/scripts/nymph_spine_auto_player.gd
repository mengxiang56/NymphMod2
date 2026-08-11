extends Node

const SKIN_CONFIG_PATH := "user://Nymph/appearance.cfg"
const SKIN_PATHS := [
	"res://Nymph/images/characters/skin0/char_4146_nymph_skeleton_data.tres",
	"res://Nymph/images/characters/skin1/char_4146_nymph_epoque_42_skeleton_data.tres",
	"res://Nymph/images/characters/skin2/char_4146_nymph_ambience_synesthesia_skeleton_data.tres",
]

@export var animation_name := "Idle"


func _ready() -> void:
	initialize_spine.call_deferred()


func initialize_spine() -> void:
	apply_selected_skin()
	await start_animation()


func apply_selected_skin() -> void:
	var config := ConfigFile.new()
	var skin_index := 0
	if config.load(SKIN_CONFIG_PATH) == OK:
		skin_index = int(config.get_value("character", "skin", 0))
	skin_index = clampi(skin_index, 0, SKIN_PATHS.size() - 1)

	var skeleton_data := load(SKIN_PATHS[skin_index])
	if skeleton_data == null:
		push_error("Unable to load Nymph merchant skin: %s" % SKIN_PATHS[skin_index])
		return

	get_parent().set("skeleton_data_res", skeleton_data)


func start_animation() -> void:
	var sprite := get_parent()
	while is_instance_valid(sprite) and sprite.is_inside_tree():
		var animation_state: Variant = sprite.get_animation_state()
		if animation_state != null:
			animation_state.set_animation(animation_name, true, 0)
			return
		await get_tree().process_frame
