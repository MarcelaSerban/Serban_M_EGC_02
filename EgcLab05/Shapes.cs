using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Egc;

/// <summary>
/// Fabrica de forme 3D: fiecare metodă calculează vârfurile și indicii unei forme (mesh procedural)
/// și construiește un obiect Mesh. Un vârf = 6 float: x, y, z, r, g, b (ca în laboratoarele 2–4).
/// Formele sunt descrise în object space, cu centrul în origine; locul, mărimea și rotația
/// din scenă sunt date de matricea model (Transform3D).
///
/// Regula sensului de parcurgere: privită DIN AFARA obiectului, fiecare față are vârfurile
/// în sens trigonometric (invers acelor de ceasornic). Cu tasta C (eliminarea fețelor din spate)
/// fețele parcurse greșit dispar, deci greșelile se văd imediat.
/// </summary>
public static class Shapes
{
    // =====================================================================================
    // Forme pentru orientare (linii)
    // =====================================================================================

    /// <summary>
    /// Grila de pe podea (planul y = 0): linii paralele cu axele X și Z, din <paramref name="step"/>
    /// în <paramref name="step"/> unități, între -<paramref name="halfSize"/> și +<paramref name="halfSize"/>.
    /// </summary>
    public static Mesh Grid(int halfSize, float step, Color4 color)
    {
        int linesPerDirection = 2 * halfSize + 1;
        var vertices = new float[linesPerDirection * 2 * 2 * Mesh.FloatsPerVertex];   // 2 direcții × 2 capete
        var indices = new uint[linesPerDirection * 2 * 2];

        float extent = halfSize * step;
        int v = 0;
        for (int k = -halfSize; k <= halfSize; k++)
        {
            float c = k * step;
            // linie paralelă cu axa Z (x = c)
            SetVertex(vertices, v++, c, 0.0f, -extent, color);
            SetVertex(vertices, v++, c, 0.0f,  extent, color);
            // linie paralelă cu axa X (z = c)
            SetVertex(vertices, v++, -extent, 0.0f, c, color);
            SetVertex(vertices, v++,  extent, 0.0f, c, color);
        }
        for (int i = 0; i < indices.Length; i++)
        {
            indices[i] = (uint)i;   // fiecare pereche de vârfuri consecutive = o linie
        }

        return new Mesh(vertices, indices, PrimitiveType.Lines);
    }

    /// <summary>Cele trei axe ale lumii, din origine: X roșu, Y verde, Z albastru.</summary>
    public static Mesh Axes(float length)
    {
        var vertices = new float[6 * Mesh.FloatsPerVertex];
        SetVertex(vertices, 0, 0, 0, 0,        1, 0, 0);
        SetVertex(vertices, 1, length, 0, 0,   1, 0, 0);
        SetVertex(vertices, 2, 0, 0, 0,        0, 1, 0);
        SetVertex(vertices, 3, 0, length, 0,   0, 1, 0);
        SetVertex(vertices, 4, 0, 0, 0,        0, 0, 1);
        SetVertex(vertices, 5, 0, 0, length,   0, 0, 1);
        uint[] indices = { 0, 1, 2, 3, 4, 5 };
        return new Mesh(vertices, indices, PrimitiveType.Lines);
    }

    // =====================================================================================
    // Forme 3D (triunghiuri)
    // =====================================================================================

