using OpenTK.Mathematics;

namespace Egc;

/// <summary>
/// Un obiect din scenă: o formă (Mesh), transformarea ei (Transform3D) și o nuanță.
/// Față de laboratorul 4: transformarea este 3D, iar Spin rotește obiectul continuu.
/// </summary>
public sealed class SceneObject
{
    public string Name { get; }
    public Mesh Mesh { get; }
    public Transform3D Transform { get; } = new();
    public Color4 Tint = Color4.White;   // se înmulțește cu culorile vârfurilor (alb = neschimbat)
    public Vector3 Spin = Vector3.Zero;  // grade pe secundă în jurul axelor X, Y, Z (0 = nemișcat)

    public SceneObject(string name, Mesh mesh)
    {
        Name = name;
        Mesh = mesh;
    }

    /// <summary>Animația: unghiurile cresc proporțional cu timpul scurs (dt, în secunde).</summary>
    public void Update(float dt)
    {
        Transform.Rotation += Spin * dt;
    }

    // Obs.: aceeași formă (Mesh) poate fi folosită de mai multe obiecte (de exemplu toți stâlpii
    // folosesc un singur cub), de aceea Game eliberează formele separat, nu prin SceneObject.
}
