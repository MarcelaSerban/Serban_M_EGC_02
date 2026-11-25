namespace Egc;

/// <summary>
/// NOU: toate programele shader ale aplicației, încărcate o singură dată (secțiunea 2).
/// Fișierele se citesc din folderul Shaders al PROIECTULUI (lângă .csproj), nu din copia de lângă
/// executabil (bin/...): așa, ce salvați în Visual Studio se vede imediat, fără recompilarea programului C#.
/// La fiecare jumătate de secundă verifică datele fișierelor și reîncarcă shaderele salvate între timp;
/// tasta R le reîncarcă pe toate.
/// </summary>
public sealed class ShaderLibrary : IDisposable
{
    private readonly Dictionary<string, Shader> _shaders = new();
    private double _checkTimer;

    /// <summary>Folderul din care se citesc fișierele .vert și .frag.</summary>
    public string Folder { get; } = FindFolder();

    /// <summary>Reîncărcarea automată la salvarea fișierelor (pornită implicit).</summary>
    public bool AutoReload { get; set; } = true;

    public IEnumerable<Shader> All => _shaders.Values;

    /// <summary>Programul format din cele două fișiere (de exemplu "procedural.vert", "rings.frag").</summary>
    public Shader Get(string vertexFile, string fragmentFile)
    {
        string key = $"{vertexFile}|{fragmentFile}";
        if (!_shaders.TryGetValue(key, out Shader? shader))
        {
            shader = new Shader(Path.Combine(Folder, vertexFile), Path.Combine(Folder, fragmentFile));
            _shaders[key] = shader;
        }
        return shader;
    }

    /// <summary>Reîncarcă toate programele (tasta R).</summary>
    public void ReloadAll()
    {
        int ok = 0;
        foreach (Shader shader in _shaders.Values)
        {
            shader.FilesChanged();   // reținem data curentă a fișierelor
            if (shader.Reload()) ok++;
        }
        ConsolePanel.PrintMessage($"shadere reîncărcate: {ok} din {_shaders.Count}");
    }

    /// <summary>Apelată la fiecare cadru, cu timpul REAL: reîncarcă shaderele ale căror fișiere s-au schimbat.</summary>
    public void Update(float realDt)
    {
        _checkTimer += realDt;
        if (!AutoReload || _checkTimer < 0.5) return;
        _checkTimer = 0.0;

        foreach (Shader shader in _shaders.Values)
        {
            if (shader.FilesChanged() && shader.Reload())
            {
                ConsolePanel.PrintMessage($"{shader.Name}: reîncărcat ({DateTime.Now:HH:mm:ss})");
            }
        }
    }

    /// <summary>Afișează uniformele cerute din C# dar absente din shadere (o singură dată pentru fiecare).</summary>
    public void ReportMissingUniforms()
    {
        foreach (Shader shader in _shaders.Values)
        {
            shader.ReportMissing();
        }
    }

    /// <summary>Caută folderul Shaders al proiectului, urcând din folderul executabilului până la .csproj.</summary>
    private static string FindFolder()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (dir.GetFiles("*.csproj").Length > 0 && Directory.Exists(Path.Combine(dir.FullName, "Shaders")))
            {
                return Path.Combine(dir.FullName, "Shaders");
            }
            dir = dir.Parent;
        }
        // Programul rulează fără sursele proiectului: folosim copia de lângă executabil.
        return Path.Combine(AppContext.BaseDirectory, "Shaders");
    }

    public void Dispose()
    {
        foreach (Shader shader in _shaders.Values)
        {
            shader.Dispose();
        }
        _shaders.Clear();
    }
}
