using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Egc;

/// <summary>
/// Clasa de bază a scenelor (laboratorul 6). NOU în laboratorul 7: scena are MATERIALE.
/// La desenare, pentru fiecare obiect: se activează programul materialului (dacă e altul decât
/// cel activ) și i se trimit uniformele cadrului (uView, uProjection, uTime...), apoi uniformele
/// materialului și ale obiectului (uModel, uTint), apoi se desenează forma.
/// </summary>
public abstract class Scene : IDisposable
{
    private readonly List<Mesh> _meshes = new();

    /// <summary>Obiectele scenei, în ordinea desenării.</summary>
    public List<SceneObject> Objects { get; } = new();

    /// <summary>NOU: materialele scenei (parametrii lor se reglează cu săgețile).</summary>
    public List<Material> Materials { get; } = new();

    /// <summary>Toate programele shader ale aplicației (shaderele se cer de aici, cu Shaders.Get).</summary>
    protected ShaderLibrary Shaders { get; private set; } = null!;

    /// <summary>Materialul obiectelor fără material propriu: basic.vert + basic.frag (culoare × Tint).</summary>
    protected Material Basic { get; private set; } = null!;

    public abstract string Name { get; }

    public Vector3 CameraPosition { get; protected set; } = new(0.0f, 6.0f, 12.0f);
    public Vector3 CameraTarget { get; protected set; } = Vector3.Zero;

    /// <summary>Un text scurt adăugat în titlul ferestrei. Implicit gol.</summary>
    public virtual string Status => "";

    /// <summary>Acțiunile proprii scenei, afișate în meniul H.</summary>
    public virtual IEnumerable<string> ActionNames => Array.Empty<string>();

    /// <summary>Parametrii reglabili ai tuturor materialelor scenei.</summary>
    public IEnumerable<UniformParam> Params => Materials.SelectMany(m => m.Params);

    /// <summary>Apelată de Game o singură dată, după crearea contextului OpenGL.</summary>
    public void Init(Input input, ShaderLibrary shaders)
    {
        Shaders = shaders;
        Basic = new Material("bază", shaders.Get("basic.vert", "basic.frag"));
        Load(input);
    }

    /// <summary>Construiește scena: forme, materiale, obiecte, taste.</summary>
    protected abstract void Load(Input input);

    /// <summary>Apelată la fiecare cadru, cu timpul de JOC. Implicit rotește obiectele care au Spin.</summary>
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
        yield return $"Obiecte        : {Objects.Count}   materiale: {Materials.Count}";
    }

    /// <summary>
    /// Desenează obiectele vizibile. <paramref name="frame"/> = uniformele comune ale cadrului
    /// (uView, uProjection, uTime, uShowUV, uResolution), trimise fiecărui program folosit.
    /// Obiectele transparente se desenează după cele opace.
    /// </summary>
    public void Draw(UniformSet frame)
    {
        Shader? active = null;
        foreach (bool transparentPass in new[] { false, true })
        {
            if (transparentPass)
            {
                // Transparența: culoarea nouă se amestecă cu cea din spate (alfa), iar obiectele
                // transparente nu ascund ce se desenează după ele (nu scriu în bufferul de adâncime).
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                GL.DepthMask(false);
            }

            foreach (SceneObject obj in Objects)
            {
                if (obj.Mesh == null || !obj.Visible) continue;
                Material material = obj.Material ?? Basic;
                if (material.Transparent != transparentPass) continue;

                if (material.Shader != active)
                {
                    active = material.Shader;
                    active.Use();
                    frame.Apply(active);   // fiecare program are uniformele LUI: le trimitem din nou
                }
                material.Uniforms.Apply(active);
                active.SetMatrix4("uModel", obj.WorldMatrix);
                active.SetColor("uTint", obj.Tint);
                obj.Mesh.Draw();
            }
        }
        GL.DepthMask(true);
        GL.Disable(EnableCap.Blend);
    }

    // --- Funcții ajutătoare pentru clasele derivate ------------------------------------------

    protected Mesh AddMesh(Mesh mesh)
    {
        _meshes.Add(mesh);
        return mesh;
    }

    /// <summary>NOU: creează un material cu programul (vertexFile, fragmentFile) și îl adaugă în scenă.</summary>
    protected Material AddMaterial(string name, string vertexFile, string fragmentFile)
    {
        var material = new Material(name, Shaders.Get(vertexFile, fragmentFile));
        Materials.Add(material);
        return material;
    }

    protected SceneObject AddObject(string name, Mesh? mesh, SceneObject? parent = null)
    {
        var obj = new SceneObject(name, mesh, parent);
        Objects.Add(obj);
        return obj;
    }

    /// <summary>Creează un obiect desenat cu materialul <paramref name="material"/>.</summary>
    protected SceneObject AddObject(string name, Mesh mesh, Material material)
    {
        SceneObject obj = AddObject(name, mesh);
        obj.Material = material;
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
