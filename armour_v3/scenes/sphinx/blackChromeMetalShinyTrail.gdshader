shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_burley, specular_schlick_ggx;

// Material properties
uniform float metallic : hint_range(0.0, 1.0) = 0.95;
uniform float roughness : hint_range(0.0, 1.0) = 0.1;
uniform float chrome_intensity : hint_range(0.0, 2.0) = 1.2;
uniform vec3 base_color : source_color = vec3(0.1, 0.1, 0.12);

// Tracer effects
uniform float tracer_speed : hint_range(0.1, 10.0) = 2.0;
uniform float tracer_width : hint_range(0.01, 0.2) = 0.05;
uniform float tracer_intensity : hint_range(0.0, 5.0) = 2.5;
uniform vec3 tracer_color : source_color = vec3(0.3, 0.8, 1.0);
uniform int tracer_count : hint_range(1, 8) = 3;
uniform float tracer_fade : hint_range(0.1, 2.0) = 0.8;

// Environment reflection
uniform sampler2D environment_map : hint_default_black;
uniform float env_reflection : hint_range(0.0, 2.0) = 1.5;

// Fresnel effect
uniform float fresnel_power : hint_range(0.1, 5.0) = 2.0;

varying vec3 world_position;
varying vec3 world_normal;
varying vec3 view_direction;

// Noise function for surface detail
float noise(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

float smooth_noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    
    float a = noise(i);
    float b = noise(i + vec2(1.0, 0.0));
    float c = noise(i + vec2(0.0, 1.0));
    float d = noise(i + vec2(1.0, 1.0));
    
    return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

// Fresnel calculation
float fresnel(vec3 normal, vec3 view_dir, float power) {
    float cosine = dot(normalize(normal), normalize(view_dir));
    return pow(1.0 - abs(cosine), power);
}

// Generate tracer lines
float tracer_effect(vec2 uv, float time) {
    float tracer_result = 0.0;
    
    for (int i = 0; i < tracer_count; i++) {
        float offset = float(i) * 0.3;
        float angle = time * tracer_speed + offset * 6.28318;
        
        // Create diagonal tracer lines
        vec2 tracer_dir = vec2(cos(angle), sin(angle));
        float line_pos = dot(uv, tracer_dir);
        float tracer_phase = fract(line_pos * 4.0 + time * tracer_speed);
        
        // Create sharp tracer line
        float line_dist = abs(fract(line_pos * 8.0) - 0.5) * 2.0;
        float tracer_mask = 1.0 - smoothstep(0.0, tracer_width, line_dist);
        
        // Fade effect along the tracer
        float fade = exp(-tracer_phase * tracer_fade);
        tracer_result += tracer_mask * fade;
    }
    
    return tracer_result;
}

void vertex() {
    world_position = VERTEX;
    world_normal = NORMAL;
    view_direction = (INV_VIEW_MATRIX * vec4(0.0, 0.0, 0.0, 1.0)).xyz - world_position;
}

void fragment() {
    vec3 normal = normalize(world_normal);
    vec3 view_dir = normalize(view_direction);
    
    // Base chrome material
    vec3 chrome_color = base_color;
    
    // Add surface noise for realism
    float surface_noise = smooth_noise(world_position.xy * 20.0) * 0.1;
    chrome_color += surface_noise;
    
    // Environment reflection
    vec3 reflect_dir = reflect(-view_dir, normal);
    vec2 env_uv = reflect_dir.xy * 0.5 + 0.5;
    vec3 env_color = texture(environment_map, env_uv).rgb;
    
    // Fresnel effect for edge highlighting
    float fresnel_factor = fresnel(normal, view_dir, fresnel_power);
    
    // Combine chrome base with environment
    vec3 final_color = mix(chrome_color, env_color * chrome_intensity, env_reflection * fresnel_factor);
    
    // Add tracer effects
    float time = TIME;
    float tracer_mask = tracer_effect(UV, time);
    vec3 tracer_contribution = tracer_color * tracer_mask * tracer_intensity;
    
    // Apply tracer with additive blending
    final_color += tracer_contribution;
    
    // Enhance metallic look
    final_color = mix(final_color, final_color * metallic, 0.8);
    
    // Add subtle rim lighting
    float rim = 1.0 - dot(normal, view_dir);
    rim = pow(rim, 3.0);
    final_color += rim * 0.2;
    
    ALBEDO = final_color;
    METALLIC = metallic;
    ROUGHNESS = roughness * (1.0 - tracer_mask * 0.3); // Smoother where tracers appear
    SPECULAR = 0.8;
}