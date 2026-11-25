using System.Text.RegularExpressions;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Egc;

/// <summary>
/// Un program shader OpenGL: vertex shader + fragment shader, citite din fișiere, compilate și legate.
/// NOU (laboratorul 7):
///  - programul se poate REÎNCĂRCA în timpul rulării (Reload, TODO 7.1): editați fișierul .vert / .frag,
///    salvați, iar aplicația îl recompilează fără repornire;
///  - la o eroare de compilare, mesajul driverului este afișat împreună cu rândul greșit din fișier,
///    iar aplicația NU se oprește: la pornire folosește un shader de rezervă (magenta),
///    la reîncărcare păstrează programul vechi;
///  - uniformele cerute de C# dar inexistente în shader sunt semnalate în consolă (secțiunea 3.4).
/// </summary>
public sealed class Shader : IDisposable
{
    /// <summary>Numele afișat în mesaje: fișierul fragment shaderului (de exemplu "rings.frag").</summary>
    public string Name { get; }

    /// <summary>Identificatorul (handle) programului în OpenGL. Se schimbă la fiecare reîncărcare reușită.</summary>
    public int Handle { get; private set; }

    /// <summary>Adevărat dacă programul este cel de rezervă (fișierele au avut erori la pornire).</summary>
    public bool IsFallback { get; private set; }

    public string VertexPath { get; }
    public string FragmentPath { get; }

    private readonly Dictionary<string, int> _uniformLocations = new();

    // Uniformele cerute de C# care nu există în program (afișate o singură dată, vezi ReportMissing).
    private readonly HashSet<string> _missing = new();
    private readonly HashSet<string> _reported = new();

    // Uniformele "comune", trimise tuturor programelor; nu e o greșeală dacă un shader nu le folosește.
    private static readonly HashSet<string> Optional = new()
    {
        "uModel", "uView", "uProjection", "uTint", "uTime", "uShowUV", "uResolution",
    };

    // Data ultimei modificări a fișierelor, la ultima încercare de compilare.
    private DateTime _stamp;

    public Shader(string vertexPath, string fragmentPath)
    {
        VertexPath = vertexPath;
        FragmentPath = fragmentPath;
        Name = Path.GetFileName(fragmentPath);
        _stamp = FilesTimestamp();

        if (TryBuild(out int handle))
        {
            Handle = handle;
        }
        else
        {
            Handle = Link(Compile(ShaderType.VertexShader, FallbackVertex, "rezervă.vert"),
                          Compile(ShaderType.FragmentShader, FallbackFragment, "rezervă.frag"));
            IsFallback = true;
            ConsolePanel.PrintMessage($"{Name}: folosesc shaderul de rezervă (magenta) până la corectarea erorii.");
        }
    }

    /// <summary>
    /// Recompilează programul din fișiere. Dacă reușește, noul program îl înlocuiește pe cel vechi;
    /// dacă fișierele au erori, mesajul apare în consolă și programul vechi rămâne activ.
    /// Întoarce true dacă programul a fost înlocuit.
    /// </summary>
    public bool Reload()
    {
        // TODO 7.1: reîncărcarea programului (secțiunea 2.3)
        // 1. Construim un program NOU din fișiere. La eroare, TryBuild a afișat deja mesajul,
        //    iar programul vechi rămâne activ (aplicația continuă să ruleze).
        if (!TryBuild(out int newHandle)) return false;

        // 2. Abia acum ștergem programul vechi și îl înlocuim cu cel nou.
        //    (Dacă l-am șterge înainte de verificare și compilarea ar eșua, nu ar mai rămâne
        //    niciun program valid: obiectele nu s-ar mai desena corect.)
        GL.DeleteProgram(Handle);
        Handle = newHandle;
        IsFallback = false;

        // 3. Locațiile uniformelor aparțin programului vechi: le golim, iar ele se vor cere din nou.
        _uniformLocations.Clear();
        _missing.Clear();
        _reported.Clear();

        // 4. Reîncărcare reușită.
        return true;
    }

    /// <summary>Adevărat dacă unul dintre fișiere a fost salvat după ultima compilare (pentru reîncărcarea automată).</summary>
    public bool FilesChanged()
    {
        DateTime now = FilesTimestamp();
        if (now == _stamp) return false;
        _stamp = now;
        return true;
    }

    /// <summary>Activează programul: apelurile de desenare următoare îl vor folosi.</summary>
    public void Use() => GL.UseProgram(Handle);

    // --- Uniforme -----------------------------------------------------------
    // Atenție: GL.Uniform* se aplică programului ACTIV (cel setat cu Use()).
    // Tipul din C# trebuie să corespundă tipului din GLSL (tabelul din secțiunea 3.2).

    public void SetMatrix4(string name, Matrix4 value) => GL.UniformMatrix4(GetLocation(name), false, ref value);

    /// <summary>NOU (laboratorul 7): uniform vec2.</summary>
    public void SetVector2(string name, Vector2 value) => GL.Uniform2(GetLocation(name), value.X, value.Y);

    public void SetVector3(string name, Vector3 value) => GL.Uniform3(GetLocation(name), value.X, value.Y, value.Z);

    /// <summary>uniform vec4 (o culoare RGBA).</summary>
    public void SetColor(string name, Color4 value) => GL.Uniform4(GetLocation(name), value.R, value.G, value.B, value.A);

    public void SetFloat(string name, float value) => GL.Uniform1(GetLocation(name), value);

