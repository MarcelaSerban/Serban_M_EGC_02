using OpenTK.Mathematics;

namespace Egc;

/// <summary>
/// NOU: clasa de bază a scenelor. O scenă își construiește obiectele, își leagă acțiunile proprii
/// și își actualizează animațiile; Game se ocupă doar de fereastră, ceas, input și desenare.
/// Scena laboratorului este LabScene; fiecare variantă a temei este o scenă nouă (folderul Teme).
/// </summary>
public abstract class Scene : IDisposable
{
    private readonly List<Mesh> _meshes = new();

    /// <summary>Obiectele scenei, în ordinea desenării.</summary>
    public List<SceneObject> Objects { get; } = new();

    /// <summary>Numele afișat în titlul ferestrei.</summary>
    public abstract string Name { get; }

    /// <summary>De unde privește camera fixă și spre ce punct.</summary>
    public Vector3 CameraPosition { get; protected set; } = new(0.0f, 6.0f, 12.0f);
    public Vector3 CameraTarget { get; protected set; } = Vector3.Zero;

    /// <summary>Un text scurt adăugat în titlul ferestrei (scor, stare etc.). Implicit gol.</summary>
    public virtual string Status => "";

    /// <summary>Acțiunile proprii scenei, afișate în meniul H.</summary>
    public virtual IEnumerable<string> ActionNames => Array.Empty<string>();

    /// <summary>Apelată o singură dată, după crearea contextului OpenGL: forme, obiecte, taste.</summary>
    public abstract void Load(Input input);

    /// <summary>
    /// Apelată la fiecare cadru. <paramref name="dt"/> = timpul de JOC (Clock.DeltaTime):
    /// 0 în pauză, scalat cu TimeScale. Implicit rotește obiectele care au Spin.
    /// </summary>
    public virtual void Update(float dt, Input input)
    {
        foreach (SceneObject obj in Objects)
        {
            obj.Update(dt);
        }
    }

    /// <summary>Rândurile afișate la tasta I.</summary>
    public virtual IEnumerable<string> Describe()
    {
        yield return $"Obiecte        : {Objects.Count}";
    }

    /// <summary>Desenează obiectele vizibile care au formă. View și projection sunt deja trimise.</summary>
    public void Draw(Shader shader)
    {
        foreach (SceneObject obj in Objects)
        {
            if (obj.Mesh == null || !obj.Visible) continue;
            shader.SetMatrix4("uModel", obj.WorldMatrix);
            shader.SetColor("uTint", obj.Tint);
            obj.Mesh.Draw();
        }
    }

    // --- Funcții ajutătoare pentru clasele derivate ------------------------------------------

    /// <summary>Reține o formă (pentru eliberare la sfârșit). O formă poate fi folosită de mai multe obiecte.</summary>
    protected Mesh AddMesh(Mesh mesh)
    {
        _meshes.Add(mesh);
        return mesh;
    }

    /// <summary>Creează un obiect (opțional, copilul altuia) și îl adaugă în scenă.</summary>
    protected SceneObject AddObject(string name, Mesh? mesh, SceneObject? parent = null)
    {
        var obj = new SceneObject(name, mesh, parent);
        Objects.Add(obj);
        return obj;
    }

    public void Dispose()
    {
        foreach (Mesh mesh in _meshes)
        {
            mesh.Dispose();
        }
        _meshes.Clear();
        Objects.Clear();
    }
}
