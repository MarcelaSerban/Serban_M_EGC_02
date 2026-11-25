using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Egc;

/// <summary>
/// Fabrica de forme 3D (completă): fiecare metodă calculează vârfurile și indicii unei forme
/// și construiește un obiect Mesh. Formele sunt descrise în object space, cu centrul în origine.
/// NOU (laboratorul 7): fiecare vârf are și coordonatele de suprafață (u, v), în [0, 1]:
///  - Quad: (0, 0) în colțul stânga-jos, (1, 1) în colțul dreapta-sus;
///  - Cube: fiecare față are propriul pătrat (0, 0) .. (1, 1);
///  - Sphere: u în jurul sferei (longitudinea), v de la polul sud (0) la polul nord (1);
///  - Cylinder: pe lateral u în jurul axei, v de jos în sus; pe capace un disc centrat în (0.5, 0.5).
/// Regula sensului de parcurgere (laboratorul 5): privită din afară, fiecare față are vârfurile
/// în sens trigonometric.
/// </summary>
public static class Shapes
{
    /// <summary>
    /// Grila de pe podea (planul y = 0): linii paralele cu axele X și Z, din <paramref name="step"/>
    /// în <paramref name="step"/> unități, între -<paramref name="halfSize"/> și +<paramref name="halfSize"/>.
    /// </summary>
    public static Mesh Grid(int halfSize, float step, Color4 color)
    {
        int lines = (int)(2 * halfSize / step) + 1;
        var vertices = new float[lines * 4 * Mesh.FloatsPerVertex];
        var indices = new uint[lines * 4];
        int v = 0;
        for (int i = 0; i < lines; i++)
        {
            float c = -halfSize + i * step;
            SetVertex(vertices, v++, c, 0, -halfSize, color, 0, 0);
            SetVertex(vertices, v++, c, 0, halfSize, color, 0, 0);
            SetVertex(vertices, v++, -halfSize, 0, c, color, 0, 0);
            SetVertex(vertices, v++, halfSize, 0, c, color, 0, 0);
        }
        for (int i = 0; i < indices.Length; i++) indices[i] = (uint)i;
        return new Mesh(vertices, indices, PrimitiveType.Lines);
    }

    /// <summary>
    /// NOU: dreptunghiul de lățime <paramref name="width"/> și înălțime <paramref name="height"/>,
    /// în planul XY, cu fața spre +Z. Este "pânza" pe care desenăm culorile procedurale:
    /// u crește spre dreapta, v crește în sus. Vârfurile sunt albe.
    /// </summary>
    public static Mesh Quad(float width = 1.0f, float height = 1.0f)
    {
        float w = width / 2.0f, h = height / 2.0f;
        var vertices = new float[4 * Mesh.FloatsPerVertex];
        var indices = new uint[6];
        AddFace(vertices, indices, 0,
                new Vector3(-w, -h, 0), new Vector3(w, -h, 0), new Vector3(w, h, 0), new Vector3(-w, h, 0),
                Color4.White);
        return new Mesh(vertices, indices);
    }

    /// <summary>
    /// Cubul cu latura 1, centrat în origine: 6 fețe × 4 vârfuri = 24 de vârfuri.
    /// Culori: +X roșu, -X cyan, +Y verde, -Y magenta, +Z albastru, -Z galben.
    /// Fiecare față are coordonatele UV de la (0, 0) (stânga-jos) la (1, 1) (dreapta-sus).
    /// </summary>
    public static Mesh Cube()
    {
        const float h = 0.5f;
        var vertices = new float[24 * Mesh.FloatsPerVertex];
        var indices = new uint[36];

        // +Z (față)
        AddFace(vertices, indices, 0,
                new Vector3(-h, -h, h), new Vector3(h, -h, h), new Vector3(h, h, h), new Vector3(-h, h, h),
                new Color4(0.2f, 0.4f, 1.0f, 1.0f));
        // +X (dreapta)
        AddFace(vertices, indices, 1,
                new Vector3(h, -h, h), new Vector3(h, -h, -h), new Vector3(h, h, -h), new Vector3(h, h, h),
                new Color4(1.0f, 0.2f, 0.2f, 1.0f));
        // -Z (spate)
        AddFace(vertices, indices, 2,
                new Vector3(h, -h, -h), new Vector3(-h, -h, -h), new Vector3(-h, h, -h), new Vector3(h, h, -h),
                new Color4(1.0f, 0.9f, 0.2f, 1.0f));
        // -X (stânga)
        AddFace(vertices, indices, 3,
                new Vector3(-h, -h, -h), new Vector3(-h, -h, h), new Vector3(-h, h, h), new Vector3(-h, h, -h),
                new Color4(0.2f, 0.9f, 0.9f, 1.0f));
        // +Y (sus)
        AddFace(vertices, indices, 4,
                new Vector3(-h, h, h), new Vector3(h, h, h), new Vector3(h, h, -h), new Vector3(-h, h, -h),
                new Color4(0.3f, 0.9f, 0.3f, 1.0f));
        // -Y (jos)
        AddFace(vertices, indices, 5,
                new Vector3(-h, -h, -h), new Vector3(h, -h, -h), new Vector3(h, -h, h), new Vector3(-h, -h, h),
                new Color4(0.9f, 0.3f, 0.9f, 1.0f));

        return new Mesh(vertices, indices);
    }

