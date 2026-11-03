using OpenTK.Mathematics;

namespace Egc;

/// <summary>Un obiect din scenă: o formă (Mesh), transformarea ei (Transform2D) și o nuanță.</summary>
public sealed class SceneObject : IDisposable
{
    public string Name { get; }
    public Mesh Mesh { get; }
    public Transform2D Transform { get; set; }
    public Color4 Tint = Color4.White;   // se înmulțește cu culorile vârfurilor (alb = neschimbat)

    public SceneObject(string name, Mesh mesh, Transform2D transform)
    {
        Name = name;
        Mesh = mesh;
        Transform = transform;
    }

    public void Dispose() => Mesh.Dispose();
}
