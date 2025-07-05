shader_type canvas_item;

// Eyelid parameters
uniform float eye_openness : hint_range(0.0, 1.0) = 1.0;
uniform float eyelid_softness : hint_range(0.0, 0.2) = 0.05;
uniform vec4 eyelid_color : source_color = vec4(0.0, 0.0, 0.0, 1.0);

// Fractal parameters
uniform vec3 color_a: source_color = vec3(0.3, 0.1, 0.6);  // Purple-blue base
uniform vec3 color_b: source_color = vec3(0.4, 0.3, 0.5);  // Purple-blue amplitude
uniform vec3 color_c: source_color = vec3(1.0, 1.0, 1.0);  // Standard frequency
uniform vec3 color_d: source_color = vec3(0.0, 0.2, 0.5);  // Purple-blue phase shift
uniform int iterations: hint_range(1, 50, 1) = 10;
uniform float speed: hint_range(0.1, 10.0) = 1.0;
uniform float zoom: hint_range(0.1, 5.0) = 1.0;
uniform float subtract: hint_range(0.1, 1.0) = 0.5;
uniform float multiply: hint_range(1.0, 2.0) = 1.1;

vec3 palette(float t, vec3 a, vec3 b, vec3 c, vec3 d) {
	return a + b * cos(2.0 * PI * (c * t + d));
}

vec2 rotate(vec2 uv, float angle) {
	return uv * mat2(
			vec2(cos(angle), -sin(angle)),
			vec2(sin(angle), cos(angle))
		);
}

vec3 invert_color(vec3 color, float intensity) {
	return mix(color.rgb, 1.0 - color.rgb, intensity);
}

void fragment() {
	// Calculate eyelid mask
	vec2 uv = UV;
	float distance_from_top = uv.y;
	float distance_from_bottom = 1.0 - uv.y;
	float min_distance = min(distance_from_top, distance_from_bottom);
	
	// visible_area: 0 (closed) -> 0.5 open; invert eye_openness logic here
	float visible_area = (1.0 - eye_openness) * 0.5;
	
	// softness scales *up* proportionally with eye_openness
	float proportional_softness = eyelid_softness * eye_openness;
	
	// Use smoothstep with proportional softness around visible_area
	float eyelid_alpha = 1.0 - smoothstep(visible_area - proportional_softness, visible_area + proportional_softness, min_distance);
	
	// Generate fractal pattern
	float time = TIME;
	float angle = time * speed * 0.1;
	vec2 fractal_uv = (SCREEN_UV - 0.5) / vec2(SCREEN_PIXEL_SIZE.x / SCREEN_PIXEL_SIZE.y, 1.0);
	vec3 fractal_color = vec3(0.0);
	
	fractal_uv /= zoom + sin(time * 0.1) * 0.5 + 0.5;
	for (int i = 0; i < iterations; i++) {
		fractal_uv = rotate((abs(fractal_uv) - subtract) * multiply, angle);
	}
	
	vec3 p = palette(length(fractal_uv) + dot(fractal_uv, fractal_uv), color_a, color_b, color_c, color_d);
	fractal_color = clamp(vec3(length(fractal_uv) * p), vec3(0.0), vec3(1.0));
	
	float intensity = sin(time) * 0.25 - 0.2;
	fractal_color = invert_color(fractal_color, intensity);
	
	// Mix fractal and eyelid colors based on eyelid mask
	// When eyelid_alpha is 1.0 (closed), show fractal
	// When eyelid_alpha is 0.0 (open), show transparent
	vec3 final_color = mix(fractal_color, eyelid_color.rgb, 0.0);
	float final_alpha = eyelid_alpha;
	
	COLOR = vec4(final_color, final_alpha);
}