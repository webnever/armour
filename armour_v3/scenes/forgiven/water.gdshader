shader_type spatial;

uniform sampler2D water_texture : hint_default_white;
uniform vec2 uv_offset = vec2(0.0, 0.0);
uniform vec2 uv_scale = vec2(1.0, 1.0);
uniform vec4 water_color : source_color = vec4(0.2, 0.6, 1.0, 1.0);
uniform float wave_strength : hint_range(0.0, 0.1) = 0.02;
uniform float wave_frequency : hint_range(1.0, 10.0) = 4.0;

void fragment() {
    // Apply UV scale first
    vec2 scaled_uv = UV * uv_scale;
    
    // Add some wave distortion
    vec2 wave_uv = scaled_uv;
    wave_uv.x += sin(TIME * wave_frequency + scaled_uv.y * 10.0) * wave_strength;
    wave_uv.y += cos(TIME * wave_frequency * 0.8 + scaled_uv.x * 8.0) * wave_strength;
    
    // Apply UV scrolling offset
    vec2 scrolled_uv = wave_uv + uv_offset;
    
    // Sample the texture
    vec4 texture_color = texture(water_texture, scrolled_uv);
    
    // Apply water color tint to the grayscale texture
    vec4 final_color = texture_color * water_color;
    
    ALBEDO = final_color.rgb;
    ALPHA = final_color.a;
}