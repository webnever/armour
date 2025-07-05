shader_type canvas_item;

// Uniforms for contrast, threshold, hue, saturation, and opacity
uniform float contrast : hint_range(-100, 100, 0.01) = 1.0;
uniform float threshold : hint_range(0.0, 1.0, 0.01) = 0.5;
uniform float custom_hue : hint_range(0.0, 1.0, 0.01) = 0.0; // Range from 0.0 to 1.0
uniform float custom_saturation : hint_range(0.0, 2.0, 0.01) = 1.0; // Range from 0.0 (greyscale) to 2.0
uniform float opacity : hint_range(0.0, 1.0, 0.01) = 1.0; // Range from 0.0 (fully transparent) to 1.0 (fully opaque)

// Existing uniforms and constants
const int ML = 0;
uniform float THRESHOLD = 0.1;
uniform float AA_SCALE = 10.0;


// Function to draw anti-aliased diagonal lines
bool diag(inout vec4 sum, vec2 uv, vec2 p1, vec2 p2, sampler2D iChannel0, float LINE_THICKNESS) {
    vec4 v1 = texelFetch(iChannel0, ivec2(uv + vec2(p1.x, p1.y)), ML),
         v2 = texelFetch(iChannel0, ivec2(uv + vec2(p2.x, p2.y)), ML);
    if (length(v1 - v2) < THRESHOLD) {
        vec2 dir = p2 - p1,
             lp = uv - (floor(uv + p1) + .5);
        dir = normalize(vec2(dir.y, -dir.x));
        float l = clamp((LINE_THICKNESS - dot(lp, dir)) * AA_SCALE, 0., 1.);
        sum = mix(sum, v1, l);
        return true;
    }
    return false;
}

// Function to adjust hue and saturation
vec3 adjust_hue_saturation(vec3 color, float hue, float saturation) {
    float angle = hue * 2.0 * 3.14159265;
    float s = sin(angle), c = cos(angle);
    vec3 weights = (vec3(2.0 * c, -sqrt(3.0) * s - c, sqrt(3.0) * s - c) + 1.0) / 3.0;
    float len = length(color);
    color = vec3(
        dot(color, weights.xyz),
        dot(color, weights.zxy),
        dot(color, weights.yzx)
    );
    return len + saturation * (color - len);
}

void fragment() {
    vec2 ip = UV * (1.0 / TEXTURE_PIXEL_SIZE);
    vec4 color = texture(TEXTURE, UV);

    // Adjust contrast
    color.rgb = ((color.rgb - 0.5) * max(contrast, 0.01)) + 0.5;

    // Convert black (below threshold) to alpha
    if (length(color.rgb) < threshold) {
        color.a = 0.0;
    }

    // Adjust hue and saturation
    color.rgb = adjust_hue_saturation(color.rgb, custom_hue, custom_saturation);

    // Apply opacity
    color.a *= opacity;

    // Start with modified color as 'background'
    vec4 s = color;


    // Draw anti-aliased diagonal lines of surrounding pixels as 'foreground'
    float LINE_THICKNESS = 0.4;
    if (diag(s, ip, vec2(-1, 0), vec2(0, 1), TEXTURE, LINE_THICKNESS)) {
        LINE_THICKNESS = 0.3;
        diag(s, ip, vec2(-1, 0), vec2(1, 1), TEXTURE, LINE_THICKNESS);
        diag(s, ip, vec2(-1, -1), vec2(0, 1), TEXTURE, LINE_THICKNESS);
    }
    LINE_THICKNESS = 0.4;
    if (diag(s, ip, vec2(0, 1), vec2(1, 0), TEXTURE, LINE_THICKNESS)) {
        LINE_THICKNESS = 0.3;
        diag(s, ip, vec2(0, 1), vec2(1, -1), TEXTURE, LINE_THICKNESS);
        diag(s, ip, vec2(-1, 1), vec2(1, 0), TEXTURE, LINE_THICKNESS);
    }
    LINE_THICKNESS = 0.4;
    if (diag(s, ip, vec2(1, 0), vec2(0, -1), TEXTURE, LINE_THICKNESS)) {
        LINE_THICKNESS = 0.3;
        diag(s, ip, vec2(1, 0), vec2(-1, -1), TEXTURE, LINE_THICKNESS);
        diag(s, ip, vec2(1, 1), vec2(0, -1), TEXTURE, LINE_THICKNESS);
    }
    LINE_THICKNESS = 0.4;
    if (diag(s, ip, vec2(0, -1), vec2(-1, 0), TEXTURE, LINE_THICKNESS)) {
        LINE_THICKNESS = 0.3;
        diag(s, ip, vec2(0, -1), vec2(-1, 1), TEXTURE, LINE_THICKNESS);
        diag(s, ip, vec2(1, -1), vec2(-1, 0), TEXTURE, LINE_THICKNESS);
    }
	// Ensure alpha is properly set for the final color
    s.a = color.a;
    COLOR = s;
}