    /// <summary>
    /// Sfera UV de rază <paramref name="radius"/> (laboratorul 5): o grilă de (stacks + 1) × (slices + 1) vârfuri.
    /// Culoarea vârfului arată coordonatele lui UV: roșu = u, verde = v.
    /// </summary>
    public static Mesh Sphere(float radius, int slices, int stacks)
    {
        int columns = slices + 1;   // ultima coloană (u = 1) dublează prima (u = 0): "cusătura"
        int rows = stacks + 1;
        var vertices = new float[rows * columns * Mesh.FloatsPerVertex];
        var indices = new uint[stacks * slices * 6];

        for (int i = 0; i < rows; i++)
        {
            float v = (float)i / stacks;                          // 0 la polul sud, 1 la polul nord
            float latitude = -MathF.PI / 2.0f + v * MathF.PI;
            for (int j = 0; j < columns; j++)
            {
                float u = (float)j / slices;
                float longitude = u * 2.0f * MathF.PI;
                float ring = radius * MathF.Cos(latitude);
                float x = ring * MathF.Cos(longitude);
                float y = radius * MathF.Sin(latitude);
                float z = -ring * MathF.Sin(longitude);
                SetVertex(vertices, i * columns + j, x, y, z, new Color4(u, v, 0.35f, 1.0f), u, v);
            }
        }

        int k = 0;
        for (int i = 0; i < stacks; i++)
        {
            for (int j = 0; j < slices; j++)
            {
                uint bottomLeft = (uint)(i * columns + j);
                uint bottomRight = bottomLeft + 1;
                uint topLeft = bottomLeft + (uint)columns;
                uint topRight = topLeft + 1;
                indices[k++] = bottomLeft; indices[k++] = bottomRight; indices[k++] = topRight;
                indices[k++] = topRight; indices[k++] = topLeft; indices[k++] = bottomLeft;
            }
        }
        return new Mesh(vertices, indices);
    }

    /// <summary>Cilindrul cu axa pe Y, de rază <paramref name="radius"/> și înălțime <paramref name="height"/>.</summary>
    public static Mesh Cylinder(float radius, float height, int segments)
    {
        int columns = segments + 1;
        int vertexCount = 2 * columns + 2 * (segments + 1);
        var vertices = new float[vertexCount * Mesh.FloatsPerVertex];
        var indices = new uint[segments * 6 + 2 * segments * 3];
        float half = height / 2.0f;

        for (int j = 0; j < columns; j++)
        {
            float u = (float)j / segments;
            float angle = u * 2.0f * MathF.PI;
            float x = radius * MathF.Cos(angle);
            float z = -radius * MathF.Sin(angle);
            Color4 color = ColorUtil.FromHsv(u, 0.7f, 1.0f);
            SetVertex(vertices, j, x, -half, z, color, u, 0.0f);
            SetVertex(vertices, columns + j, x, half, z, color, u, 1.0f);
        }

        int k = 0;
        for (int j = 0; j < segments; j++)
        {
            uint bl = (uint)j, br = bl + 1, tl = (uint)(columns + j), tr = tl + 1;
            indices[k++] = bl; indices[k++] = br; indices[k++] = tr;
            indices[k++] = tr; indices[k++] = tl; indices[k++] = bl;
        }

        int top = 2 * columns;
        int bottom = top + segments + 1;
        Color4 topColor = new(0.9f, 0.9f, 0.9f, 1.0f), bottomColor = new(0.4f, 0.4f, 0.4f, 1.0f);
        SetVertex(vertices, top, 0, half, 0, topColor, 0.5f, 0.5f);
        SetVertex(vertices, bottom, 0, -half, 0, bottomColor, 0.5f, 0.5f);
        for (int j = 0; j < segments; j++)
        {
            float angle = 2.0f * MathF.PI * j / segments;
            float c = MathF.Cos(angle), s = MathF.Sin(angle);
            SetVertex(vertices, top + 1 + j, radius * c, half, -radius * s, topColor, 0.5f + 0.5f * c, 0.5f + 0.5f * s);
            SetVertex(vertices, bottom + 1 + j, radius * c, -half, -radius * s, bottomColor, 0.5f + 0.5f * c, 0.5f + 0.5f * s);
        }
        for (int j = 0; j < segments; j++)
        {
            uint a = (uint)(1 + j), b = (uint)(1 + (j + 1) % segments);
            indices[k++] = (uint)top; indices[k++] = (uint)top + a; indices[k++] = (uint)top + b;
            indices[k++] = (uint)bottom; indices[k++] = (uint)bottom + b; indices[k++] = (uint)bottom + a;
        }
        return new Mesh(vertices, indices);
    }