    /// <summary>
    /// Piramida cu baza pătrată (latura 1, înălțimea 1), centrată în origine. EXEMPLU COMPLET:
    /// fiecare față are culoarea ei, deci vârfurile NU sunt partajate între fețe:
    /// 4 fețe laterale × 3 vârfuri + baza × 4 vârfuri = 16 vârfuri.
    /// </summary>
    public static Mesh Pyramid()
    {
        const float h = 0.5f;
        var vertices = new float[16 * Mesh.FloatsPerVertex];
        var indices = new uint[4 * 3 + 6];

        var apex = new Vector3(0, h, 0);
        Vector3 frontLeft = new(-h, -h, h), frontRight = new(h, -h, h);
        Vector3 backRight = new(h, -h, -h), backLeft = new(-h, -h, -h);

        // Fețele laterale: două colțuri ale bazei (de la stânga la dreapta, privind din afară) + vârful.
        AddTriangle(vertices, indices, 0, frontLeft, frontRight, apex, ColorUtil.FromHsv(0.00f, 0.8f, 1.0f));  // față (+Z)
        AddTriangle(vertices, indices, 1, frontRight, backRight, apex, ColorUtil.FromHsv(0.15f, 0.8f, 1.0f));  // dreapta (+X)
        AddTriangle(vertices, indices, 2, backRight, backLeft, apex,   ColorUtil.FromHsv(0.30f, 0.8f, 1.0f));  // spate (-Z)
        AddTriangle(vertices, indices, 3, backLeft, frontLeft, apex,   ColorUtil.FromHsv(0.55f, 0.8f, 1.0f));  // stânga (-X)

        // Baza (-Y), privită de dedesubt: un pătrat = 4 vârfuri (12..15) și 6 indici (12..17).
        Color4 baseColor = new(0.35f, 0.35f, 0.35f, 1.0f);
        SetVertex(vertices, 12, backLeft, baseColor);
        SetVertex(vertices, 13, backRight, baseColor);
        SetVertex(vertices, 14, frontRight, baseColor);
        SetVertex(vertices, 15, frontLeft, baseColor);
        uint[] baseIndices = { 12, 13, 14, 14, 15, 12 };
        baseIndices.CopyTo(indices, 12);

        return new Mesh(vertices, indices);
    }

    /// <summary>
    /// Cubul cu latura 1, centrat în origine. Fiecare față are culoarea ei, deci fiecare față
    /// are propriile 4 vârfuri: 6 fețe × 4 = 24 de vârfuri și 6 fețe × 6 = 36 de indici.
    /// Culori: +X roșu, -X cyan, +Y verde, -Y magenta, +Z albastru, -Z galben
    /// (axa pozitivă are culoarea axei, cea negativă culoarea complementară).
    /// </summary>
    public static Mesh Cube()
    {
        const float h = 0.5f;   // jumătate din latură
        var vertices = new float[24 * Mesh.FloatsPerVertex];
        var indices = new uint[36];

        // Fiecare față: cele 4 colțuri în sens trigonometric, privind fața din afara cubului
        // (stânga-jos, dreapta-jos, dreapta-sus, stânga-sus).

        // Fața 0: +Z (față). Privită din +Z: dreapta = +X, sus = +Y.
        AddFace(vertices, indices, 0,
                new Vector3(-h, -h, h), new Vector3(h, -h, h), new Vector3(h, h, h), new Vector3(-h, h, h),
                new Color4(0.2f, 0.4f, 1.0f, 1.0f));

        // Fața 1: +X (dreapta). Privită din +X: dreapta = -Z, sus = +Y.
        AddFace(vertices, indices, 1,
                new Vector3(h, -h, h), new Vector3(h, -h, -h), new Vector3(h, h, -h), new Vector3(h, h, h),
                new Color4(1.0f, 0.2f, 0.2f, 1.0f));

        // Fața 2: -X (stânga). Privită din -X: dreapta = +Z, sus = +Y.
        AddFace(vertices, indices, 2,
                new Vector3(-h, -h, -h), new Vector3(-h, -h, h), new Vector3(-h, h, h), new Vector3(-h, h, -h),
                new Color4(0.0f, 1.0f, 1.0f, 1.0f));

        // Fața 3: +Y (sus). Privită de deasupra: dreapta = +X, sus = -Z.
        AddFace(vertices, indices, 3,
                new Vector3(-h, h, h), new Vector3(h, h, h), new Vector3(h, h, -h), new Vector3(-h, h, -h),
                new Color4(0.2f, 0.9f, 0.3f, 1.0f));

        // Fața 4: -Y (jos). Privită de dedesubt: dreapta = +X, sus = +Z.
        AddFace(vertices, indices, 4,
                new Vector3(-h, -h, -h), new Vector3(h, -h, -h), new Vector3(h, -h, h), new Vector3(-h, -h, h),
                new Color4(1.0f, 0.2f, 1.0f, 1.0f));

        // Fața 5: -Z (spate). Privită din -Z: dreapta = -X, sus = +Y.
        AddFace(vertices, indices, 5,
                new Vector3(h, -h, -h), new Vector3(-h, -h, -h), new Vector3(-h, h, -h), new Vector3(h, h, -h),
                new Color4(1.0f, 0.9f, 0.2f, 1.0f));

        return new Mesh(vertices, indices);
    }

