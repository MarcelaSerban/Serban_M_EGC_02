using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

/// <summary>
/// Clasa de bază a camerelor 3D. Conține ce au în comun toate camerele:
/// proiecția în perspectivă (FOV, near, far, raportul de aspect) și "contractul" pe care
/// trebuie să-l respecte fiecare cameră concretă: poziție, direcția privirii, matricea view,
/// reacția la tastatură și mouse.
/// Camera FPS (FpsCamera) se scrie în laborator; camera orbită (OrbitCamera) este tema.
/// </summary>
public abstract class Camera
{
    // --- Projection (secțiunea 4) ---------------------------------------------

    public const float MinFov = 10.0f;     // grade
    public const float MaxFov = 120.0f;    // grade

    /// <summary>Câmpul vizual VERTICAL, în grade (unghiul de sus până jos al imaginii).</summary>
    public float FieldOfView = 60.0f;

    public float Near = 0.1f;              // planul apropiat: ce e mai aproape nu se desenează
    public float Far = 100.0f;             // planul depărtat: ce e mai departe nu se desenează

    /// <summary>Raportul lățime / înălțime al ferestrei.</summary>
    public float AspectRatio { get; private set; } = 4.0f / 3.0f;

    /// <summary>Matricea de proiecție în perspectivă.</summary>
    public Matrix4 ProjectionMatrix =>
        Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(FieldOfView), AspectRatio, Near, Far);

    /// <summary>Câmpul vizual ORIZONTAL, în grade, care rezultă din cel vertical și din raportul de aspect.</summary>
    public float HorizontalFov
    {
        get
        {
            // hFOV = 2 · atan( tan(vFOV / 2) · aspect ), calculat în radiani, returnat în grade
            float vFovRad = MathHelper.DegreesToRadians(FieldOfView);
            float hFovRad = 2.0f * MathF.Atan(MathF.Tan(vFovRad / 2.0f) * AspectRatio);
            return MathHelper.RadiansToDegrees(hFovRad);
        }
    }

    /// <summary>Apelată din Game.OnResize: reține noul raport de aspect.</summary>
    public void Resize(int width, int height)
    {
        // La minimizare fereastra are 0 × 0 pixeli; un aspect 0 ar face proiecția invalidă.
        if (width <= 0 || height <= 0)
        {
            return;
        }
        AspectRatio = width / (float)height;
    }

    /// <summary>Modifică FOV-ul cu <paramref name="deltaDegrees"/> și îl păstrează între MinFov și MaxFov.</summary>
    public void ChangeFov(float deltaDegrees)
    {
        FieldOfView = Math.Clamp(FieldOfView + deltaDegrees, MinFov, MaxFov);
    }

    // --- Ce trebuie să ofere fiecare cameră concretă -----------------------------

    /// <summary>Numele afișat în titlul ferestrei și în consolă.</summary>
    public abstract string Name { get; }

    /// <summary>Poziția camerei (ochiul), în coordonate de lume.</summary>
    public abstract Vector3 Position { get; }

    /// <summary>Direcția în care privește camera (vector unitate).</summary>
    public abstract Vector3 Front { get; }

    /// <summary>Matricea view: world space -> view space.</summary>
    public abstract Matrix4 ViewMatrix { get; }

    /// <summary>
    /// Apelată la fiecare cadru din Game.OnUpdateFrame.
    /// <paramref name="mouseDelta"/> = deplasarea mouse-ului față de cadrul anterior, în pixeli.
    /// <paramref name="dt"/> = timpul scurs de la cadrul anterior, în secunde.
    /// </summary>
    public abstract void Update(KeyboardState keyboard, MouseState mouse, Vector2 mouseDelta, float dt);

    /// <summary>Readuce camera în starea inițială (tasta R).</summary>
    public abstract void Reset();

    /// <summary>Rândurile cu starea camerei, afișate în consolă la tasta I.</summary>
    public abstract IEnumerable<string> Describe();
}
