shader_type canvas_item;

// FBM implementation from
// https://github.com/MaxBittker/glsl-fractal-brownian-noise

uniform vec4 black_color: source_color = vec4(0.0, 0.0, 0.0, 1.0);
uniform vec4 red_color: source_color = vec4(1.0, 0.0, 0.0, 1.0);
uniform vec4 white_color: source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float black_threshold: hint_range(0.0, 1.0) = 0.3;
uniform float red_threshold: hint_range(0.0, 1.0) = 0.6;
uniform float white_threshold: hint_range(0.0, 1.0) = 0.8;
uniform float noise_scale: hint_range(0.1, 5.0) = 1.0;
uniform float noise_intensity: hint_range(0.0, 10.0) = 1.0;
uniform float time_scale: hint_range(0.1, 5.0) = 1.0;
uniform int octaves: hint_range(1, 8) = 3;

vec3 mod289(vec3 x) {
  return x - floor(x * (1.0 / 289.0)) * 289.0;
}

vec4 mod2891(vec4 x) {
  return x - floor(x * (1.0 / 289.0)) * 289.0;
}

vec4 permute(vec4 x) {
     return mod2891(((x*34.0)+1.0)*x);
}

vec4 taylorInvSqrt(vec4 r)
{
  return 1.79284291400159 - 0.85373472095314 * r;
}

float snoise(vec3 v)
  {
  const vec2  C = vec2(1.0/6.0, 1.0/3.0) ;
  const vec4  D = vec4(0.0, 0.5, 1.0, 2.0);

// First corner
  vec3 i  = floor(v + dot(v, C.yyy) );
  vec3 x0 =   v - i + dot(i, C.xxx) ;

// Other corners
  vec3 g = step(x0.yzx, x0.xyz);
  vec3 l = 1.0 - g;
  vec3 i1 = min( g.xyz, l.zxy );
  vec3 i2 = max( g.xyz, l.zxy );

  //   x0 = x0 - 0.0 + 0.0 * C.xxx;
  //   x1 = x0 - i1  + 1.0 * C.xxx;
  //   x2 = x0 - i2  + 2.0 * C.xxx;
  //   x3 = x0 - 1.0 + 3.0 * C.xxx;
  vec3 x1 = x0 - i1 + C.xxx;
  vec3 x2 = x0 - i2 + C.yyy; // 2.0*C.x = 1/3 = C.y
  vec3 x3 = x0 - D.yyy;      // -1.0+3.0*C.x = -0.5 = -D.y

// Permutations
  i = mod289(i);
  vec4 p = permute( permute( permute(
             i.z + vec4(0.0, i1.z, i2.z, 1.0 ))
           + i.y + vec4(0.0, i1.y, i2.y, 1.0 ))
           + i.x + vec4(0.0, i1.x, i2.x, 1.0 ));

// Gradients: 7x7 points over a square, mapped onto an octahedron.
// The ring size 17*17 = 289 is close to a multiple of 49 (49*6 = 294)
  float n_ = 0.142857142857; // 1.0/7.0
  vec3  ns = n_ * D.wyz - D.xzx;

  vec4 j = p - 49.0 * floor(p * ns.z * ns.z);  //  mod(p,7*7)

  vec4 x_ = floor(j * ns.z);
  vec4 y_ = floor(j - 7.0 * x_ );    // mod(j,N)

  vec4 x = x_ *ns.x + ns.yyyy;
  vec4 y = y_ *ns.x + ns.yyyy;
  vec4 h = 1.0 - abs(x) - abs(y);

  vec4 b0 = vec4( x.xy, y.xy );
  vec4 b1 = vec4( x.zw, y.zw );

  //vec4 s0 = vec4(lessThan(b0,0.0))*2.0 - 1.0;
  //vec4 s1 = vec4(lessThan(b1,0.0))*2.0 - 1.0;
  vec4 s0 = floor(b0)*2.0 + 1.0;
  vec4 s1 = floor(b1)*2.0 + 1.0;
  vec4 sh = -step(h, vec4(0.0));

  vec4 a0 = b0.xzyw + s0.xzyw*sh.xxyy ;
  vec4 a1 = b1.xzyw + s1.xzyw*sh.zzww ;

  vec3 p0 = vec3(a0.xy,h.x);
  vec3 p1 = vec3(a0.zw,h.y);
  vec3 p2 = vec3(a1.xy,h.z);
  vec3 p3 = vec3(a1.zw,h.w);

//Normalise gradients
  vec4 norm = taylorInvSqrt(vec4(dot(p0,p0), dot(p1,p1), dot(p2, p2), dot(p3,p3)));
  p0 *= norm.x;
  p1 *= norm.y;
  p2 *= norm.z;
  p3 *= norm.w;

// Mix final noise value
  vec4 m = max(0.6 - vec4(dot(x0,x0), dot(x1,x1), dot(x2,x2), dot(x3,x3)), 0.0);
  m = m * m;
  return 42.0 * dot( m*m, vec4( dot(p0,x0), dot(p1,x1),
                                dot(p2,x2), dot(p3,x3) ) );
  }



float fbm3d(vec3 x) {
    float v = 0.0;
    float a = 0.5;
    vec3 shift = vec3(100);
    int iterations = octaves;

    for (int i = 0; i < 32; ++i) {
        if(i < iterations) {
            v += a * snoise(x);
            x = x * 2.0 + shift;
            a *= 0.5;
        }
    }
    return v;
}

void fragment()
{
   float t = TIME * time_scale;
    
    // Flip the y-coordinate
    vec2 uv = vec2(FRAGCOORD.x, 1080.0 - FRAGCOORD.y) * noise_scale / 1080.0;
    vec2 st = vec2(
        length(uv) * 1.5,
        atan(uv.y, uv.x)
    );
    
    st.y += st.x * 1.1;
        
    float x = fbm3d(
        vec3(
            sin( st.y ),
            cos( st.y ),
            pow( st.x, .3 ) + t * .1
        )
    );
    float y = fbm3d(
        vec3(
            cos( 1. - st.y ),
            sin( 1. - st.y ),
            pow( st.x, .5 ) + t * .1
        )
    );
    
    float r = fbm3d(
        vec3(
            x,
            y,
            st.x + t * .3
        )
    );
    r = fbm3d(
        vec3(
            r - x,
            r - y,
            r + t * .3
        )
    );
    
    float c = (r + st.x * 5.) / 6. * noise_intensity;

    vec4 color;
    if (c < black_threshold) {
        // Use the chosen black color with its alpha
        color = black_color;
    } else if (c < red_threshold) {
        // Transition from black to red color
        float mixFactor = smoothstep(black_threshold, red_threshold, c);
        color = mix(black_color, red_color, mixFactor);
    } else if (c < white_threshold) {
        // Transition from red to white color
        float mixFactor = smoothstep(red_threshold, white_threshold, c);
        color = mix(red_color, white_color, mixFactor);
    } else {
        // Use the chosen white color with its alpha
        color = white_color;
    }

    COLOR = color;
}