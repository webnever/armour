using Godot;
using System;

[Tool]
public partial class DesertTerrainGenerator : Node3D
{
    [Export]
    public Vector2 TerrainSize { get; set; } = new Vector2(100, 100);
    
    [Export]
    public float TerrainHeight { get; set; } = 15.0f;
    
    [Export]
    public float NoiseScale { get; set; } = 50.0f;
    
    [Export]
    public float DuneSharpness { get; set; } = 2.5f;
    
    [Export]
    public string SavePath { get; set; } = "res://scenes/desert_terrain.tscn";

    [Export(PropertyHint.Range, "4,200,1")]  // Min 4, Max 200, Step 1
    public int VertexDensity { get; set; } = 100;  // Controls the detail level of the mesh

    private bool _generate;
    [ExportGroup("Generation")]
    [Export]
    public bool Generate
    {
        get => _generate;
        set
        {
            _generate = value;
            if (value)
            {
                if (_noise == null)
                {
                    InitializeNoise();
                }
                GenerateAndSave();
            }
        }
    }

    private FastNoiseLite _noise;

    public override void _Ready()
    {
        InitializeNoise();
    }

    private void InitializeNoise()
    {
        _noise = new FastNoiseLite();
        _noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
        _noise.Seed = (int)GD.Randi();
        _noise.Frequency = 0.01f;
        _noise.FractalOctaves = 4;
        _noise.FractalLacunarity = 2.0f;
        _noise.FractalGain = 0.5f;
    }

    private void GenerateAndSave()
    {
        string directory = SavePath.GetBaseDir();
        if (!DirAccess.DirExistsAbsolute(directory))
        {
            DirAccess.MakeDirRecursiveAbsolute(directory);
        }

        var meshInstance = GenerateTerrain();
        
        var sceneRoot = new Node3D();
        sceneRoot.Name = "DesertTerrain";
        
        sceneRoot.AddChild(meshInstance);
        meshInstance.Owner = sceneRoot;
        
        var packedScene = new PackedScene();
        packedScene.Pack(sceneRoot);
        
        Error error = ResourceSaver.Save(packedScene, SavePath);
        if (error == Error.Ok)
            GD.Print($"Terrain saved successfully to: {SavePath}");
        else
            GD.Print($"Error saving terrain: {error}");
    }

    private MeshInstance3D GenerateTerrain()
    {
        var surfaceTool = new SurfaceTool();
        surfaceTool.Begin(Mesh.PrimitiveType.Triangles);
        
        // Use VertexDensity parameter instead of hardcoded value
        int gridSize = VertexDensity;
        var vertices = new Vector3[gridSize * gridSize];
        var indices = new int[(gridSize - 1) * (gridSize - 1) * 6];
        var indexCounter = 0;
        
        // Calculate vertex spacing based on density
        float vertexSpacing = 1.0f / (gridSize - 1);
        
        for (int z = 0; z < gridSize; z++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                var percent = new Vector2(x, z) * vertexSpacing;
                var pos = new Vector3(
                    percent.X * TerrainSize.X - TerrainSize.X * 0.5f,
                    0,
                    percent.Y * TerrainSize.Y - TerrainSize.Y * 0.5f
                );
                
                float duneNoise = _noise.GetNoise2D(pos.X * NoiseScale, pos.Z * NoiseScale);
                float detailNoise = _noise.GetNoise2D(pos.X * NoiseScale * 4, pos.Z * NoiseScale * 4) * 0.2f;
                
                float height = Mathf.Pow(Mathf.Abs(duneNoise), DuneSharpness) * Mathf.Sign(duneNoise);
                height = height * TerrainHeight + detailNoise * TerrainHeight * 0.3f;
                
                pos.Y = height;
                
                var uv = new Vector2(percent.X, percent.Y);
                surfaceTool.SetUV(uv);
                surfaceTool.AddVertex(pos);
                
                vertices[z * gridSize + x] = pos;
                
                if (x < gridSize - 1 && z < gridSize - 1)
                {
                    int current = z * gridSize + x;
                    int next = current + 1;
                    int down = current + gridSize;
                    int downNext = down + 1;
                    
                    // First triangle
                    surfaceTool.AddIndex(current);
                    surfaceTool.AddIndex(down);
                    surfaceTool.AddIndex(next);
                    
                    indices[indexCounter++] = current;
                    indices[indexCounter++] = down;
                    indices[indexCounter++] = next;
                    
                    // Second triangle
                    surfaceTool.AddIndex(next);
                    surfaceTool.AddIndex(down);
                    surfaceTool.AddIndex(downNext);
                    
                    indices[indexCounter++] = next;
                    indices[indexCounter++] = down;
                    indices[indexCounter++] = downNext;
                }
            }
        }
        
        surfaceTool.GenerateNormals();
        surfaceTool.GenerateTangents();
        var mesh = surfaceTool.Commit();
        
        var meshInstance = new MeshInstance3D();
        meshInstance.Name = "TerrainMesh";
        meshInstance.Mesh = mesh;
        
        var material = new StandardMaterial3D();
        material.ResourceName = "DesertMaterial";
        material.AlbedoColor = new Color(0.76f, 0.698f, 0.502f);
        material.Roughness = 1.0f;
        material.Metallic = 0.0f;
        
        string materialPath = SavePath.GetBaseDir() + "/desert_material.tres";
        ResourceSaver.Save(material, materialPath);
        
        meshInstance.MaterialOverride = material;
        
        return meshInstance;
    }
}