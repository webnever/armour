shader_type canvas_item;

// Add alpha uniform
uniform float alpha : hint_range(0.0, 1.0) = 1.0;
// Existing properties
uniform vec3 glow_color : source_color = vec3(0.0, 1.0, 0.0);
uniform float speed : hint_range(0.1, 10.0) = 1.0;
uniform float density : hint_range(0.1, 5.0) = 1.0;
uniform float brightness : hint_range(0.1, 2.0) = 1.0;
uniform sampler2D character_map : hint_default_white;
uniform float character_change_speed : hint_range(0.1, 5.0) = 2.0;
uniform float character_size : hint_range(0.1, 5.0) = 1.0;
uniform float character_padding : hint_range(0.0, 0.5) = 0.1;

float random(vec2 st) {
    return fract(sin(dot(st.xy, vec2(12.9898,78.233))) * 43758.5453123);
}

void fragment() {
    // Get the UV coordinates from the TextureRect
    vec2 uv = UV * density / character_size;
    // Adjust Y scale to make characters twice as tall
    uv.y = uv.y * 0.5;

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

    // Use both column and row for character selection
    float random_value = random(vec2(column + floor(char_time), row)) * 100.0;
    float char_x = floor(mod(random_value, 10.0));
    float char_y = floor(random_value / 10.0);

    vec2 char_map_uv = (char_uv + vec2(char_x, char_y)) / vec2(10.0, 10.0);
    vec4 char_sample = texture(character_map, char_map_uv);
    float char_value = char_sample.a;

    float fade = exp(-fract(uv.y) * 2.0);
    vec3 final_color = glow_color * char_value * fade * brightness;

    // Mix with the base texture
    vec4 texture_color = texture(TEXTURE, UV);
    COLOR = vec4(final_color, char_value * fade * alpha);
}