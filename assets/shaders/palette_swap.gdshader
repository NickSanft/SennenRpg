shader_type canvas_item;

// Increase if you need more than 32 unique sprite colours.
const int MAX_COLORS = 32;

// Parallel arrays: source[i] is the original colour, target[i] is the replacement.
// Unused slots must have source_colors[i].a == 0.0 (default for unset entries).
uniform vec4  source_colors[MAX_COLORS];
uniform vec4  target_colors[MAX_COLORS];
uniform float tolerance : hint_range(0.0, 0.1) = 0.01;

void fragment() {
	vec4 col     = texture(TEXTURE, UV);
	vec4 result  = col;
	bool matched = false;

	if (col.a >= 0.01) {
		for (int i = 0; i < MAX_COLORS; i++) {
			// source_colors[i].a == 0 means slot is unused — skip it.
			if (!matched && source_colors[i].a > 0.5) {
				if (distance(col.rgb, source_colors[i].rgb) < tolerance) {
					result  = vec4(target_colors[i].rgb, col.a);
					matched = true;
				}
			}
		}
	}

	COLOR = result;
}
