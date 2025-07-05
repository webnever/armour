shader_type canvas_item;

uniform float hue_shift_speed : hint_range(0.0, 5.0) = 1.0;
uniform float hue_shift_amount : hint_range(0.0, 1.0) = 1.0;
uniform float saturation_boost : hint_range(0.0, 2.0) = 1.0;
uniform float brightness_boost : hint_range(0.0, 2.0) = 1.0;
uniform bool cycle_continuously = true;
uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_linear_mipmap;

// Convert RGB to HSV
vec3 rgb_to_hsv(vec3 c) {
    vec4 K = vec4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    vec4 p = mix(vec4(c.bg, K.wz), vec4(c.gb, K.xy), step(c.b, c.g));
    vec4 q = mix(vec4(p.xyw, c.r), vec4(c.r, p.yzx), step(p.x, c.r));
    
    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return vec3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

// Convert HSV to RGB
vec3 hsv_to_rgb(vec3 c) {
    vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

void fragment() {
    // Sample the screen texture
    vec4 original_color = texture(screen_texture, SCREEN_UV);
    
    // Convert to HSV
    vec3 hsv = rgb_to_hsv(original_color.rgb);
    
    // Calculate hue shift based on time
    float time_shift;
    if (cycle_continuously) {
        // Continuous cycling from 0 to 1 and back
        time_shift = sin(TIME * hue_shift_speed) * 0.5 + 0.5;
    } else {
        // Linear progression
        time_shift = fract(TIME * hue_shift_speed * 0.1);
    }
    
    // Apply hue shift
    hsv.x += time_shift * hue_shift_amount;
    hsv.x = fract(hsv.x); // Keep hue in 0-1 range
    
    // Apply saturation and brightness boosts
    hsv.y *= saturation_boost;
    hsv.z *= brightness_boost;
    
    // Clamp saturation and brightness to valid ranges
    hsv.y = clamp(hsv.y, 0.0, 1.0);
    hsv.z = clamp(hsv.z, 0.0, 1.0);
    
    // Convert back to RGB
    vec3 shifted_color = hsv_to_rgb(hsv);
    
    COLOR = vec4(shifted_color, original_color.a);
}