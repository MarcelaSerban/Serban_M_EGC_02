using OpenTK.Mathematics;

namespace Egc;

/// <summary>
/// Un obiect din scenă: o formă (Mesh), transformarea ei (Transform3D), o nuanță și,
/// NOU în laboratorul 7, un MATERIAL: programul shader și uniformele cu care se desenează.
/// Fără material, obiectul se desenează cu shaderul de bază (culoarea vârfurilor × Tint).
/// Obiectul poate avea un părinte (laboratorul 6, secțiunea 6).
/// </summary>
public sealed class SceneObject
{
    public string Name { get; }
    public Mesh? Mesh { get; }
    public Transform3D Transform { get; } = new();
    public Color4 Tint = Color4.White;   // uniforma uTint: se înmulțește cu culorile vârfurilor
    public Vector3 Spin = Vector3.Zero;  // grade pe secundă în jurul axelor X, Y, Z
    public bool Visible = true;

    /// <summary>NOU: materialul (null = shaderul de bază al scenei).</summary>
    public Material? Material { get; set; }

    /// <summary>Părintele (null = obiectul este așezat direct în lume).</summary>
    public SceneObject? Parent { get; set; }

    public SceneObject(string name, Mesh? mesh, SceneObject? parent = null)
    {
        Name = name;
        Mesh = mesh;
        Parent = parent;
    }

    /// <summary>Matricea object space → world space: întâi transformarea proprie, apoi a părintelui.</summary>
    public Matrix4 WorldMatrix
    {
        get
        {
            Matrix4 local = Transform.ModelMatrix;
            return Parent == null ? local : local * Parent.WorldMatrix;
        }
    }

    /// <summary>Animația continuă: unghiurile cresc proporțional cu timpul de joc.</summary>
    public void Update(float dt)
    {
        Transform.Rotation += Spin * dt;
    }
}
