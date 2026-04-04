shader_type canvas_item;

uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform float noise_strength : hint_range(0.0, 0.3) = 0.08;

void fragment() {
	vec4 tex = texture(TEXTURE, UV);
	// Dissolve from bottom (UV.y=1) to top (UV.y=0)
	// Add noise for a rough sand-like edge
	float noise = (fract(sin(dot(UV, vec2(12.9898, 78.233))) * 43758.5453) - 0.5) * noise_strength;
	float threshold = (1.0 - UV.y) + noise;
	if (threshold < progress) discard;
	COLOR = tex;
}
