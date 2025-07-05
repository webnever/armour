shader_type canvas_item;

// Image mipmap level for base upscaling
const int ML = 0;

// Equality threshold of 2 colors before forming lines
uniform float THRESHOLD = 0.1;

// Anti-aliasing scaling, smaller value make lines more blurry
uniform float AA_SCALE = 10.0;

// Improved color difference calculation
float colorDifference(vec4 color1, vec4 color2) {
    vec3 diff = abs(color1.rgb - color2.rgb);
    // Perceptual weighting based on human vision sensitivity to RGB
    float weightedDiff = dot(diff, vec3(0.3, 0.59, 0.11));
    return weightedDiff;
}

// Draw diagonal line connecting 2 pixels if within threshold
bool diag(inout vec4 sum, vec2 uv, vec2 p1, vec2 p2, sampler2D iChannel0, float LINE_THICKNESS) {
    vec4 v1 = texelFetch(iChannel0, ivec2(uv + p1), ML),
         v2 = texelFetch(iChannel0, ivec2(uv + p2), ML);

    if (colorDifference(v1, v2) < THRESHOLD) {
        vec2 dir = p2 - p1,
            lp = uv - (floor(uv + p1) + 0.5);
        dir = normalize(vec2(dir.y, -dir.x));
        float l = clamp((LINE_THICKNESS - dot(lp, dir)) * AA_SCALE, 0.0, 1.0);
        sum = mix(sum, v1, l);
        return true;
    }
    return false;
}

void fragment() {
    // Line thickness
    float LINE_THICKNESS = 0.4;
    vec2 ip = UV * (1.0 / TEXTURE_PIXEL_SIZE);

    // Start with nearest pixel as 'background'
    vec4 s = texelFetch(TEXTURE, ivec2(ip), ML);

    // Draw anti-aliased diagonal lines of surrounding pixels as 'foreground'
    if (diag(s, ip, vec2(-1, 0), vec2(0, 1), TEXTURE, LINE_THICKNESS)) {
        LINE_THICKNESS = 0.3;
        diag(s, ip, vec2(-1, 0), vec2(1, 1), TEXTURE, LINE_THICKNESS);
        diag(s, ip, vec2(-1, -1), vec2(0, 1), TEXTURE, LINE_THICKNESS);
    }

    // Repeat this pattern for the other three quadrants
    // ...

    // Simple smoothing filter (optional)
    // Implement a smoothing step here if needed

    COLOR = s;
}
