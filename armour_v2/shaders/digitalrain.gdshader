shader_type spatial;

// Add alpha uniform
uniform float alpha : hint_range(0.0, 1.0) = 1.0;

// Add albedo color uniform
uniform vec4 albedo_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);

// Existing properties
uniform vec3 glow_color : source_color = vec3(0.0, 1.0, 0.0);
uniform float speed : hint_range(0.1, 10.0) = 1.0;
uniform float density : hint_range(0.1, 5.0) = 1.0;
uniform float brightness : hint_range(0.1, 2.0) = 1.0;
uniform sampler2D character_map;
uniform float character_change_speed : hint_range(0.1, 5.0) = 2.0;
uniform float character_size : hint_range(0.1, 5.0) = 1.0;
uniform float character_padding : hint_range(0.0, 0.5) = 0.1;

varying vec3 world_normal;
varying vec3 world_position;

float random(vec2 st) {
    return fract(sin(dot(st.xy, vec2(12.9898,78.233))) * 43758.5453123);
}

void vertex() {
    world_normal = (MODEL_MATRIX * vec4(NORMAL, 0.0)).xyz;
    world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
    vec3 normal = normalize(world_normal);

    vec2 uv = vec2(
        dot(world_position, normalize(cross(normal, vec3(0.0, 1.0, 0.0)))),
        dot(world_position, normalize(cross(normal, cross(normal, vec3(0.0, 1.0, 0.0)))))
    );

    uv *= density / character_size;

    float time = TIME * speed;
    uv.y += time;

    float column = floor(uv.x);
    float row = floor(uv.y);

    float column_speed = random(vec2(column, 0.0)) * 0.8 + 0.2;
    float column_offset = random(vec2(column, 1.0)) * 10.0;

    float char_time = TIME * character_change_speed + column_offset;
    vec2 char_uv = vec2(
        fract(uv.x),
        fract(uv.y - column_speed * time)
    );

    float random_value = random(vec2(column, floor(char_time))) * 100.0;
    float char_x = floor(mod(random_value, 10.0));
    float char_y = floor(random_value / 10.0);

    vec2 char_map_uv = (char_uv + vec2(char_x, char_y)) / vec2(10.0, 10.0);
    vec4 char_sample = texture(character_map, char_map_uv);
    float char_value = char_sample.a;

    float fade = exp(-fract(uv.y) * 2.0);

    vec3 final_color = glow_color * char_value * fade * brightness;

    // Blend the albedo color with the digital rain effect
    vec3 albedo = albedo_color.rgb;
    float albedo_alpha = albedo_color.a;

    vec3 blended_color = mix(albedo, final_color, char_value * fade * alpha);

    // Output the final color and alpha
    ALBEDO = blended_color;
    EMISSION = final_color;
    METALLIC = 0.0;
    ROUGHNESS = 1.0;
    ALPHA = albedo_alpha * (1.0 - char_value * fade * alpha) + char_value * fade * alpha;
}