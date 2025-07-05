shader_type canvas_item;

// Editor-adjustable parameters
uniform float EQUALCOLOR_THRESHOLD : hint_range(0.0, 1.0, 0.01) = 0.1;
uniform float STEEP_DIRECTION_THRESHOLD : hint_range(0.0, 5.0, 0.1) = 2.2;
uniform float DOMINANT_DIRECTION_THRESHOLD : hint_range(0.0, 5.0, 0.1) = 3.6;

// Optional color adjustment parameters
uniform float LUMINANCE_WEIGHT : hint_range(0.0, 1.0, 0.01) = 1.0;
uniform float COLOR_BOOST : hint_range(0.0, 2.0, 0.01) = 1.0;

// Distance function
float dist_Y(vec4 a, vec4 b) {
    // RGB to Y (luminance)
    vec3 w = vec3(0.2126, 0.7152, 0.0722) * LUMINANCE_WEIGHT;
    float Y1 = dot(w, a.rgb);
    float Y2 = dot(w, b.rgb);
    return abs(Y1 - Y2);
}

bool is_equal(vec4 a, vec4 b) {
    return dist_Y(a, b) < EQUALCOLOR_THRESHOLD;
}

void fragment() {
    vec2 ps = TEXTURE_PIXEL_SIZE;
    vec2 uv = UV;

    // Sample pixels
    vec4 P0 = texture(TEXTURE, uv + vec2(-ps.x, -ps.y));
    vec4 P1 = texture(TEXTURE, uv + vec2(0.0, -ps.y));
    vec4 P2 = texture(TEXTURE, uv + vec2(ps.x, -ps.y));
    vec4 P3 = texture(TEXTURE, uv + vec2(-ps.x, 0.0));
    vec4 P4 = texture(TEXTURE, uv);
    vec4 P5 = texture(TEXTURE, uv + vec2(ps.x, 0.0));
    vec4 P6 = texture(TEXTURE, uv + vec2(-ps.x, ps.y));
    vec4 P7 = texture(TEXTURE, uv + vec2(0.0, ps.y));
    vec4 P8 = texture(TEXTURE, uv + vec2(ps.x, ps.y));

    // Compute color differences
    float d_P4_P0 = dist_Y(P4, P0);
    float d_P4_P1 = dist_Y(P4, P1);
    float d_P4_P2 = dist_Y(P4, P2);
    float d_P4_P3 = dist_Y(P4, P3);
    float d_P4_P5 = dist_Y(P4, P5);
    float d_P4_P6 = dist_Y(P4, P6);
    float d_P4_P7 = dist_Y(P4, P7);
    float d_P4_P8 = dist_Y(P4, P8);

    // Compute diagonal edge directions
    bool dir_up_left = d_P4_P0 + d_P4_P8 < d_P4_P2 + d_P4_P6;
    bool dir_up_right = d_P4_P2 + d_P4_P6 < d_P4_P0 + d_P4_P8;

    // Check for steep diagonal edges
    bool is_steep_diagonal = (dir_up_left || dir_up_right) &&
                           (d_P4_P1 + d_P4_P7 + d_P4_P3 + d_P4_P5) < STEEP_DIRECTION_THRESHOLD;

    // Compute dominant direction
    float max_grad = max(max(d_P4_P1, d_P4_P7), max(d_P4_P3, d_P4_P5));
    bool has_dominant_direction = max_grad > DOMINANT_DIRECTION_THRESHOLD;

    // Final color computation
    vec4 result = P4;

    if (is_steep_diagonal) {
        if (dir_up_left) {
            result = mix(P4, mix(P0, P8, 0.5), 0.5);
        } else if (dir_up_right) {
            result = mix(P4, mix(P2, P6, 0.5), 0.5);
        }
    } else if (has_dominant_direction) {
        if (d_P4_P1 == max_grad) {
            result = mix(P4, P1, 0.5);
        } else if (d_P4_P7 == max_grad) {
            result = mix(P4, P7, 0.5);
        } else if (d_P4_P3 == max_grad) {
            result = mix(P4, P3, 0.5);
        } else if (d_P4_P5 == max_grad) {
            result = mix(P4, P5, 0.5);
        }
    }

    // Apply color boost
    result.rgb *= COLOR_BOOST;

    COLOR = result;
}