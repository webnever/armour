shader_type canvas_item;

uniform vec4 black_color: source_color = vec4(0.0, 0.0, 0.0, 1.0);
uniform vec4 red_color: source_color = vec4(1.0, 0.0, 0.0, 1.0);
uniform vec4 white_color: source_color = vec4(1.0, 0.0, 0.0, 0.0);
uniform float black_threshold: hint_range(0.0, 1.0) = 0.3;
uniform float red_threshold: hint_range(0.0, 1.0) = 0.6;
uniform float white_threshold: hint_range(0.0, 1.0) = 0.8;
uniform float noise_scale: hint_range(0.1, 5.0) = 1.0;
uniform float noise_intensity: hint_range(0.0, 10.0) = 1.0;
uniform float time_scale: hint_range(0.1, 5.0) = 1.0;
uniform int octaves: hint_range(1, 8) = 3;
uniform float centerX: hint_range(-1.5, 1.5) = 0.0;
uniform float centerY: hint_range(-1.5, 1.5) = 0.0;

// Precomputed constants
const vec2 C = vec2(1.0/6.0, 1.0/3.0);
const vec4 D = vec4(0.0, 0.5, 1.0, 2.0);
const float INV_289 = 1.0 / 289.0;
const float n_ = 0.142857142857;

// Optimized mod289 for vec3
vec3 mod289(vec3 x) {
    return x - floor(x * INV_289) * 289.0;
}

// Optimized mod289 for vec4
vec4 mod289_v4(vec4 x) {
    return x - floor(x * INV_289) * 289.0;
}

// Optimized permute function
vec4 permute(vec4 x) {
    return mod289_v4(((x * 34.0) + 1.0) * x);
}

// Optimized inverse square root
vec4 taylorInvSqrt(vec4 r) {
    return 1.79284291400159 - 0.85373472095314 * r;
}

// Optimized 3D noise function
float snoise(vec3 v) {
    vec3 i = floor(v + dot(v, C.yyy));
    vec3 x0 = v - i + dot(i, C.xxx);

    vec3 g = step(x0.yzx, x0.xyz);
    vec3 l = 1.0 - g;
    vec3 i1 = min(g.xyz, l.zxy);
    vec3 i2 = max(g.xyz, l.zxy);

    vec3 x1 = x0 - i1 + C.xxx;
    vec3 x2 = x0 - i2 + C.yyy;
    vec3 x3 = x0 - D.yyy;

    i = mod289(i);
    vec4 p = permute(permute(permute(
        i.z + vec4(0.0, i1.z, i2.z, 1.0))
        + i.y + vec4(0.0, i1.y, i2.y, 1.0))
        + i.x + vec4(0.0, i1.x, i2.x, 1.0));

    vec3 ns = n_ * D.wyz - D.xzx;
    vec4 j = p - 49.0 * floor(p * ns.z * ns.z);

    vec4 x_ = floor(j * ns.z);
    vec4 y_ = floor(j - 7.0 * x_);

    vec4 x = x_ * ns.x + ns.yyyy;
    vec4 y = y_ * ns.x + ns.yyyy;
    vec4 h = 1.0 - abs(x) - abs(y);

    vec4 b0 = vec4(x.xy, y.xy);
    vec4 b1 = vec4(x.zw, y.zw);

    vec4 s0 = floor(b0) * 2.0 + 1.0;
    vec4 s1 = floor(b1) * 2.0 + 1.0;
    vec4 sh = -step(h, vec4(0.0));

    vec4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
    vec4 a1 = b1.xzyw + s1.xzyw * sh.zzww;

    vec3 p0 = vec3(a0.xy, h.x);
    vec3 p1 = vec3(a0.zw, h.y);
    vec3 p2 = vec3(a1.xy, h.z);
    vec3 p3 = vec3(a1.zw, h.w);

    vec4 norm = taylorInvSqrt(vec4(dot(p0,p0), dot(p1,p1), dot(p2,p2), dot(p3,p3)));
    p0 *= norm.x;
    p1 *= norm.y;
    p2 *= norm.z;
    p3 *= norm.w;

    vec4 m = max(0.6 - vec4(dot(x0,x0), dot(x1,x1), dot(x2,x2), dot(x3,x3)), 0.0);
    m = m * m;
    return 42.0 * dot(m * m, vec4(dot(p0,x0), dot(p1,x1), dot(p2,x2), dot(p3,x3)));
}

// Optimized FBM function
float fbm3d(vec3 x) {
    float v = 0.0;
    float a = 0.5;
    vec3 shift = vec3(100.0);

    for (int i = 0; i < 8; ++i) {
        if (i >= octaves) break;
        v += a * snoise(x);
        x = x * 2.0 + shift;
        a *= 0.5;
    }
    return v;
}

void fragment() {
    float t = TIME * time_scale;

    // Precompute screen dimensions and center offsets
    vec2 screen = vec2(1920.0, 1080.0);
    vec2 center_offset = vec2(
        centerX * screen.x * 0.5,
        centerY * screen.y * 0.5
    );

    // Optimize coordinate calculation
    vec2 centeredUv = (FRAGCOORD.xy - vec2(screen.x * 0.5 + center_offset.x,
                                          screen.y * 0.5 - center_offset.y))
                      * (noise_scale / screen.y);

    // Compute polar coordinates
    float len = length(centeredUv) * 1.5;
    float angle = atan(centeredUv.y, centeredUv.x);
    angle += len * 1.1;

    // Optimize sine/cosine calculations
    vec2 sincos_angle = vec2(sin(angle), cos(angle));
    vec2 sincos_inv_angle = vec2(sin(1.0 - angle), cos(1.0 - angle));

    float pow_len_1 = pow(len, 0.3);
    float pow_len_2 = pow(len, 0.5);

    // Compute noise
    float x = fbm3d(vec3(sincos_angle.x, sincos_angle.y, pow_len_1 + t * 0.1));
    float y = fbm3d(vec3(sincos_inv_angle.y, sincos_inv_angle.x, pow_len_2 + t * 0.1));

    float r = fbm3d(vec3(x, y, len + t * 0.3));
    r = fbm3d(vec3(r - x, r - y, r + t * 0.3));

    // Compute final color value
    float c = (r + len * 5.0) / 6.0 * noise_intensity;

    // Optimized color mixing
    vec4 color;
    if (c < black_threshold) {
        color = black_color;
    } else if (c < red_threshold) {
        float t = smoothstep(black_threshold, red_threshold, c);
        color = mix(black_color, red_color, t);
    } else if (c < white_threshold) {
        float t = smoothstep(red_threshold, white_threshold, c);
        color = mix(red_color, white_color, t);
    } else {
        color = white_color;
    }

    COLOR = color;
}