using OpenTK.Graphics.OpenGL4;

namespace Egc;

/// <summary>
/// O formă geometrică desenată în stilul OpenGL modern: date în VBO, indici în EBO,
/// descrierea formatului în VAO. Este clasa Triangle din laboratorul 2, generalizată:
/// vârfurile și indicii nu mai sunt scrise în clasă, ci primiți ca parametri.
/// Formatul unui vârf NU s-a schimbat: 6 float = x, y, z, r, g, b.
/// </summary>
public sealed class Mesh : IDisposable
{
    /// <summary>Câți float are un vârf: poziție (x, y, z) + culoare (r, g, b).</summary>
    public const int FloatsPerVertex = 6;

    /// <summary>
    /// Datele vârfurilor, în memoria RAM (CPU). Modificarea lor NU schimbă imaginea
    /// până când nu apelăm UpdateVertices() (vezi laboratorul 2).
    /// </summary>
    public float[] Vertices { get; }

    /// <summary>Indicii: ordinea în care sunt luate vârfurile de apelul de desenare.</summary>
    public uint[] Indices { get; }

    /// <summary>
    /// Cum sunt interpretați indicii: triunghiuri, linii, puncte etc.
    /// Poate fi schimbat oricând; nu afectează datele din GPU.
    /// </summary>
    public PrimitiveType Primitive { get; set; }

    /// <summary>Numărul de vârfuri.</summary>
    public int VertexCount => Vertices.Length / FloatsPerVertex;

    private readonly int _vao;   // Vertex Array Object   – "cum se citesc datele"
    private readonly int _vbo;   // Vertex Buffer Object  – "datele vârfurilor"
    private readonly int _ebo;   // Element Buffer Object – "indicii"

    public Mesh(float[] vertices, uint[] indices, PrimitiveType primitive = PrimitiveType.Triangles)
    {
        if (vertices.Length % FloatsPerVertex != 0)
        {
            throw new ArgumentException($"Lungimea tabloului de vârfuri trebuie să fie multiplu de {FloatsPerVertex}.");
        }

        Vertices = vertices;
        Indices = indices;
        Primitive = primitive;

        // Aceiași pași ca în constructorul clasei Triangle din laboratorul 2.
        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();
        _ebo = GL.GenBuffer();

        GL.BindVertexArray(_vao);

        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, Vertices.Length * sizeof(float),
                      Vertices, BufferUsageHint.DynamicDraw);

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, Indices.Length * sizeof(uint),
                      Indices, BufferUsageHint.StaticDraw);

        int stride = FloatsPerVertex * sizeof(float);   // 24 de octeți
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
    }

    /// <summary>Setează poziția unui vârf în tabloul din RAM (apoi apelați UpdateVertices()).</summary>
    public void SetPosition(int vertex, float x, float y, float z = 0.0f)
    {
        int i = vertex * FloatsPerVertex;
        Vertices[i] = x;
        Vertices[i + 1] = y;
        Vertices[i + 2] = z;
    }

    /// <summary>Setează culoarea unui vârf în tabloul din RAM (apoi apelați UpdateVertices()).</summary>
    public void SetColor(int vertex, float r, float g, float b)
    {
        int i = vertex * FloatsPerVertex + 3;
        Vertices[i] = r;
        Vertices[i + 1] = g;
        Vertices[i + 2] = b;
    }

    /// <summary>Copiază din nou tabloul Vertices (RAM) în VBO (memoria GPU).</summary>
    public void UpdateVertices()
    {
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero,
                         Vertices.Length * sizeof(float), Vertices);
    }

    /// <summary>Desenează forma (programul shader trebuie activat înainte).</summary>
    public void Draw()
    {
        GL.BindVertexArray(_vao);
        GL.DrawElements(Primitive, Indices.Length, DrawElementsType.UnsignedInt, 0);
    }

    public void Dispose()
    {
        GL.DeleteBuffer(_vbo);
        GL.DeleteBuffer(_ebo);
        GL.DeleteVertexArray(_vao);
    }
}
