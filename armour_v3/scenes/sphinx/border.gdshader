shader_type canvas_item;

// Brightness and Contrast controls
uniform float brightness_boost : hint_range(0.0, 3.0) = 1.0;
uniform float contrast_boost : hint_range(0.0, 3.0) = 1.0;

// Black to Alpha controls
uniform float alpha_threshold : hint_range(0.0, 1.0) = 0.1;
uniform bool enable_black_to_alpha = true;

// Glow controls
uniform float glow_strength : hint_range(0.0, 5.0) = 1.0;
uniform float glow_radius : hint_range(0.0, 0.05) = 0.01;
uniform vec4 glow_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform bool enable_glow = true;

// Screen texture for glow sampling
uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_linear_mipmap;

// Function to apply brightness and contrast
vec3 apply_brightness_contrast(vec3 color, float brightness_val, float contrast_val) {
    // Apply contrast: (color - 0.5) * contrast + 0.5
    color = (color - vec3(0.5)) * contrast_val + vec3(0.5);
    // Apply brightness
    color *= brightness_val;
    return clamp(color, vec3(0.0), vec3(1.0));
}

// Function to convert black pixels to transparent
float black_to_alpha(vec3 color, float threshold) {
    if (!enable_black_to_alpha) return 1.0;
    
    // Calculate luminance
    float luminance = dot(color, vec3(0.299, 0.587, 0.114));
    
    // Smooth transition from black to opaque
    return smoothstep(0.0, threshold, luminance);
}

// Simple glow effect using multiple samples
vec3 calculate_glow(vec2 uv, sampler2D tex, float radius, float strength) {
    if (!enable_glow) return vec3(0.0);
    
    vec3 glow = vec3(0.0);
    float total_weight = 0.0;
    
    // Sample in a circle pattern for glow
    int samples = 8;
    for (int i = 0; i < samples; i++) {
        float angle = float(i) * 6.28318 / float(samples);
        vec2 offset = vec2(cos(angle), sin(angle)) * radius;
        
        vec3 sample_color = texture(tex, uv + offset).rgb;
        float weight = 1.0 / (1.0 + length(offset) * 10.0);
        
        glow += sample_color * weight;
        total_weight += weight;
    }
    
    glow /= total_weight;
    return glow * strength * glow_color.rgb;
}

void fragment() {
    vec2 uv = UV;
    
    // Sample the original texture
    vec4 original_color = texture(TEXTURE, uv);
    
    // Apply brightness and contrast
    vec3 adjusted_color = apply_brightness_contrast(original_color.rgb, brightness_boost, contrast_boost);
    
    // Calculate alpha from black-to-alpha conversion
    float alpha = black_to_alpha(adjusted_color, alpha_threshold) * original_color.a;
    
    // Calculate glow effect
    vec3 glow = calculate_glow(SCREEN_UV, screen_texture, glow_radius, glow_strength);
    
    // Combine the adjusted color with glow
    vec3 final_color = adjusted_color + glow;
    
    COLOR = vec4(final_color, alpha);
}