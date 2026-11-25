using OpenTK.Mathematics;

namespace Egc;

/// <summary>
/// Camera fixă: stă într-un punct și privește spre o țintă. Fiecare scenă își alege
/// poziția și ținta (Scene.CameraPosition, Scene.CameraTarget). Clasa este completă.
/// În acest laborator mouse-ul și tastatura nu mișcă camera: le folosim pentru animații.
/// </summary>
public sealed class FixedCamera
{
    public Vector3 Position = new(0.0f, 6.0f, 12.0f);
    public Vector3 Target = Vector3.Zero;

    public float FieldOfView = 50.0f;      // FOV vertical, în grade (laboratorul 5)
    public float Near = 0.1f;
    public float Far = 200.0f;
    public float AspectRatio { get; private set; } = 16.0f / 10.0f;

    public Matrix4 ViewMatrix => Matrix4.LookAt(Position, Target, Vector3.UnitY);

    public Matrix4 ProjectionMatrix =>
        Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(FieldOfView), AspectRatio, Near, Far);

    /// <summary>Apelată din Game.OnResize. La minimizare (0 × 0) păstrăm aspectul vechi.</summary>
    public void Resize(int width, int height)
    {
        if (width > 0 && height > 0)
        {
            AspectRatio = width / (float)height;
        }
    }
}
