shader_type canvas_item;

// Fractal parameters - simplified color system
uniform vec3 primary_color: source_color = vec3(0.3, 0.1, 0.6);    // Main purple-blue
uniform vec3 secondary_color: source_color = vec3(0.4, 0.3, 0.5);  // Secondary purple-blue
uniform vec3 accent_color: source_color = vec3(0.8, 0.6, 0.9);     // Light purple accent
uniform int iterations: hint_range(1, 50, 1) = 10;
uniform float speed: hint_range(0.1, 10.0) = 1.0;
uniform float zoom: hint_range(0.1, 5.0) = 1.0;
uniform float subtract: hint_range(0.1, 1.0) = 0.5;
uniform float multiply: hint_range(1.0, 2.0) = 1.1;
uniform float background_alpha: hint_range(0.0, 1.0) = 0.3;

// Simple color mixing function
vec3 simple_palette(float t) {
    // Normalize t to 0-1 range
    t = fract(t);
    
    if (t < 0.33) {
        // Blend between primary and secondary
        return mix(primary_color, secondary_color, t * 3.0);
    } else if (t < 0.66) {
        // Blend between secondary and accent
        return mix(secondary_color, accent_color, (t - 0.33) * 3.0);
    } else {
        // Blend between accent and primary
        return mix(accent_color, primary_color, (t - 0.66) * 3.0);
    }
}

vec2 rotate(vec2 uv, float angle) {
    return uv * mat2(
        vec2(cos(angle), -sin(angle)),
        vec2(sin(angle), cos(angle))
    );
}

vec3 invert_color(vec3 color, float intensity) {
    return mix(color.rgb, 1.0 - color.rgb, intensity);
}

void fragment() {
    // Generate identical fractal pattern
    float time = TIME;
    float angle = time * speed * 0.1;
    vec2 uv = (SCREEN_UV - 0.5) / vec2(SCREEN_PIXEL_SIZE.x / SCREEN_PIXEL_SIZE.y, 1.0);
    vec3 color = vec3(0.0);
    
    uv /= zoom + sin(time * 0.1) * 0.5 + 0.5;
    for (int i = 0; i < iterations; i++) {
        uv = rotate((abs(uv) - subtract) * multiply, angle);
    }
    
    // Use simplified palette that only uses your specified colors
    float pattern_value = length(uv) + dot(uv, uv) * 0.1;
    color = simple_palette(pattern_value);
    
    // Apply intensity variation
    float intensity = length(uv) * 0.8;
    color *= intensity;
    
    // Optional color inversion effect (reduced)
    float invert_intensity = sin(time) * 0.1;
    color = invert_color(color, invert_intensity);
    
    // Apply background alpha
    COLOR = vec4(color, background_alpha);
}