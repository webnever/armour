// Based on:
// https://www.shadertoy.com/view/MsVXWW
shader_type spatial;
render_mode blend_mix, diffuse_burley, specular_schlick_ggx, unshaded;

// Color and appearance
uniform vec4 albedo : source_color = vec4(1.0, 0.5, 0.3, 1.0);
uniform vec4 star_color : source_color = vec4(1.0, 0.8, 0.6, 1.0);

// Noise parameters
uniform float nudge : hint_range(0.1, 2.0) = 0.739513;
uniform int noise_iterations : hint_range(1, 16) = 8;
uniform float frequency_multiplier : hint_range(1.0, 3.0) = 1.733733;

// Nebula noise parameters
uniform float nebula_scale : hint_range(0.1, 2.0) = 0.5;
uniform float base_height : hint_range(-10.0, 10.0) = 4.5;
uniform float large_scale_multiplier : hint_range(0.1, 2.0) = 0.5123;
uniform float large_scale_offset : hint_range(0.0, 200.0) = 100.0;
uniform float large_scale_intensity : hint_range(1.0, 10.0) = 4.0;
uniform float nebula_intensity : hint_range(0.1, 2.0) = 0.5;
uniform float nebula_base : hint_range(0.0, 0.1) = 0.03;

// Raymarching parameters
uniform int max_steps : hint_range(10, 100) = 30;
uniform float min_distance : hint_range(0.0, 5.0) = 0.0;
uniform float max_distance : hint_range(10.0, 200.0) = 100.0;
uniform float step_size : hint_range(0.01, 0.5) = 0.1;
uniform float min_step : hint_range(0.001, 0.1) = 0.02;

// Density and alpha parameters
uniform float density_accumulation : hint_range(0.001, 0.1) = 0.014286; // 1.0/70.0
uniform float density_threshold : hint_range(0.5, 1.5) = 0.9;
uniform float min_density : hint_range(0.01, 0.2) = 0.08;
uniform float final_min_density : hint_range(0.01, 0.2) = 0.04;
uniform float break_distance : hint_range(0.01, 1.0) = 0.1;
uniform float max_ray_length : hint_range(5.0, 50.0) = 10.0;
uniform float alpha_threshold : hint_range(0.5, 1.0) = 0.99;

// Star effect parameters
uniform float star_intensity : hint_range(0.1, 5.0) = 1.0;
uniform float star_falloff : hint_range(10.0, 100.0) = 30.0;
uniform float min_star_distance : hint_range(0.0001, 0.01) = 0.001;

// Final output parameters
uniform float brightness_multiplier : hint_range(0.1, 5.0) = 1.0;

varying vec3 global_pos;

float SpiralNoiseC(vec3 p) {
    float normalizer = 1.0 / sqrt(1.0 + nudge * nudge);
    float n = 0.0;
    float iter = 1.0;
    
    for (int i = 0; i < noise_iterations; i++) {
        n += -abs(sin(p.y * iter) + cos(p.x * iter)) / iter;
        p.xy += vec2(p.y, -p.x) * nudge;
        p.xy *= normalizer;
        p.xz += vec2(p.z, -p.x) * nudge;
        p.xz *= normalizer;
        iter *= frequency_multiplier;
    }
    return n;
}

float NebulaNoise(vec3 p) {
    float final = p.y + base_height;
    final -= SpiralNoiseC(p.xyz);
    final += SpiralNoiseC(p.zxy * large_scale_multiplier + large_scale_offset) * large_scale_intensity;
    return final;
}

float myMap(vec3 p) {
    float NebNoise = abs(NebulaNoise(p / nebula_scale) * nebula_intensity) + nebula_base;
    return NebNoise;
}

void vertex() {
    global_pos = NODE_POSITION_WORLD;
}

void fragment() {
    vec3 world_pos = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
    vec3 ray_dir = normalize(world_pos - CAMERA_POSITION_WORLD);
    
    vec4 sum = vec4(0.0);
    float d = 1.0, t = 0.0;
    float ld = 0.0, td = 0.0, w = 0.0;
    
    ALPHA = 0.0;
    t = min_distance * step(t, min_distance);
    
    for (int i = 0; i < max_steps; i++) {
        vec3 pos = world_pos + t * ray_dir;
        
        // Break conditions
        if (td > density_threshold || 
            d < break_distance * t || 
            t > max_ray_length || 
            sum.a > alpha_threshold || 
            t > max_distance) break;
        
        float d = myMap(pos);
        d = max(d, min_density);
        
        // STAR in CENTER
        vec3 ldst = global_pos - pos;
        float lDist = max(length(ldst), min_star_distance);
        sum.rgb += (star_color.rgb * star_intensity / (lDist * lDist) / star_falloff);
        
        td += density_accumulation;
        d = max(d, final_min_density);
        t += max(d * step_size * max(min(length(ldst), length(world_pos)), 1.0), min_step);
    }
    
    ALBEDO = sum.rgb * albedo.rgb;
    ALBEDO *= t * brightness_multiplier;
    ALPHA = ALBEDO.r;
}