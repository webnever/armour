using Godot;

public partial class MoveVal : Node3D
{
    [Export]
    public float Speed { get; set; } = 5.0f; // Units per second

    [Export]
    public float RaycastHeight { get; set; } = 10.0f; // Height above node to start raycast

    [Export]
    public float RaycastLength { get; set; } = 50.0f; // How far down to raycast

    public override void _Process(double delta)
    {
        // Move the node along the Z axis
        Translate(new Vector3(0, 0, (float)(Speed * delta)));

        // Raycast down from above the node to find the collider below
        var spaceState = GetWorld3D().DirectSpaceState;
        Vector3 rayStart = GlobalPosition + new Vector3(0, RaycastHeight, 0);
        Vector3 rayEnd = GlobalPosition + new Vector3(0, -RaycastLength, 0);

        var query = PhysicsRayQueryParameters3D.Create(rayStart, rayEnd);
        query.CollideWithAreas = false;
        query.CollideWithBodies = true;

        var result = spaceState.IntersectRay(query);

        if (result.Count > 0)
        {
            Vector3 hitPos = (Vector3)result["position"];
            GlobalPosition = new Vector3(GlobalPosition.X, hitPos.Y, GlobalPosition.Z);
        }
    }
}