    /// <summary>
    /// Sfera UV de rază <paramref name="radius"/>: o grilă de (stacks + 1) × (slices + 1) vârfuri,
    /// așezată pe sferă prin coordonatele u (de-a lungul paralelelor) și v (de-a lungul meridianelor).
    /// Culoarea vârfului arată coordonatele lui UV: roșu = u, verde = v.
    /// </summary>
    /// <param name="slices">Numărul de felii verticale (meridiane), de exemplu 32.</param>
    /// <param name="stacks">Numărul de benzi orizontale (paralele), de exemplu 16.</param>
    public static Mesh Sphere(float radius, int slices, int stacks)
    {
        int columns = slices + 1;   // ultima coloană (u = 1) dublează prima (u = 0): "cusătura"
        int rows = stacks + 1;
        var vertices = new float[rows * columns * Mesh.FloatsPerVertex];
        var indices = new uint[stacks * slices * 6];

        // 1. Vârfurile: rândul i (de jos în sus), coloana j (spre est).
        for (int i = 0; i < rows; i++)
        {
            float v = (float)i / stacks;                          // 0 la polul sud, 1 la polul nord
            float latitude = -MathF.PI / 2.0f + v * MathF.PI;     // unghiul față de ecuator: -90° .. +90°

            for (int j = 0; j < columns; j++)
            {
                float u = (float)j / slices;                      // 0 .. 1 în jurul sferei
                float longitude = u * 2.0f * MathF.PI;            // 0° .. 360°

                // poziția vârfului: coordonate sferice -> carteziene (secțiunea 2.2)
                float parallelRadius = radius * MathF.Cos(latitude);   // raza paralelei r_p = R · cos φ
                float x = parallelRadius * MathF.Cos(longitude);
                float y = radius * MathF.Sin(latitude);
                float z = -parallelRadius * MathF.Sin(longitude);

                SetVertex(vertices, i * columns + j, x, y, z, u, v, 0.35f);
            }
        }

        // 2. Indicii: fiecare celulă a grilei (un "dreptunghi" curbat) = 2 triunghiuri.
        int k = 0;
        for (int i = 0; i < stacks; i++)
        {
            for (int j = 0; j < slices; j++)
            {
                uint bottomLeft = (uint)(i * columns + j);
                uint bottomRight = bottomLeft + 1;
                uint topLeft = bottomLeft + (uint)columns;
                uint topRight = topLeft + 1;

                // două triunghiuri, în sens trigonometric privite din afară
                indices[k++] = bottomLeft; indices[k++] = bottomRight; indices[k++] = topRight;
                indices[k++] = topRight;   indices[k++] = topLeft;     indices[k++] = bottomLeft;
            }
        }

        return new Mesh(vertices, indices);
    }