    // =====================================================================================
    // Forme "albe", umbrite simplu (laboratorul 6)
    // =====================================================================================
    // Nu avem încă iluminare. Fiecare vârf primește un gri care depinde de orientarea suprafeței
    // față de o lumină fixă. În laboratorul 7, fragment shaderele procedurale înmulțesc culoarea
    // calculată cu acest gri (vColor), ca obiectele să nu arate "plate".

    /// <summary>Cubul cu latura 1, gri umbrit.</summary>
    public static Mesh ShadedCube()
    {
        Vector3[] faceNormals = { Vector3.UnitZ, Vector3.UnitX, -Vector3.UnitZ, -Vector3.UnitX, Vector3.UnitY, -Vector3.UnitY };
        return Recolor(Cube(), (vertex, _) => faceNormals[vertex / 4]);
    }

    /// <summary>Sfera de rază <paramref name="radius"/>, gri umbrit.</summary>
    public static Mesh ShadedSphere(float radius, int slices, int stacks) =>
        Recolor(Sphere(radius, slices, stacks), (_, position) => position.Normalized());

    /// <summary>Cilindrul cu axa pe Y, gri umbrit.</summary>
    public static Mesh ShadedCylinder(float radius, float height, int segments)
    {
        int lateral = 2 * (segments + 1);
        int top = lateral;
        int bottom = top + segments + 1;
        return Recolor(Cylinder(radius, height, segments), (vertex, position) =>
            vertex < lateral ? new Vector3(position.X, 0, position.Z).Normalized()
          : vertex < bottom ? Vector3.UnitY
          : -Vector3.UnitY);
    }

    private static readonly Vector3 LightDirection = new Vector3(0.4f, 1.0f, 0.6f).Normalized();

    /// <summary>Înlocuiește culoarea fiecărui vârf cu un gri: 0.45 (în umbră) .. 1.0 (spre lumină).</summary>
    private static Mesh Recolor(Mesh mesh, Func<int, Vector3, Vector3> normalOf)
    {
        for (int vertex = 0; vertex < mesh.VertexCount; vertex++)
        {
            int i = vertex * Mesh.FloatsPerVertex;
            var position = new Vector3(mesh.Vertices[i], mesh.Vertices[i + 1], mesh.Vertices[i + 2]);
            float light = MathF.Max(0.0f, Vector3.Dot(normalOf(vertex, position), LightDirection));
            float gray = 0.45f + 0.55f * light;
            mesh.SetColor(vertex, gray, gray, gray);
        }
        mesh.UpdateVertices();
        return mesh;
    }

    // =====================================================================================
    // Funcții ajutătoare
    // =====================================================================================

    /// <summary>
    /// Scrie fața <paramref name="face"/> a unei forme compuse din patrulatere. Colțurile a, b, c, d
    /// (în sens trigonometric, privind fața din afară) primesc UV (0,0), (1,0), (1,1), (0,1).
    /// </summary>
    private static void AddFace(float[] vertices, uint[] indices, int face,
                                Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color4 color)
    {
        int first = 4 * face;
        SetVertex(vertices, first, a.X, a.Y, a.Z, color, 0, 0);
        SetVertex(vertices, first + 1, b.X, b.Y, b.Z, color, 1, 0);
        SetVertex(vertices, first + 2, c.X, c.Y, c.Z, color, 1, 1);
        SetVertex(vertices, first + 3, d.X, d.Y, d.Z, color, 0, 1);

        uint f = (uint)first;
        int k = 6 * face;
        indices[k] = f;         indices[k + 1] = f + 1; indices[k + 2] = f + 2;
        indices[k + 3] = f + 2; indices[k + 4] = f + 3; indices[k + 5] = f;
    }

    /// <summary>Scrie un vârf (poziție, culoare, UV) în tabloul <paramref name="vertices"/>.</summary>
    private static void SetVertex(float[] vertices, int index, float x, float y, float z, Color4 color, float u, float v)
    {
        int i = index * Mesh.FloatsPerVertex;
        vertices[i] = x;
        vertices[i + 1] = y;
        vertices[i + 2] = z;
        vertices[i + 3] = color.R;
        vertices[i + 4] = color.G;
        vertices[i + 5] = color.B;
        vertices[i + 6] = u;
        vertices[i + 7] = v;
    }
}