    /// <summary>uniform int; tot cu el se trimit și uniformele bool (0 = false, altceva = true).</summary>
    public void SetInt(string name, int value) => GL.Uniform1(GetLocation(name), value);

    private int GetLocation(string name)
    {
        // Întrebăm driverul o singură dată pentru fiecare nume și reținem răspunsul.
        // Dacă uniforma nu există (sau a fost eliminată de compilator fiindcă nu e folosită),
        // locația este -1, iar GL.Uniform* cu -1 este ignorat FĂRĂ EROARE. De aceea o notăm.
        if (!_uniformLocations.TryGetValue(name, out int location))
        {
            location = GL.GetUniformLocation(Handle, name);
            _uniformLocations[name] = location;
            if (location < 0 && !Optional.Contains(name) && !IsFallback)
            {
                _missing.Add(name);
            }
        }
        return location;
    }

    /// <summary>
    /// Afișează (o singură dată) uniformele trimise din C# care nu există în program:
    /// nume greșit, uniformă nedeclarată sau declarată dar nefolosită (eliminată la compilare).
    /// </summary>
    public void ReportMissing()
    {
        string[] fresh = _missing.Where(n => !_reported.Contains(n)).OrderBy(n => n).ToArray();
        if (fresh.Length == 0) return;
        foreach (string n in fresh) _reported.Add(n);
        ConsolePanel.PrintMessage($"{Name}: uniforme inexistente sau nefolosite în shader: {string.Join(", ", fresh)}");
    }

    // --- Compilare ------------------------------------------------------------------------

    /// <summary>Citește fișierele și construiește un program nou. La eroare afișează mesajul și întoarce false.</summary>
    private bool TryBuild(out int handle)
    {
        handle = 0;
        int vertex = 0, fragment = 0;
        try
        {
            vertex = Compile(ShaderType.VertexShader, File.ReadAllText(VertexPath), Path.GetFileName(VertexPath));
            fragment = Compile(ShaderType.FragmentShader, File.ReadAllText(FragmentPath), Name);
            handle = Link(vertex, fragment);
            return true;
        }
        catch (Exception e) when (e is InvalidOperationException or IOException)
        {
            if (vertex != 0) GL.DeleteShader(vertex);
            if (fragment != 0) GL.DeleteShader(fragment);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine();
            Console.WriteLine(e.Message);
            Console.ResetColor();
            return false;
        }
    }

    private static int Compile(ShaderType type, string source, string fileName)
    {
        int shader = GL.CreateShader(type);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);

        GL.GetShader(shader, ShaderParameter.CompileStatus, out int compiled);
        if (compiled == 0)
        {
            string log = GL.GetShaderInfoLog(shader);
            GL.DeleteShader(shader);
            throw new InvalidOperationException($" ✗ Eroare de compilare în {fileName}:\n{Explain(log, source)}");
        }
        return shader;
    }

    private static int Link(int vertex, int fragment)
    {
        int program = GL.CreateProgram();
        GL.AttachShader(program, vertex);
        GL.AttachShader(program, fragment);
        GL.LinkProgram(program);

        // După link, obiectele shader individuale nu mai sunt necesare.
        GL.DetachShader(program, vertex);
        GL.DetachShader(program, fragment);
        GL.DeleteShader(vertex);
        GL.DeleteShader(fragment);

        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int linked);
        if (linked == 0)
        {
            string log = GL.GetProgramInfoLog(program);
            GL.DeleteProgram(program);
            throw new InvalidOperationException($" ✗ Eroare la legarea (link) programului:\n{log}");
        }
        return program;
    }

    // Numărul rândului din mesajele driverelor: "0(23) : error ..." (NVIDIA), "0:23(5): error ..." (Mesa),
    // "ERROR: 0:23: ..." (AMD, Intel).
    private static readonly Regex LineNumber = new(@"\b\d+[:(](\d+)\b");

    /// <summary>Adaugă după fiecare mesaj al driverului rândul din fișier la care se referă.</summary>
    private static string Explain(string log, string source)
    {
        string[] lines = source.Replace("\r\n", "\n").Split('\n');
        var result = new List<string>();
        foreach (string message in log.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            result.Add("   " + message.TrimEnd());
            Match match = LineNumber.Match(message);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int n) && n >= 1 && n <= lines.Length)
            {
                result.Add($"   {n,4} | {lines[n - 1].Trim()}");
            }
        }
        return string.Join("\n", result);
    }

    private DateTime FilesTimestamp()
    {
        try
        {
            DateTime v = File.GetLastWriteTimeUtc(VertexPath), f = File.GetLastWriteTimeUtc(FragmentPath);
            return v > f ? v : f;
        }
        catch (IOException)
        {
            return _stamp;
        }
    }

    // Shaderul de rezervă: obiectul apare magenta, semn că fișierele lui au o eroare.
    private const string FallbackVertex = @"#version 330 core
layout (location = 0) in vec3 aPosition;
uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;
void main() { gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0); }";

    private const string FallbackFragment = @"#version 330 core
out vec4 FragColor;
void main()
{
    bool odd = mod(floor(gl_FragCoord.x / 8.0) + floor(gl_FragCoord.y / 8.0), 2.0) > 0.5;
    FragColor = odd ? vec4(1.0, 0.0, 1.0, 1.0) : vec4(0.25, 0.0, 0.25, 1.0);
}";

    public void Dispose()
    {
        GL.DeleteProgram(Handle);
    }
}
