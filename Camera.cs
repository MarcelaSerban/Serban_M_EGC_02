using OpenTK.Mathematics;

namespace Egc;

/// <summary>
/// Camera 2D: matricea view (secțiunea 6) și matricea projection (secțiunea 7).
/// Clasa este completă; o folosiți la exercițiile din clasă și la tema T3.
/// </summary>
public class Camera
{
    // --- View (secțiunea 6) ---------------------------------------------------

    public Vector2 Position = Vector2.Zero;   // punctul din lume aflat în centrul imaginii
    public float Zoom = 1.0f;                 // > 1: obiectele apar mai mari; < 1: mai mici
    public float Distance = 5.0f;             // distanța camerei față de planul scenei (contează în perspectivă)

    /// <summary>
    /// Matricea view = inversa transformării camerei: lumea se mută în sens invers față de cameră,
    /// apoi se scalează față de centrul imaginii (zoom).
    /// </summary>
    public Matrix4 ViewMatrix =>
        Matrix4.CreateTranslation(-Position.X, -Position.Y, -Distance)
      * Matrix4.CreateScale(Zoom, Zoom, 1.0f);

    // --- Projection (secțiunea 7) ---------------------------------------------

    public float ViewHeight = 4.0f;       // câte unități de lume se văd pe verticală (ortografic)
    public float FieldOfView = 45.0f;     // în grade (perspectivă)
    public bool IsPerspective = false;    // comutat cu tasta O

    /// <summary>Raportul lățime / înălțime al ferestrei.</summary>
    public float AspectRatio { get; private set; } = 4.0f / 3.0f;

    public Matrix4 ProjectionMatrix => IsPerspective ? Perspective() : Orthographic();

    /// <summary>Apelată din Game.OnResize: reține noul raport de aspect.</summary>
    public void Resize(int width, int height)
    {
        AspectRatio = width / (float)Math.Max(height, 1);
    }

    private Matrix4 Orthographic()
    {
        float h = ViewHeight / 2.0f;
        float w = h * AspectRatio;    // lățimea se adaptează la forma ferestrei
        return Matrix4.CreateOrthographicOffCenter(-w, w, -h, h, 0.1f, 100.0f);
    }

    private Matrix4 Perspective() =>
        Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(FieldOfView), AspectRatio, 0.1f, 100.0f);

    /// <summary>Readuce camera în starea inițială (tasta R).</summary>
    public void Reset()
    {
        Position = Vector2.Zero;
        Zoom = 1.0f;
        IsPerspective = false;
    }
}
