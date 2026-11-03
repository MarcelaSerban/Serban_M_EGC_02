using OpenTK.Mathematics;

namespace Egc;

/// <summary>
/// Fabrica de forme: fiecare metodă construiește un obiect Mesh (vârfuri + indici).
/// Un vârf = 6 float: x, y, z, r, g, b (aceeași structură ca în laboratoarele 2 și 3).
/// Formele sunt plate (z = 0) și sunt descrise în object space, cu centrul în origine;
/// locul, mărimea și rotația din scenă sunt date de matricea model (Transform2D).
/// </summary>
public static class Shapes
{
    /// <summary>Triunghiul din laboratorul 2.</summary>
    public static Mesh Triangle()
    {
        float[] vertices =
        {
            //  x      y     z       r     g     b
            -0.5f, -0.5f, 0.0f,   1.0f, 0.0f, 0.0f,   // 0: stânga-jos, roșu
             0.5f, -0.5f, 0.0f,   0.0f, 1.0f, 0.0f,   // 1: dreapta-jos, verde
             0.0f,  0.5f, 0.0f,   0.0f, 0.0f, 1.0f    // 2: sus, albastru
        };
        uint[] indices = { 0, 1, 2 };
        return new Mesh(vertices, indices);
    }

    /// <summary>Dreptunghiul (aici pătrat cu latura 1) din 2 triunghiuri: 4 vârfuri și 6 indici.</summary>
    public static Mesh Quad()
    {
        var vertices = new float[4 * Mesh.FloatsPerVertex];

        //                       poziție (x, y, z)         culoare (r, g, b)
        SetVertex(vertices, 0, -0.5f, -0.5f, 0.0f,   1.0f, 0.0f, 0.0f);   // 0: stânga-jos, roșu
        SetVertex(vertices, 1,  0.5f, -0.5f, 0.0f,   0.0f, 1.0f, 0.0f);   // 1: dreapta-jos, verde
        SetVertex(vertices, 2,  0.5f,  0.5f, 0.0f,   0.0f, 0.0f, 1.0f);   // 2: dreapta-sus, albastru
        SetVertex(vertices, 3, -0.5f,  0.5f, 0.0f,   1.0f, 1.0f, 0.0f);   // 3: stânga-sus, galben

        // Două triunghiuri, ambele în sens trigonometric: (0, 1, 2) și (2, 3, 0).
        uint[] indices = { 0, 1, 2,   2, 3, 0 };

        return new Mesh(vertices, indices);
    }

    /// <summary>
    /// Poligon regulat cu <paramref name="sides"/> laturi, centrat în origine.
    /// Vârful 0 = centrul; vârfurile 1..n = cele n colțuri, pe un cerc de rază <paramref name="radius"/>.
    /// Un "cerc" este un poligon cu multe laturi (de exemplu 64).
    /// </summary>
    public static Mesh RegularPolygon(int sides, float radius)
    {
        // n laturi => n + 1 vârfuri (centrul și cele n colțuri) și n triunghiuri => 3 * n indici
        var vertices = new float[(sides + 1) * Mesh.FloatsPerVertex];
        var indices = new uint[sides * 3];

        // Vârful 0: centrul, alb.
        SetVertex(vertices, 0, 0.0f, 0.0f, 0.0f, Color4.White);

        for (int i = 0; i < sides; i++)
        {
            // Unghiul colțului i: începem de sus (90 de grade) și mergem în sens trigonometric.
            float angle = MathF.PI / 2.0f + 2.0f * MathF.PI * i / sides;

            float x = radius * MathF.Cos(angle);
            float y = radius * MathF.Sin(angle);
            Color4 color = ColorUtil.FromHsv((float)i / sides, 1.0f, 1.0f);

            // Colțul i se memorează în vârful 1 + i (vârful 0 este centrul).
            SetVertex(vertices, 1 + i, x, y, 0.0f, color);

            // Triunghiul i: centrul, colțul i și colțul următor (ultimul se închide pe primul, prin %).
            indices[3 * i] = 0;
            indices[3 * i + 1] = (uint)(1 + i);
            indices[3 * i + 2] = (uint)(1 + (i + 1) % sides);
        }

        return new Mesh(vertices, indices);
    }

    // --- Funcții ajutătoare ----------------------------------------------

    /// <summary>Scrie un vârf (poziție + culoare) în tabloul <paramref name="vertices"/>, pe poziția <paramref name="index"/>.</summary>
    private static void SetVertex(float[] vertices, int index, float x, float y, float z, float r, float g, float b)
    {
        int i = index * Mesh.FloatsPerVertex;
        vertices[i] = x;
        vertices[i + 1] = y;
        vertices[i + 2] = z;
        vertices[i + 3] = r;
        vertices[i + 4] = g;
        vertices[i + 5] = b;
    }

    /// <summary>Aceeași funcție, cu culoarea dată ca Color4.</summary>
    private static void SetVertex(float[] vertices, int index, float x, float y, float z, Color4 color)
    {
        SetVertex(vertices, index, x, y, z, color.R, color.G, color.B);
    }
}
