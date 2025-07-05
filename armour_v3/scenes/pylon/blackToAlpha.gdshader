shader_type canvas_item;

uniform float threshold: hint_range(0.0, 1.0) = 0.1;
uniform float smoothness: hint_range(0.0, 0.5) = 0.05;
uniform float hue_shift: hint_range(-1.0, 1.0) = 0.0;
uniform float saturation: hint_range(0.0, 2.0) = 1.0;
uniform float value: hint_range(0.0, 2.0) = 1.0;

vec3 rgb_to_hsv(vec3 c) {
    vec4 K = vec4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    vec4 p = mix(vec4(c.bg, K.wz), vec4(c.gb, K.xy), step(c.b, c.g));
    vec4 q = mix(vec4(p.xyw, c.r), vec4(c.r, p.yzx), step(p.x, c.r));

    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return vec3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

vec3 hsv_to_rgb(vec3 c) {
    vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

void fragment() {
    vec4 tex_color = texture(TEXTURE, UV);
    
    // Convert to HSV for adjustments
    vec3 hsv = rgb_to_hsv(tex_color.rgb);
    
    // Apply adjustments
    hsv.x = fract(hsv.x + hue_shift); // Hue shift (wraps around)
    hsv.y *= saturation;              // Saturation multiplier
    hsv.z *= value;                   // Value/brightness multiplier
    
    // Clamp saturation and value to valid ranges
    hsv.y = clamp(hsv.y, 0.0, 1.0);
    hsv.z = clamp(hsv.z, 0.0, 1.0);
    
    // Convert back to RGB
    vec3 adjusted_color = hsv_to_rgb(hsv);
    
    // Calculate luminance for alpha (using original color)
    float luminance = dot(tex_color.rgb, vec3(0.299, 0.587, 0.114));
    
    // Convert black to alpha using smoothstep for soft edges
    float alpha = smoothstep(0.0, threshold + smoothness, luminance);
    
    COLOR = vec4(adjusted_color, tex_color.a * alpha);
}