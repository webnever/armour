extends Node3D

class P4Vector:
	var x: float
	var y: float
	var z: float
	var w: float

	func _init(_x: float, _y: float, _z: float, _w: float):
		x = _x
		y = _y
		z = _z
		w = _w

	func to_vec3() -> Vector3:
		return Vector3(x, y, z)

func vec_to_matrix(v) -> Array:
	var m = []
	if v is Vector3:
		m.append([v.x])
		m.append([v.y])
		m.append([v.z])
	return m

# Explicitly defined vec4_to_matrix function
func vec4_to_matrix(v: P4Vector) -> Array:
	var m = vec_to_matrix(v.to_vec3())
	m.append([v.w])
	return m

func matrix_to_vec(m: Array) -> Vector3:
	return Vector3(m[0][0], m[1][0], m[2][0])

func matrix_to_vec4(m: Array) -> P4Vector:
	var r = P4Vector.new(m[0][0], m[1][0], m[2][0], 0)
	if m.size() > 3:
		r.w = m[3][0]
	return r

func matmul(a: Array, b: Array) -> Array:
	var colsA = a[0].size()
	var rowsA = a.size()
	var colsB = b[0].size()
	var rowsB = b.size()

	if colsA != rowsB:
		push_error("Columns of A must match rows of B")
		return []

	var result = []
	for j in range(rowsA):
		result.append([])
		for i in range(colsB):
			var sum = 0.0
			for n in range(colsA):
				sum += a[j][n] * b[n][i]
			result[j].append(sum)
	return result

func matmulvec(a: Array, vec: Vector3) -> Vector3:
	var m = vec_to_matrix(vec)
	var r = matmul(a, m)
	return matrix_to_vec(r)

func matmulvec4(a: Array, vec: P4Vector) -> P4Vector:
	var m = vec4_to_matrix(vec) # Ensure this call matches the defined function
	var r = matmul(a, m)
	return matrix_to_vec4(r)

var vertex_scene_path = "res://tscn_candle.tscn"
var vertex_instances = []
var angle = 0.0

# Define the 4D vertices of a hypercube
var vertices_4d = [
	P4Vector.new(-1, -1, -1, 1), P4Vector.new(1, -1, -1, 1), P4Vector.new(1, 1, -1, 1), P4Vector.new(-1, 1, -1, 1),
	P4Vector.new(-1, -1, 1, 1), P4Vector.new(1, -1, 1, 1), P4Vector.new(1, 1, 1, 1), P4Vector.new(-1, 1, 1, 1),
	P4Vector.new(-1, -1, -1, -1), P4Vector.new(1, -1, -1, -1), P4Vector.new(1, 1, -1, -1), P4Vector.new(-1, 1, -1, -1),
	P4Vector.new(-1, -1, 1, -1), P4Vector.new(1, -1, 1, -1), P4Vector.new(1, 1, 1, -1), P4Vector.new(-1, 1, 1, -1)
]

func _ready():
	instantiate_vertices()
	# Scale down the hypercube for better visualization
	scale = Vector3(0.5, 0.5, 0.5)

var angleXY = 0.0
var angleZW = 0.0

func _process(delta):
	angleXY += delta * 0.5 # Control the speed of rotation in the XY plane
	angleZW += delta * 0.5 # Control the speed of rotation in the ZW plane
	update_hypercube()

func update_hypercube():
	for i in range(vertices_4d.size()):
		var v = vertices_4d[i]
		# Apply 4D rotation
		var rotated = rotate_4d(v, angleXY, angleZW)
		# Project down to 3D (ignoring the 'w' component for simplicity)
		var projected = rotated.to_vec3()
		if i < vertex_instances.size():
			vertex_instances[i].global_transform.origin = projected * 1.0 # Adjust scale as needed

func instantiate_vertices():
	for i in range(vertices_4d.size()):
		var scene_instance = load(vertex_scene_path).instantiate()
		add_child(scene_instance)
		vertex_instances.append(scene_instance)

func rotate_4d(v: P4Vector, angleXY: float, angleZW: float) -> P4Vector:
	# Rotation in the XY plane
	var nx = v.x * cos(angleXY) - v.y * sin(angleXY)
	var ny = v.x * sin(angleXY) + v.y * cos(angleXY)
	
	# Rotation in the ZW plane
	var nz = v.z * cos(angleZW) - v.w * sin(angleZW)
	var nw = v.z * sin(angleZW) + v.w * cos(angleZW)
	
	return P4Vector.new(nx, ny, nz, nw)
