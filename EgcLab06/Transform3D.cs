using OpenTK.Mathematics;

namespace Egc;

/// <summary>
/// Poziția, rotația și scalarea unui obiect 3D. Este Transform2D din laboratorul 4, extinsă la 3D:
/// poziția și scalarea au trei componente, iar rotația are câte un unghi pentru fiecare axă.
/// Clasa este completă.
/// </summary>
public class Transform3D
{
    public Vector3 Position = Vector3.Zero;   // translația, în unități de lume
    public Vector3 Rotation = Vector3.Zero;   // unghiurile în GRADE, în jurul axelor X, Y și Z
    public Vector3 Scale = Vector3.One;       // factorii de scalare pe x, y și z

    /// <summary>
    /// Matricea model: scalare, apoi rotațiile (X, apoi Y, apoi Z), apoi translație.
    /// În OpenTK matricele se înmulțesc în ordinea aplicării (de la stânga la dreapta).
    /// </summary>
    public Matrix4 ModelMatrix =>
        Matrix4.CreateScale(Scale)
      * Matrix4.CreateRotationX(MathHelper.DegreesToRadians(Rotation.X))
      * Matrix4.CreateRotationY(MathHelper.DegreesToRadians(Rotation.Y))
      * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(Rotation.Z))
      * Matrix4.CreateTranslation(Position);
}
