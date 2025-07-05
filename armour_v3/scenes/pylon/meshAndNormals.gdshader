shader_type spatial;

uniform sampler3D noise_texture;
uniform float noise_scale : hint_range(0.0, 10.0) = 2.0;
uniform float displacement_strength : hint_range(0.0, 1.0) = 0.1;

// Buzz displacement parameters
uniform float buzz_frequency : hint_range(0.1, 20.0) = 8.0;
uniform float buzz_amplitude : hint_range(0.0, 0.1) = 0.02;

void vertex() {
    vec3 world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
    vec3 noise_coords = world_pos * noise_scale;

    // Original noise displacement
    float noise = texture(noise_texture, noise_coords).r;

    // Simple buzz effect - just one sine wave
    float buzz = sin(TIME * buzz_frequency) * buzz_amplitude;

    // Apply combined displacement
    VERTEX += NORMAL * (noise * displacement_strength + buzz);
}

void fragment() {
    vec3 normal_color = NORMAL * 0.5 + 0.5;
    ALBEDO = normal_color;
    EMISSION = vec3(0.0);
}