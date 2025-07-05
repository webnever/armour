shader_type spatial;
render_mode depth_draw_opaque, depth_test_disabled, blend_mix, alpha_to_coverage, vertex_lighting;

uniform float brightness : hint_range(-1.0, 1.0) = 0.0;
uniform float contrast : hint_range(0.0, 3.0) = 1.0;
uniform float alpha_multiplier : hint_range(0.0, 2.0) = 1.0;
uniform bool affect_alpha = false;

// Texture uniforms
uniform sampler2D texture_albedo : source_color;
uniform sampler2D texture_normal : hint_normal;
uniform sampler2D texture_roughness : hint_default_white;
uniform sampler2D texture_metallic : hint_default_black;
uniform sampler2D texture_emission : source_color, hint_default_black;
uniform sampler2D texture_ao : hint_default_white;

// Texture properties
uniform float metallic : hint_range(0.0, 1.0) = 0.0;
uniform float roughness : hint_range(0.0, 1.0) = 0.5;
uniform float normal_scale : hint_range(-16.0, 16.0) = 1.0;
uniform float emission_energy : hint_range(0.0, 16.0) = 1.0;
uniform vec4 emission_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float ao_strength : hint_range(0.0, 1.0) = 1.0;

// UV controls
uniform vec2 uv_scale = vec2(1.0, 1.0);
uniform vec2 uv_offset = vec2(0.0, 0.0);

varying vec2 scaled_uv;

void vertex() {
    scaled_uv = UV * uv_scale + uv_offset;
}

void fragment() {
    vec4 tex_color = texture(texture_albedo, scaled_uv);
    
    // Apply brightness
    vec3 brightened = tex_color.rgb + brightness;
    
    // Apply contrast
    vec3 contrasted = (brightened - 0.5) * contrast + 0.5;
    
    // Clamp to valid range
    contrasted = clamp(contrasted, 0.0, 1.0);
    
    // Handle alpha
    float final_alpha = tex_color.a * alpha_multiplier;
    
    // Optionally apply brightness/contrast to alpha channel
    if (affect_alpha) {
        final_alpha = (final_alpha + brightness);
        final_alpha = (final_alpha - 0.5) * contrast + 0.5;
        final_alpha = clamp(final_alpha, 0.0, 1.0);
    }
    
    // Sample additional textures
    vec3 normal_tex = texture(texture_normal, scaled_uv).rgb;
    float roughness_tex = texture(texture_roughness, scaled_uv).r;
    float metallic_tex = texture(texture_metallic, scaled_uv).r;
    vec3 emission_tex = texture(texture_emission, scaled_uv).rgb;
    float ao_tex = texture(texture_ao, scaled_uv).r;
    
    // Apply textures
    ALBEDO = contrasted;
    ALPHA = final_alpha;
    
    // Normal mapping
    NORMAL_MAP = normal_tex;
    NORMAL_MAP_DEPTH = normal_scale;
    
    // Material properties
    METALLIC = metallic * metallic_tex;
    ROUGHNESS = roughness * roughness_tex;
    
    // Emission
    EMISSION = emission_tex * emission_color.rgb * emission_energy;
    
    // Ambient occlusion
    AO = mix(1.0, ao_tex, ao_strength);
}