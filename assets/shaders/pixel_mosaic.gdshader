shader_type canvas_item;

uniform float pixel_size : hint_range(1.0, 64.0) = 1.0;
uniform sampler2D screen_texture : hint_screen_texture, filter_nearest;

void fragment() {
	vec2 screen_uv = SCREEN_UV;
	vec2 screen_size = vec2(textureSize(screen_texture, 0));
	vec2 grid_uv = floor(screen_uv * screen_size / pixel_size) * pixel_size / screen_size;
	COLOR = texture(screen_texture, grid_uv);
	COLOR.a = 1.0;
}
