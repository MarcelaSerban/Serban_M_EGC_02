using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

/// <summary>
/// Camera orbită: privește mereu spre o țintă și se mișcă pe o sferă în jurul ei.
/// Poziția este dată de distanță (raza) și de două unghiuri: azimut (în jurul axei Y) și elevație.
/// Comenzi: clic stânga ținut + mouse sau săgeți = rotație; rotița = distanța; W A S D = mută ținta (bonus).
/// </summary>
public class OrbitCamera : Camera
{
    public Vector3 Target { get; private set; }
    public float Distance { get; private set; }
    public float Azimuth { get; private set; }     // grade
    public float Elevation { get; private set; }   // grade, limitat la ±MaxElevation

    public const float MaxElevation = 89.0f;       // ca pitch-ul: la ±90° "sus" ar fi paralel cu privirea
    public const float MinDistance = 1.5f;
    public const float MaxDistance = 40.0f;

    public float MouseSensitivity = 0.3f;          // grade pe pixel
    public float KeySpeed = 90.0f;                 // grade pe secundă (săgeți)
    public float ZoomFactor = 0.9f;                // un pas al rotiței înmulțește distanța cu 0,9
    public float TargetSpeed = 3.0f;               // unități de lume pe secundă (W A S D)

    private static readonly Vector3 StartTarget = new(0.0f, 1.0f, 0.0f);
    private const float StartDistance = 8.0f;
    private const float StartAzimuth = 90.0f;      // camera pe axa +Z, privind spre -Z
    private const float StartElevation = 20.0f;

    public OrbitCamera()
    {
        Reset();
    }

    public override string Name => "Orbită";

    /// <summary>Poziția = țintă + distanță · (cos el · cos az, sin el, cos el · sin az).</summary>
    public override Vector3 Position
    {
        get
        {
            float az = MathHelper.DegreesToRadians(Azimuth);
            float el = MathHelper.DegreesToRadians(Elevation);
            return Target + Distance * new Vector3(
                MathF.Cos(el) * MathF.Cos(az),
                MathF.Sin(el),
                MathF.Cos(el) * MathF.Sin(az));
        }
    }

    public override Vector3 Front => Vector3.Normalize(Target - Position);

    public override Matrix4 ViewMatrix => Matrix4.LookAt(Position, Target, Vector3.UnitY);

    public override void Update(KeyboardState keyboard, MouseState mouse, Vector2 mouseDelta, float dt)
    {
        // Rotația: clic stânga ținut + mouse
        if (mouse.IsButtonDown(MouseButton.Left))
        {
            Azimuth -= mouseDelta.X * MouseSensitivity;
            Elevation += mouseDelta.Y * MouseSensitivity;
        }

        // Rotația de la tastatură
        if (keyboard.IsKeyDown(Keys.Left)) Azimuth -= KeySpeed * dt;
        if (keyboard.IsKeyDown(Keys.Right)) Azimuth += KeySpeed * dt;
        if (keyboard.IsKeyDown(Keys.Up)) Elevation += KeySpeed * dt;
        if (keyboard.IsKeyDown(Keys.Down)) Elevation -= KeySpeed * dt;
        Elevation = Math.Clamp(Elevation, -MaxElevation, MaxElevation);

        // Rotița schimbă distanța (nu FOV-ul)
        if (mouse.ScrollDelta.Y != 0.0f)
        {
            Distance *= MathF.Pow(ZoomFactor, mouse.ScrollDelta.Y);
            Distance = Math.Clamp(Distance, MinDistance, MaxDistance);
        }

        // Bonus: W A S D mută ținta în planul orizontal, relativ la direcția privirii
        Vector3 forward = new Vector3(Front.X, 0.0f, Front.Z);
        if (forward.LengthSquared > 1e-6f) forward.Normalize();
        Vector3 right = Vector3.Cross(forward, Vector3.UnitY);

        Vector3 move = Vector3.Zero;
        if (keyboard.IsKeyDown(Keys.W)) move += forward;
        if (keyboard.IsKeyDown(Keys.S)) move -= forward;
        if (keyboard.IsKeyDown(Keys.D)) move += right;
        if (keyboard.IsKeyDown(Keys.A)) move -= right;
        if (move.LengthSquared > 1e-6f)
        {
            move.Normalize();
            Target += move * TargetSpeed * dt;
        }
    }

    /// <summary>
    /// Așază camera orbită astfel încât imaginea să nu sară: ținta este la <paramref name="distance"/>
    /// în fața camerei FPS; unghiurile rezultă din o = -front (elevație = asin(o.y), azimut = atan2(o.z, o.x)).
    /// </summary>
    public void SetFromView(Vector3 position, Vector3 front, float distance)
    {
        front.Normalize();
        Distance = Math.Clamp(distance, MinDistance, MaxDistance);
        Target = position + front * Distance;
        Vector3 o = -front;
        Elevation = Math.Clamp(MathHelper.RadiansToDegrees(MathF.Asin(o.Y)), -MaxElevation, MaxElevation);
        Azimuth = MathHelper.RadiansToDegrees(MathF.Atan2(o.Z, o.X));
    }

    public override void Reset()
    {
        Target = StartTarget;
        Distance = StartDistance;
        Azimuth = StartAzimuth;
        Elevation = StartElevation;
        FieldOfView = 60.0f;
    }

    public override IEnumerable<string> Describe()
    {
        Vector3 p = Position;
        yield return $"Țintă          : ({Target.X,6:F2}, {Target.Y,6:F2}, {Target.Z,6:F2})";
        yield return $"Distanță       : {Distance,6:F2}";
        yield return $"Azimut / elev. : {NormalizeAngle(Azimuth),7:F1}° / {Elevation,6:F1}°";
        yield return $"Poziție        : ({p.X,6:F2}, {p.Y,6:F2}, {p.Z,6:F2})";
    }

    private static float NormalizeAngle(float degrees)
    {
        degrees %= 360.0f;
        if (degrees > 180.0f) degrees -= 360.0f;
        if (degrees <= -180.0f) degrees += 360.0f;
        return degrees;
    }
}
