shader_type canvas_item;

uniform vec4 lightning_color : source_color = vec4(0.97, 0.89, 0.85, 0.65);
uniform float size : hint_range(0.0, 1.0) = 0.005;
uniform float width : hint_range(0.0, 1.0) = 0.4;
uniform float speed = 1.0;
uniform float cycle = 0.17;
uniform float ratio = 3.0;
uniform float time_shift = 0.5;
uniform int strike_count = 5;
uniform float strike_delay = 0.25;
uniform float glow_intensity : hint_range(0.0, 1.0) = 0.9;
uniform float pixel_size : hint_range(1.0, 1000.0) = 32.0;

float rand(float x) {
    return fract(sin(x) * 100000.0);
}

void fragment() {
    vec2 pixelated_uv = floor(UV * pixel_size) / pixel_size;

    float wave = 0.0;
    float time = TIME * speed;

    for (int i = 0; i < strike_count; i++) {
        float strike_offset = float(i) / float(strike_count);
        float strike_time = floor(time / strike_delay) + strike_offset;

        float bolt = abs(mod(pixelated_uv.y * cycle + (rand(strike_time) + time_shift) * -1.0, 0.5) - 0.25) - 0.125;
        bolt *= 4.0 * width;
        bolt *= (0.5 - abs(pixelated_uv.y - 0.5)) * 2.0;

        float strike_position = rand(strike_time * 10.0) - 0.5;
        float lightning = 1.0 - step(size * 0.5, abs(pixelated_uv.x - 0.5 + strike_position + bolt));
        wave += lightning;
    }

    wave = clamp(wave, 0.0, 1.0);

    // Optional: Individual strike blinking instead of global
    // float blink = step(rand(floor(time / strike_delay)) * ratio, 0.5);
    // wave *= blink;

    float dist = distance(pixelated_uv, vec2(0.5, 0.5));
    float glow = smoothstep(0.0, glow_intensity, 1.0 - dist);

    vec4 display = lightning_color * vec4(wave);
    display.rgb += glow * lightning_color.rgb * wave;

    COLOR = vec4(display.rgb, display.a * wave);
}