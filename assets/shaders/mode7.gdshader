shader_type canvas_item;

// SNES Mode 7-style pseudo-3D perspective for the world map.
// The TOP of the screen stays nearly unchanged (close ground).
// Moving downward, scanlines compress vertically and spread horizontally,
// creating the illusion of a receding ground plane all the way to the bottom.

uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear;
uniform float pitch : hint_range(0.2, 0.8) = 0.35;
uniform vec2 camera_offset = vec2(0.0, 0.0);
uniform float transition_progress : hint_range(0.0, 1.0) = 1.0;

void fragment() {
    vec2 uv = SCREEN_UV;
    float t = transition_progress;

    vec4 result;

    if (t < 0.001) {
        result = texture(SCREEN_TEXTURE, uv);
    } else {
        // Ground plane covers the entire screen — no sky band.
        // screen_y: 1.0 at the top (close), 0.0 at the bottom (far/horizon).
        float screen_y = 1.0 - uv.y;

        // Perspective warp: raise to power < 1 to compress near the bottom
        // (horizon) and keep near-camera (top) mostly unchanged.
        float warped_y = pow(screen_y, mix(1.0, pitch, t));

        // Map back to source texture (flipped).
        // top (close) → source_y near 0 (top of original).
        // bottom (far) → source_y near 1 (bottom of original).
        float source_y = 1.0 - warped_y;

        // Horizontal scaling: widen near the bottom, normal at top.
        // Quadratic curve keeps top half nearly untouched while the
        // lower portion fans out dramatically.
        float dist = 1.0 - screen_y;
        float h_scale = 1.0 + dist * dist * 2.0 * t;
        float source_x = 0.5 + (uv.x - 0.5) / h_scale;

        // Apply camera offset (scaled by distance — more effect near horizon)
        source_x += camera_offset.x * dist * 0.3;
        source_y -= camera_offset.y * dist * 0.15;

        // Blend between flat and mode7
        vec2 mode7_uv = vec2(source_x, source_y);
        vec2 final_uv = mix(uv, mode7_uv, t);
        final_uv = clamp(final_uv, vec2(0.0), vec2(1.0));

        // Subtle scanlines for retro feel
        float scanline = 1.0 - 0.06 * t * step(0.5, fract(FRAGCOORD.y / 2.0));

        // Slight distance fog near horizon (bottom of screen)
        float fog = smoothstep(0.0, 0.25, screen_y);
        vec3 fog_color = vec3(0.08, 0.06, 0.18);

        vec4 tex = texture(SCREEN_TEXTURE, final_uv) * vec4(vec3(scanline), 1.0);
        result = vec4(mix(fog_color, tex.rgb, mix(1.0, fog, t)), 1.0);
    }

    COLOR = result;
}
