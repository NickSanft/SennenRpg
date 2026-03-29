shader_type canvas_item;
render_mode unshaded;

// Samples the composited screen rendered beneath this CanvasLayer.
uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;

// 0 = Normal, 1 = Protanopia, 2 = Deuteranopia, 3 = Tritanopia
uniform int colorblind_mode = 0;
uniform bool high_contrast  = false;

// ── HSV helpers ────────────────────────────────────────────────────────────

vec3 rgb_to_hsv(vec3 c) {
    float cmax = max(c.r, max(c.g, c.b));
    float cmin = min(c.r, min(c.g, c.b));
    float d    = cmax - cmin;

    float h = 0.0;
    if (d > 0.0001) {
        if      (cmax == c.r) h = mod((c.g - c.b) / d,       6.0) / 6.0;
        else if (cmax == c.g) h =     ((c.b - c.r) / d + 2.0) / 6.0;
        else                  h =     ((c.r - c.g) / d + 4.0) / 6.0;
    }
    float s = (cmax > 0.0001) ? d / cmax : 0.0;
    return vec3(h, s, cmax);
}

vec3 hsv_to_rgb(vec3 hsv) {
    float h6 = hsv.x * 6.0;
    float s  = hsv.y;
    float v  = hsv.z;
    int   i  = int(h6);
    float f  = h6 - float(i);
    float p  = v * (1.0 - s);
    float q  = v * (1.0 - s * f);
    float t  = v * (1.0 - s * (1.0 - f));
    if (i == 0) return vec3(v, t, p);
    if (i == 1) return vec3(q, v, p);
    if (i == 2) return vec3(p, v, t);
    if (i == 3) return vec3(p, q, v);
    if (i == 4) return vec3(t, p, v);
                return vec3(v, p, q);
}

// ── Colorblind correction ──────────────────────────────────────────────────
//
// Hue-rotation approach: shifts the hue range the deficiency makes
// indistinguishable toward a hue the player CAN discriminate.
//
// Protanopia  (red deficient)  : reds (~0.0/1.0) → orange (~0.08)
// Deuteranopia (green deficient): greens (~0.33)  → yellow (~0.17)
// Tritanopia   (blue deficient) : blues (~0.63)   → violet (~0.80)

vec3 apply_colorblind(vec3 c, int mode) {
    vec3  hsv = rgb_to_hsv(c);
    float h   = hsv.x;
    float s   = hsv.y;

    // Skip nearly-grey pixels — shifting their hue has no visible effect
    // and can create subtle artifacts.
    if (s < 0.15) return c;

    if (mode == 1) {
        // Reds wrap around 0/1; map to signed distance from 0
        float d = h > 0.5 ? h - 1.0 : h;   // range roughly (-0.5, 0.5]
        float w = 1.0 - smoothstep(0.0, 0.12, abs(d));
        hsv.x   = fract(h + w * 0.08);      // nudge toward 0.08 (orange)
    } else if (mode == 2) {
        // Greens: hue 0.25 – 0.45
        float w = smoothstep(0.20, 0.28, h) * (1.0 - smoothstep(0.38, 0.48, h));
        hsv.x   = mix(h, 0.17, w * 0.55);  // pull toward yellow (0.17)
    } else if (mode == 3) {
        // Blues: hue 0.55 – 0.72
        float w = smoothstep(0.50, 0.58, h) * (1.0 - smoothstep(0.68, 0.76, h));
        hsv.x   = mix(h, 0.80, w * 0.50);  // pull toward violet (0.80)
    }

    return hsv_to_rgb(hsv);
}

// ── High contrast ──────────────────────────────────────────────────────────
//
// Boost saturation and apply an S-curve to the Value channel so shadows
// get darker and highlights get brighter, increasing perceived contrast.

vec3 apply_high_contrast(vec3 c) {
    vec3  hsv = rgb_to_hsv(c);
    float v   = hsv.z;

    // S-curve: darkens shadows, brightens highlights
    hsv.z = v < 0.5
        ?  2.0 * v * v
        :  1.0 - 2.0 * (1.0 - v) * (1.0 - v);

    // Saturation boost — vivid colours become more vivid
    hsv.y = clamp(hsv.y * 1.40, 0.0, 1.0);

    return hsv_to_rgb(hsv);
}

// ── Fragment ───────────────────────────────────────────────────────────────

void fragment() {
    vec4 screen = texture(SCREEN_TEXTURE, SCREEN_UV);
    vec3 c      = screen.rgb;

    if (colorblind_mode > 0)
        c = apply_colorblind(c, colorblind_mode);

    if (high_contrast)
        c = apply_high_contrast(c);

    COLOR = vec4(clamp(c, 0.0, 1.0), screen.a);
}
