using OpenTK.Mathematics;

namespace Egc;

/// <summary>
/// NOU: un set de valori uniforme, cu nume (secțiunea 3). Valorile se rețin în C# (Set) și se trimit
/// programului shader activ cu Apply, înainte de desenare. O uniformă își păstrează valoarea în program
/// până la următorul GL.Uniform*, dar fiecare PROGRAM are uniformele lui: când schimbăm programul,
/// trebuie să-i trimitem din nou valorile.
/// Tipurile acceptate corespund tipurilor GLSL: float, int, bool, vec2, vec3, vec4 (Color4), mat4.
/// </summary>
public sealed class UniformSet
{
    private readonly Dictionary<string, object> _values = new();
    private static readonly HashSet<Type> Unsupported = new();   // afișate o singură dată

    // Câte o metodă Set pentru fiecare tip acceptat: o valoare de alt tip (de exemplu double)
    // este o eroare de COMPILARE, nu o uniformă care "nu merge".
    public void Set(string name, float value) => _values[name] = value;
    public void Set(string name, int value) => _values[name] = value;
    public void Set(string name, bool value) => _values[name] = value;
    public void Set(string name, Vector2 value) => _values[name] = value;
    public void Set(string name, Vector3 value) => _values[name] = value;
    public void Set(string name, Color4 value) => _values[name] = value;
    public void Set(string name, Matrix4 value) => _values[name] = value;

    /// <summary>Valoarea reținută pentru <paramref name="name"/> (default dacă lipsește sau are alt tip).</summary>
    public T Get<T>(string name) => _values.TryGetValue(name, out object? v) && v is T t ? t : default!;

    public bool Contains(string name) => _values.ContainsKey(name);

    /// <summary>Numele și valorile, pentru afișare (tasta I).</summary>
    public IEnumerable<KeyValuePair<string, object>> Values => _values;

    /// <summary>Trimite toate valorile programului <paramref name="shader"/> (care trebuie să fie ACTIV).</summary>
    public void Apply(Shader shader)
    {
        foreach ((string name, object value) in _values)
        {
            switch (value)
            {
                case float f:
                    shader.SetFloat(name, f);
                    break;
                case int i:
                    shader.SetInt(name, i);
                    break;
                case Color4 c:
                    shader.SetColor(name, c);
                    break;
                case Matrix4 m:
                    shader.SetMatrix4(name, m);
                    break;
                // TODO 7.2: tratați și tipurile Vector2 (SetVector2), Vector3 (SetVector3)
                //           și bool (SetInt, cu 1 pentru true și 0 pentru false), după modelul de mai sus.
                default:
                    if (Unsupported.Add(value.GetType()))
                    {
                        ConsolePanel.PrintMessage($"UniformSet.Apply: tipul {value.GetType().Name} nu este tratat ({name}) – TODO 7.2");
                    }
                    break;
            }
        }
    }

    /// <summary>Valoarea scrisă scurt, pentru consolă.</summary>
    public static string Format(object value) => value switch
    {
        float f => f.ToString("0.###"),
        bool b => b ? "true" : "false",
        Vector2 v => $"({v.X:0.##}, {v.Y:0.##})",
        Vector3 v => $"({v.X:0.##}, {v.Y:0.##}, {v.Z:0.##})",
        Color4 c => $"({c.R:0.##}, {c.G:0.##}, {c.B:0.##}, {c.A:0.##})",
        Matrix4 => "mat4",
        _ => value.ToString() ?? "",
    };
}
