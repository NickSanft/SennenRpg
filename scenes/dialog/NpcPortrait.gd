@tool
extends DialogicPortrait

## Custom portrait scene for all MAPP NPCs.
##
## Auto-discovers the portrait image at res://assets/sprites/npcs/{DisplayName}.jpeg
## so the Dialogic character editor cannot accidentally overwrite the path.
## Falls back to the export_overrides 'image' field if the auto path is missing.
##
## The background Polygon2D is tinted to the character's assigned color so each
## NPC has a distinct coloured frame around their portrait.

## Fallback image used when auto-discovery by display name fails.
@export_file var image := ""

const NPC_PORTRAIT_DIR := "res://assets/sprites/npcs/"
const BACKGROUND_ALPHA := 0.38
const BORDER_PAD := 6.0


func _update_portrait(passed_character: DialogicCharacter, passed_portrait: String) -> void:
	apply_character_and_portrait(passed_character, passed_portrait)

	# Prefer auto-discovered path over export_overrides so editor reverts cannot break it.
	var auto_path: String = NPC_PORTRAIT_DIR + passed_character.display_name + "_Portrait.png"
	var resolved: String = auto_path if ResourceLoader.exists(auto_path) else image
	apply_texture($Portrait, resolved)

	_update_background(passed_character.color)


func _update_background(char_color: Color) -> void:
	var portrait := $Portrait as Sprite2D
	if portrait == null or portrait.texture == null:
		($Background as Polygon2D).polygon = PackedVector2Array()
		return

	var tex_size: Vector2 = portrait.texture.get_size()
	var p: Vector2 = portrait.position  # set by apply_texture to (-w/2, -h)

	var bg_color := char_color
	bg_color.a = BACKGROUND_ALPHA
	($Background as Polygon2D).color = bg_color

	# Slightly larger than the portrait so a coloured border is always visible
	($Background as Polygon2D).polygon = PackedVector2Array([
		Vector2(p.x - BORDER_PAD,              p.y - BORDER_PAD),
		Vector2(p.x + tex_size.x + BORDER_PAD, p.y - BORDER_PAD),
		Vector2(p.x + tex_size.x + BORDER_PAD, p.y + tex_size.y + BORDER_PAD),
		Vector2(p.x - BORDER_PAD,              p.y + tex_size.y + BORDER_PAD),
	])


func _get_covered_rect() -> Rect2:
	var portrait := $Portrait as Sprite2D
	if portrait == null or portrait.texture == null:
		return Rect2()
	return Rect2(portrait.position, portrait.texture.get_size())
