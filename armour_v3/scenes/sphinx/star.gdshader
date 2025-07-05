shader_type spatial;
render_mode depth_draw_opaque, depth_test_disabled, shadows_disabled, specular_disabled, diffuse_lambert, blend_add;

uniform float star_brightness : hint_range(0.0, 5.0) = 2.0;
uniform float beam_intensity : hint_range(0.0, 3.0) = 1.5;
uniform float beam_width : hint_range(0.01, 0.3) = 0.05;
uniform float star_size : hint_range(0.1, 1.0) = 0.3;
uniform float glow_falloff : hint_range(1.0, 10.0) = 3.0;
uniform float beam_length : hint_range(0.5, 2.0) = 1.0;
uniform vec4 star_color : source_color = vec4(1.0, 0.9, 0.6, 1.0);
uniform vec4 beam_color : source_color = vec4(0.8, 0.9, 1.0, 1.0);
uniform float pulse_speed : hint_range(0.0, 5.0) = 1.0;
uniform float pulse_strength : hint_range(0.0, 1.0) = 0.3;
uniform float rotation_speed : hint_range(0.0, 3.0) = 0.5;
uniform float shimmer_speed : hint_range(0.0, 10.0) = 4.0;
uniform float shimmer_strength : hint_range(0.0, 1.0) = 0.4;
uniform float flicker_speed : hint_range(0.0, 20.0) = 8.0;
uniform float flicker_strength : hint_range(0.0, 1.0) = 0.2;

varying vec2 screen_uv;

void vertex() {
    // Make the quad face the camera
    MODELVIEW_MATRIX = VIEW_MATRIX * mat4(
        vec4(length(MODEL_MATRIX[0].xyz), 0.0, 0.0, 0.0),
        vec4(0.0, length(MODEL_MATRIX[1].xyz), 0.0, 0.0),
        vec4(0.0, 0.0, length(MODEL_MATRIX[2].xyz), 0.0),
        MODEL_MATRIX[3]
    );
    
    screen_uv = UV;
}

float noise(vec2 p) {
    return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453);
}

float smoothstep_pulse(float edge0, float edge1, float x) {
    float t = clamp((x - edge0) / (edge1 - edge0), 0.0, 1.0);
    return t * t * (3.0 - 2.0 * t);
}

void fragment() {
    vec2 center = vec2(0.5, 0.5);
    vec2 pos = screen_uv - center;
    
    // Rotate the star
    float rotation = TIME * rotation_speed;
    mat2 rot_matrix = mat2(vec2(cos(rotation), -sin(rotation)), 
                          vec2(sin(rotation), cos(rotation)));
    pos = rot_matrix * pos;
    
    // Calculate distance from center
    float dist = length(pos);
    
    // Time-based effects
    float pulse = 1.0 + sin(TIME * pulse_speed) * pulse_strength;
    
    // Shimmer effect - creates subtle sparkles
    float shimmer = sin(TIME * shimmer_speed + dist * 10.0) * 
                   cos(TIME * shimmer_speed * 1.3 + pos.x * 15.0) *
                   sin(TIME * shimmer_speed * 0.8 + pos.y * 12.0);
    shimmer = (shimmer + 1.0) * 0.5; // Normalize to 0-1
    shimmer = 1.0 + shimmer * shimmer_strength;
    
    // Flicker effect - random brightness variations
    float flicker_noise = noise(vec2(TIME * flicker_speed, 0.0));
    float flicker = 1.0 + (flicker_noise - 0.5) * 2.0 * flicker_strength;
    
    // Core star glow - bright center with falloff
    float star_mask = 1.0 - smoothstep(0.0, star_size, dist);
    star_mask = pow(star_mask, glow_falloff);
    
    // Horizontal beam (rotated)
    float horizontal_beam = 1.0 - smoothstep(0.0, beam_width, abs(pos.y));
    horizontal_beam *= 1.0 - smoothstep(0.0, beam_length, abs(pos.x));
    horizontal_beam = pow(horizontal_beam, 2.0);
    
    // Vertical beam (rotated)
    float vertical_beam = 1.0 - smoothstep(0.0, beam_width, abs(pos.x));
    vertical_beam *= 1.0 - smoothstep(0.0, beam_length, abs(pos.y));
    vertical_beam = pow(vertical_beam, 2.0);
    
    // Combine beams
    float beam_mask = max(horizontal_beam, vertical_beam) * beam_intensity;
    
    // Apply all effects
    float combined_effects = pulse * shimmer * flicker;
    
    // Final color mixing
    vec3 star_contribution = star_color.rgb * star_mask * star_brightness * combined_effects;
    vec3 beam_contribution = beam_color.rgb * beam_mask * combined_effects;
    
    // Combine star and beams
    vec3 final_color = star_contribution + beam_contribution;
    
    // Calculate alpha based on overall brightness
    float alpha = clamp((star_mask + beam_mask * 0.7) * combined_effects, 0.0, 1.0);
    
    ALBEDO = final_color;
    ALPHA = alpha;
    EMISSION = final_color;
}