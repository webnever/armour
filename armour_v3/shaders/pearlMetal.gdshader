shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_disabled, diffuse_burley, specular_schlick_ggx;

varying vec3 worldPos;
varying vec3 worldNormal;
varying vec3 worldEye;

group_uniforms color;
	uniform vec3 baseColor: source_color = vec3(1.0, 1.0, 1.0);
	uniform float transparency: hint_range(0.0, 1.0) = 0.8;
	uniform float metallic: hint_range(0.0, 1.0) = 0.1;
	uniform float roughness: hint_range(0.0, 1.0) = 0.2;
group_uniforms;

group_uniforms gradientBasis;
	uniform vec3 gradientBasis = vec3(3.0, 3.0, 3.0);
	uniform vec3 gradientBasisDistort = vec3(0.0, 0.0, 0.0);
	uniform float gradientBasisSpeed = 0.0;
group_uniforms;

group_uniforms refraction;
	uniform float refractionSplit = 2.0;
	uniform float refractionSplitPower: hint_range(0.01, 10.0) = 1.0;
	uniform float refractionAffect = 0.8;
group_uniforms;

group_uniforms reflection;
	uniform float reflectionStrength: hint_range(0.0, 1.0) = 0.5;
	uniform float fresnelPower: hint_range(0.1, 5.0) = 2.0;
	uniform samplerCube environment: hint_default_black;
group_uniforms;

void vertex() {
	// Use the built-in smooth normals from Godot
	worldNormal = normalize(NORMAL);
	worldPos = VERTEX;
	worldEye = (INV_VIEW_MATRIX * vec4(0.0, 0.0, 0.0, 1.0)).xyz;
}

void fragment() {
	// Get world space position and normal
	vec3 worldPosition = (MODEL_MATRIX * vec4(worldPos, 1.0)).xyz;
	vec3 worldSpaceNormal = normalize((MODEL_MATRIX * vec4(worldNormal, 0.0)).xyz);
	
	// Get the direction we are looking towards
	vec3 lookDir = normalize(worldPosition - worldEye);
	
	// Get the dot product between the look direction and the normal
	float lookDot = pow(
		abs(dot(lookDir, worldSpaceNormal)),
		refractionSplitPower
	);
	
	// Get the reflection direction
	vec3 reflectionDir = reflect(lookDir, worldSpaceNormal);
	
	// Assemble the gradient
	vec3 gradientDirection = gradientBasis * reflectionDir;
	
	if (gradientBasisDistort != vec3(0.0)) {
		gradientDirection += sin(gradientBasisDistort * PI * reflectionDir);
	}
	
	float gradient = (
		  gradientDirection.x
		+ gradientDirection.y
		+ gradientDirection.z
		+ gradientBasisSpeed * TIME
	);
	
	// The final sine wave for refraction effect
	vec3 refraction = 0.5 + 0.5 * sin(
		  vec3(-1.0, 0.0, 1.0)
		* (1.0 - lookDot)
		* refractionSplit
		+ gradient
	);
	
	// Construct the base color with refraction
	vec3 color = baseColor * (1.0 - refraction * refractionAffect);
	
	// Calculate Fresnel effect for reflections
	float fresnel = pow(1.0 - max(dot(-lookDir, worldSpaceNormal), 0.0), fresnelPower);
	
	// Sample environment map for reflections
	vec3 envReflection = textureLod(environment, reflectionDir, roughness * 7.0).rgb;
	
	// Mix base color with environment reflection
	color = mix(color, envReflection, fresnel * reflectionStrength);
	
	// Set material properties
	ALBEDO = color;
	ALPHA = transparency;
	METALLIC = metallic;
	ROUGHNESS = roughness;
	
	// Add some emission based on the refraction pattern for extra visual interest
	EMISSION = refraction * 0.1 * baseColor;
}