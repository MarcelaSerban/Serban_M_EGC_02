using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Egc;

/// <summary>
/// Un program shader OpenGL: vertex shader + fragment shader, compilate și legate (link).
/// Neschimbată față de laboratorul 3.
/// </summary>
public sealed class Shader : IDisposable
{
    /// <summary>Identificatorul (handle) programului în OpenGL.</summary>
    public int Handle { get; }

    private readonly Dictionary<string, int> _uniformLocations = new();

    public Shader(string vertexSource, string fragmentSource)
    {
        int vertex = Compile(ShaderType.VertexShader, vertexSource);
        int fragment = Compile(ShaderType.FragmentShader, fragmentSource);

        Handle = GL.CreateProgram();
        GL.AttachShader(Handle, vertex);
        GL.AttachShader(Handle, fragment);
        GL.LinkProgram(Handle);

        GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int linked);
        if (linked == 0)
        {
            string log = GL.GetProgramInfoLog(Handle);
            throw new InvalidOperationException($"Eroare la linkarea programului shader:\n{log}");
        }

        // După link, obiectele shader individuale nu mai sunt necesare.
        GL.DetachShader(Handle, vertex);
        GL.DetachShader(Handle, fragment);
        GL.DeleteShader(vertex);
        GL.DeleteShader(fragment);
    }

    /// <summary>Citește shaderele din fișiere (căile sunt relative la folderul executabilului).</summary>
    public static Shader FromFiles(string vertexPath, string fragmentPath)
    {
        string baseDir = AppContext.BaseDirectory;
        string vertexSource = File.ReadAllText(Path.Combine(baseDir, vertexPath));
        string fragmentSource = File.ReadAllText(Path.Combine(baseDir, fragmentPath));
        return new Shader(vertexSource, fragmentSource);
    }

    /// <summary>Activează programul: apelurile de desenare următoare îl vor folosi.</summary>
    public void Use() => GL.UseProgram(Handle);

    // --- Uniforme -----------------------------------------------------------
    // Atenție: GL.Uniform* se aplică programului ACTIV (cel setat cu Use()).

    public void SetMatrix4(string name, Matrix4 value)
    {
        GL.UniformMatrix4(GetLocation(name), false, ref value);
    }

    public void SetVector3(string name, Vector3 value)
    {
        GL.Uniform3(GetLocation(name), value.X, value.Y, value.Z);
    }

    public void SetColor(string name, Color4 value)
    {
        GL.Uniform4(GetLocation(name), value.R, value.G, value.B, value.A);
    }

    /// <summary>NOU (laboratorul 3): uniformă de tip float (în GLSL: uniform float ...).</summary>
    public void SetFloat(string name, float value)
    {
        GL.Uniform1(GetLocation(name), value);
    }

    /// <summary>NOU (laboratorul 3): uniformă de tip int (în GLSL: uniform int ...).</summary>
    public void SetInt(string name, int value)
    {
        GL.Uniform1(GetLocation(name), value);
    }

    private int GetLocation(string name)
    {
        // Întrebăm driverul o singură dată pentru fiecare nume și reținem răspunsul.
        // Dacă uniforma nu există (sau a fost eliminată de compilator fiindcă nu e folosită),
        // locația este -1, iar GL.Uniform* cu -1 este ignorat fără eroare.
        if (!_uniformLocations.TryGetValue(name, out int location))
        {
            location = GL.GetUniformLocation(Handle, name);
            _uniformLocations[name] = location;
        }
        return location;
    }

    private static int Compile(ShaderType type, string source)
    {
        int shader = GL.CreateShader(type);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);

        GL.GetShader(shader, ShaderParameter.CompileStatus, out int compiled);
        if (compiled == 0)
        {
            string log = GL.GetShaderInfoLog(shader);
            GL.DeleteShader(shader);
            throw new InvalidOperationException($"Eroare la compilarea {type}:\n{log}");
        }
        return shader;
    }

    public void Dispose()
    {
        GL.DeleteProgram(Handle);
    }
}
