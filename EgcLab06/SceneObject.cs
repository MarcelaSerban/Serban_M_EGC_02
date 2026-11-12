using OpenTK.Mathematics;

namespace Egc;

/// <summary>
/// Un obiect din scenă: o formă (Mesh), transformarea ei (Transform3D) și o nuanță.
/// Față de laboratorul 5: obiectul poate avea un PĂRINTE (secțiunea 6). Transformarea lui este
/// atunci relativă la părinte: dacă părintele se mișcă sau se rotește, copilul îl urmează.
/// Un obiect fără formă (Mesh = null) este un PIVOT: nu se vede, dar își poartă copiii.
/// </summary>
public sealed class SceneObject
{
    public string Name { get; }
    public Mesh? Mesh { get; }
    public Transform3D Transform { get; } = new();
    public Color4 Tint = Color4.White;   // se înmulțește cu culorile vârfurilor (alb = neschimbat)
    public Vector3 Spin = Vector3.Zero;  // grade pe secundă în jurul axelor X, Y, Z (0 = nemișcat)
    public bool Visible = true;

    /// <summary>Părintele (null = obiectul este așezat direct în lume).</summary>
    public SceneObject? Parent { get; set; }

    public SceneObject(string name, Mesh? mesh, SceneObject? parent = null)
    {
        Name = name;
        Mesh = mesh;
        Parent = parent;
    }

    /// <summary>
    /// Matricea care duce obiectul din object space în world space.
    /// Fără părinte este chiar matricea model. Cu părinte: întâi transformarea proprie
    /// (față de părinte), apoi transformarea părintelui față de lume.
    /// </summary>
    public Matrix4 WorldMatrix
    {
        get
        {
            // TODO 6.6* (facultativ): ModelMatrix * Parent.WorldMatrix, dacă există părinte.
            // (În OpenTK matricele se înmulțesc în ordinea aplicării, de la stânga la dreapta.)
            // provizoriu: părintele este ignorat
            return Transform.ModelMatrix;
        }
    }

    /// <summary>Poziția obiectului în lume (ultima linie a matricei WorldMatrix).</summary>
    public Vector3 WorldPosition => WorldMatrix.Row3.Xyz;

    /// <summary>Animația continuă: unghiurile cresc proporțional cu timpul de joc (dt, în secunde).</summary>
    public void Update(float dt)
    {
        Transform.Rotation += Spin * dt;
    }
}
