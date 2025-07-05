shader_type canvas_item;

uniform float cloudscale = 1.1;
uniform float speed = 0.01;
uniform float clouddark = 0.5;
uniform float cloudlight = 0.3;
uniform float cloudcover = 0.2;
uniform float cloudalpha = 8.0;
uniform float skytint = 0.5;
uniform vec3 skycolour1 = vec3(0.2, 0.4, 0.6);
uniform vec3 skycolour2 = vec3(0.4, 0.7, 1.0);

// Precomputed constants
const float K1 = 0.366025404;
const float K2 = 0.211324865;
const vec3 noise_scale = vec3(70.0);
const mat2 rotation = mat2(vec2(1.6,1.2), vec2(-1.2,1.6));

// Optimized hash function using dot product
vec2 hash(vec2 p) {
    vec2 dots = vec2(
        dot(p, vec2(127.1, 311.7)),
        dot(p, vec2(269.5, 183.3))
    );
    return -1.0 + 2.0 * fract(sin(dots) * 43758.5453123);
}

// Simplified noise function with fewer operations
float noise(vec2 p) {
    vec2 i = floor(p + (p.x + p.y) * K1);
    vec2 a = p - i + (i.x + i.y) * K2;
    vec2 o = step(a.yx, a.xy);
    vec2 b = a - o + K2;
    vec2 c = a - 1.0 + 2.0 * K2;

    vec3 h = max(0.5 - vec3(dot(a,a), dot(b,b), dot(c,c)), 0.0);
    h = h * h * h * h;

    return dot(h * vec3(
        dot(a, hash(i + 0.0)),
        dot(b, hash(i + o)),
        dot(c, hash(i + 1.0))
    ), noise_scale);
}

// Optimized fbm function with reduced iterations
float fbm(vec2 n) {
    float total = 0.0;
    float amplitude = 0.1;

    for (int i = 0; i < 5; i++) { // Reduced from 7 to 5 iterations
        total += noise(n) * amplitude;
        n = rotation * n;
        amplitude *= 0.4;
    }
    return total;
}

void fragment() {
    // Precompute common values
    vec2 res = 1.0 / SCREEN_PIXEL_SIZE;
    vec2 p = FRAGCOORD.xy / res.xy;
    vec2 uv = p * vec2(res.x/res.y, 1.0);
    float time = TIME * speed;

    // Compute base noise
    float q = fbm(uv * cloudscale * 0.5);

    // Compute ridged noise with fewer iterations
    float r = 0.0;
    vec2 uv_scaled = uv * cloudscale + q - time;
    float weight = 0.8;

    for (int i = 0; i < 6; i++) { // Reduced from 8 to 6 iterations
        r += abs(weight * noise(uv_scaled));
        uv_scaled = rotation * uv_scaled + time;
        weight *= 0.7;
    }

    // Compute main noise with fewer iterations
    float f = 0.0;
    uv_scaled = uv * cloudscale + q - time;
    weight = 0.7;

    for (int i = 0; i < 6; i++) { // Reduced from 8 to 6 iterations
        f += weight * noise(uv_scaled);
        uv_scaled = rotation * uv_scaled + time;
        weight *= 0.6;
    }

    f *= r + f;

    // Compute color noise with fewer iterations
    float c = 0.0;
    time *= 2.0;
    uv_scaled = uv * cloudscale * 2.0 + q - time;
    weight = 0.4;

    for (int i = 0; i < 5; i++) { // Reduced from 7 to 5 iterations
        c += weight * noise(uv_scaled);
        uv_scaled = rotation * uv_scaled + time;
        weight *= 0.6;
    }

    // Compute final color
    vec3 skycolour = mix(skycolour2, skycolour1, p.y);
    vec3 cloudcolour = vec3(1.1, 1.1, 0.9) * clamp(clouddark + cloudlight * c, 0.0, 1.0);
    f = cloudcover + cloudalpha * f * r;

    COLOR.rgb = mix(
        skycolour,
        clamp(skytint * skycolour + cloudcolour, 0.0, 1.0),
        clamp(f + c, 0.0, 1.0)
    );
}