    /// <summary>
    /// FACULTATIV (TODO 5.8): cilindrul cu axa pe Y, de rază <paramref name="radius"/> și înălțime
    /// <paramref name="height"/>, cu <paramref name="segments"/> segmente pe circumferință.
    /// Suprafața laterală = o grilă cu 2 rânduri (ca sfera, cu un singur "stack"); capacele = evantaie.
    /// </summary>
    public static Mesh Cylinder(float radius, float height, int segments)
    {
        // TODO 5.8 (facultativ): vârfurile și indicii cilindrului.
        // Axa pe Y, centrat în origine: baza la y = -height/2, capacul de sus la y = +height/2.
        int columns = segments + 1;               // ultima coloană dublează prima ("cusătura"), ca la sferă
        float halfHeight = height / 2.0f;

        // Numărul de vârfuri: laterala (2 rânduri × columns) + 2 capace × (centru + columns vârfuri de pe cerc).
        int sideVertices = 2 * columns;
        int capVertices = 1 + columns;
        var vertices = new float[(sideVertices + 2 * capVertices) * Mesh.FloatsPerVertex];
        var indices = new uint[segments * 6 + 2 * segments * 3];

        // 1. Suprafața laterală: o grilă cu un singur rând de celule (2 rânduri de vârfuri).
        for (int i = 0; i < 2; i++)                       // i = 0 jos, i = 1 sus
        {
            float y = -halfHeight + i * height;
            for (int j = 0; j < columns; j++)
            {
                float u = (float)j / segments;
                float angle = u * 2.0f * MathF.PI;
                // aceeași parametrizare ca la sferă: x = r·cos, z = -r·sin
                SetVertex(vertices, i * columns + j, radius * MathF.Cos(angle), y, -radius * MathF.Sin(angle), u, i, 0.35f);
            }
        }

        int k = 0;
        for (int j = 0; j < segments; j++)
        {
            uint bottomLeft = (uint)j;
            uint bottomRight = bottomLeft + 1;
            uint topLeft = bottomLeft + (uint)columns;
            uint topRight = topLeft + 1;

            // două triunghiuri, în sens trigonometric privite din afară (ca la sferă)
            indices[k++] = bottomLeft; indices[k++] = bottomRight; indices[k++] = topRight;
            indices[k++] = topRight;   indices[k++] = topLeft;     indices[k++] = bottomLeft;
        }

        // 2. Capacele: evantaie de triunghiuri (ca poligonul regulat din laboratorul 3).
        //    Capacul de sus (+Y), privit de deasupra, e parcurs în sens trigonometric;
        //    capacul de jos (-Y) are ordinea inversă, ca să fie privit corect de dedesubt.
        int topCenter = sideVertices;
        int bottomCenter = sideVertices + capVertices;
        var topColor = new Color4(0.2f, 0.9f, 0.3f, 1.0f);
        var bottomColor = new Color4(1.0f, 0.2f, 1.0f, 1.0f);

        SetVertex(vertices, topCenter, 0.0f, halfHeight, 0.0f, topColor);
        SetVertex(vertices, bottomCenter, 0.0f, -halfHeight, 0.0f, bottomColor);
        for (int j = 0; j < columns; j++)
        {
            float angle = (float)j / segments * 2.0f * MathF.PI;
            float x = radius * MathF.Cos(angle);
            float z = -radius * MathF.Sin(angle);
            SetVertex(vertices, topCenter + 1 + j, x, halfHeight, z, topColor);
            SetVertex(vertices, bottomCenter + 1 + j, x, -halfHeight, z, bottomColor);
        }

        for (int j = 0; j < segments; j++)
        {
            // sus: centru, j, j+1
            indices[k++] = (uint)topCenter;
            indices[k++] = (uint)(topCenter + 1 + j);
            indices[k++] = (uint)(topCenter + 2 + j);
            // jos: centru, j+1, j (sens inversat)
            indices[k++] = (uint)bottomCenter;
            indices[k++] = (uint)(bottomCenter + 2 + j);
            indices[k++] = (uint)(bottomCenter + 1 + j);
        }

        return new Mesh(vertices, indices);
    }

    // =====================================================================================
    // Funcții ajutătoare
    // =====================================================================================

    /// <summary>
    /// Scrie fața <paramref name="face"/> a unei forme compuse din patrulatere: vârfurile
    /// 4·face .. 4·face + 3 și indicii 6·face .. 6·face + 5 (triunghiurile a-b-c și c-d-a).
    /// Colțurile a, b, c, d trebuie date în sens trigonometric, privind fața din afară.
    /// </summary>
    private static void AddFace(float[] vertices, uint[] indices, int face,
                                Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color4 color)
    {
        int first = 4 * face;
        SetVertex(vertices, first, a, color);
        SetVertex(vertices, first + 1, b, color);
        SetVertex(vertices, first + 2, c, color);
        SetVertex(vertices, first + 3, d, color);

        uint f = (uint)first;
        int k = 6 * face;
        indices[k] = f;         indices[k + 1] = f + 1; indices[k + 2] = f + 2;   // a, b, c
        indices[k + 3] = f + 2; indices[k + 4] = f + 3; indices[k + 5] = f;       // c, d, a
    }

    /// <summary>Scrie triunghiul <paramref name="face"/>: vârfurile 3·face .. 3·face + 2 și indicii lor.</summary>
    private static void AddTriangle(float[] vertices, uint[] indices, int face,
                                    Vector3 a, Vector3 b, Vector3 c, Color4 color)
    {
        int first = 3 * face;
        SetVertex(vertices, first, a, color);
        SetVertex(vertices, first + 1, b, color);
        SetVertex(vertices, first + 2, c, color);
        indices[first] = (uint)first;
        indices[first + 1] = (uint)first + 1;
        indices[first + 2] = (uint)first + 2;
    }

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

    private static void SetVertex(float[] vertices, int index, float x, float y, float z, Color4 color) =>
        SetVertex(vertices, index, x, y, z, color.R, color.G, color.B);

    private static void SetVertex(float[] vertices, int index, Vector3 p, Color4 color) =>
        SetVertex(vertices, index, p.X, p.Y, p.Z, color.R, color.G, color.B);
}
