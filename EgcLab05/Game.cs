using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

/// <summary>
/// Fereastra aplicației și bucla principală (main game loop).
/// Față de laboratorul 4: scenă 3D cu forme generate procedural, test de adâncime,
/// camera FPS controlată cu mouse-ul și tastatura, consola pentru meniu și parametri.
/// </summary>
public class Game : GameWindow
{
    // Camerele (nu depind de OpenGL, pot fi create imediat).
    private readonly FpsCamera _fps = new();
    // TEMA: aici se adaugă camera orbită (private readonly OrbitCamera _orbit = new();)
    private Camera _active;                        // camera folosită la desenare

    // Obiectele care folosesc OpenGL se creează în OnLoad (abia atunci există contextul OpenGL).
    private Shader _shader = null!;
    private readonly List<Mesh> _meshes = new();          // toate formele, pentru eliberare în OnUnload
    private readonly List<SceneObject> _scene = new();

    // Starea de desenare (comutată de la tastatură).
    private bool _depthTest;           // tasta Z (după TODO 5.2 îl puteți porni de la început: = true)
    private bool _faceCulling;         // tasta C
    private bool _wireframe;           // tasta F

    // Mouse-ul: după capturarea cursorului sau după schimbarea camerei, prima deplasare
    // citită poate fi foarte mare (cursorul "sare"); o ignorăm.
    private bool _ignoreMouseOnce = true;

    // Contoare afișate în titlul ferestrei.
    private readonly string _baseTitle;
    private double _secondTimer;
    private int _frames, _lastFrames;
    private bool _titleDirty = true;

    public Game(int width, int height, string title)
        : base(GameWindowSettings.Default,
               new NativeWindowSettings
               {
                   ClientSize = new Vector2i(width, height),
                   Title = title,
                   APIVersion = new Version(3, 3),          // cerem OpenGL 3.3 ...
                   Profile = ContextProfile.Core,           // ... profil Core (fără modul imediat)
                   Flags = ContextFlags.ForwardCompatible
               })
    {
        _baseTitle = title;
        _active = _fps;
        VSync = VSyncMode.On;   // swap-ul bufferelor așteaptă reîmprospătarea monitorului
    }

    /// <summary>Apelată O SINGURĂ DATĂ, după crearea ferestrei și a contextului OpenGL.</summary>
    protected override void OnLoad()
    {
        base.OnLoad();
        ConsolePanel.Init();

        Console.WriteLine($"OpenGL  : {GL.GetString(StringName.Version)}");
        Console.WriteLine($"Renderer: {GL.GetString(StringName.Renderer)}");

        GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
        _fps.Resize(ClientSize.X, ClientSize.Y);
        // TEMA: și camera orbită trebuie să afle raportul de aspect

        _shader = Shader.FromFiles("Shaders/shader.vert", "Shaders/shader.frag");

        BuildScene();

        // Camera FPS: cursorul este ascuns și "captiv" în fereastră, iar mouse-ul raportează
        // doar deplasări (nu se oprește la marginea ecranului).
        CursorState = CursorState.Grabbed;

        ConsolePanel.PrintHelp();
        PrintSceneInfo();
    }

    /// <summary>Construiește scena: forme generate procedural, așezate în lume prin Transform3D.</summary>
    private void BuildScene()
    {
        // Formele (fiecare se încarcă o singură dată în GPU, chiar dacă e folosită de mai multe obiecte).
        Mesh grid = AddMesh(Shapes.Grid(10, 1.0f, new Color4(0.35f, 0.38f, 0.42f, 1.0f)));
        Mesh axes = AddMesh(Shapes.Axes(2.0f));
        Mesh pyramid = AddMesh(Shapes.Pyramid());
        Mesh cube = AddMesh(Shapes.Cube());
        Mesh sphere = AddMesh(Shapes.Sphere(1.0f, 32, 16));
        Mesh smallSphere = AddMesh(Shapes.Sphere(0.35f, 16, 8));

        // Podeaua și axele (axele puțin deasupra grilei, ca să nu se suprapună liniile).
        AddObject("podea", grid);
        AddObject("axe", axes).Transform.Position = new Vector3(0.0f, 0.01f, 0.0f);

        // Obiectele din centru.
        AddObject("piramidă", pyramid).Transform.Position = new Vector3(-3.0f, 0.5f, 0.0f);

        SceneObject bigCube = AddObject("cub", cube);
        bigCube.Transform.Position = new Vector3(0.0f, 1.2f, -2.0f);
        bigCube.Transform.Scale = new Vector3(1.2f);
        bigCube.Spin = new Vector3(20.0f, 35.0f, 0.0f);

        SceneObject globe = AddObject("sferă", sphere);
        globe.Transform.Position = new Vector3(3.0f, 1.2f, -1.0f);
        globe.Spin = new Vector3(0.0f, 30.0f, 0.0f);

        // Un "coridor" de stâlpi: același cub, scalat, cu nuanțe diferite; pe fiecare, o sferă mică.
        for (int k = 0; k < 6; k++)
        {
            foreach (float side in new[] { -4.0f, 4.0f })
            {
                float z = -4.0f - 3.0f * k;
                SceneObject pillar = AddObject($"stâlp {k}", cube);
                pillar.Transform.Position = new Vector3(side, 1.5f, z);
                pillar.Transform.Scale = new Vector3(0.6f, 3.0f, 0.6f);
                pillar.Tint = ColorUtil.FromHsv(k / 6.0f, 0.25f, 0.95f);

                AddObject($"bilă {k}", smallSphere).Transform.Position = new Vector3(side, 3.35f, z);
            }
        }

        // TODO 5.8 (facultativ): un cilindru generat procedural, adăugat în scenă
        //           (de exemplu în punctul (-1.5, 0.75, -6)).
    }

    private Mesh AddMesh(Mesh mesh)
    {
        _meshes.Add(mesh);
        return mesh;
    }

    private SceneObject AddObject(string name, Mesh mesh)
    {
        var obj = new SceneObject(name, mesh);
        _scene.Add(obj);
        return obj;
    }

    /// <summary>Apelată când se schimbă dimensiunea ferestrei.</summary>
    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        GL.Viewport(0, 0, e.Width, e.Height);
        _fps.Resize(e.Width, e.Height);     // proiecția se adaptează la noul raport de aspect
        // TEMA: și camera orbită trebuie să afle noul raport de aspect
        _titleDirty = true;
    }

    /// <summary>La revenirea în fereastră (Alt+Tab), prima deplasare a mouse-ului se ignoră.</summary>
    protected override void OnFocusedChanged(FocusedChangedEventArgs e)
    {
        base.OnFocusedChanged(e);
        _ignoreMouseOnce = true;
    }

    /// <summary>Apelată la fiecare iterație a buclei: LOGICA aplicației (intrări, mișcare, timp).</summary>
    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        float dt = (float)args.Time;

        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
            return;
        }

        HandleKeys();

        // --- Camera activă: mouse + tastatură ------------------------------------------

        Vector2 mouseDelta = MouseState.Delta;
        bool fpsWithoutCursor = _active == _fps && CursorState != CursorState.Grabbed;
        if (_ignoreMouseOnce || fpsWithoutCursor)
        {
            mouseDelta = Vector2.Zero;   // camera FPS se rotește doar cu cursorul capturat
            _ignoreMouseOnce = false;
        }

        float fovBefore = _active.FieldOfView;
        _active.Update(KeyboardState, MouseState, mouseDelta, dt);
        if (_active.FieldOfView != fovBefore)
        {
            _titleDirty = true;
        }

        // --- Animația obiectelor -----------------------------------------------------------

        foreach (SceneObject obj in _scene)
        {
            obj.Update(dt);
        }

        UpdateTitle(args.Time);
    }

    /// <summary>Tastele care comută stări (apăsare, nu ținere: IsKeyPressed).</summary>
    private void HandleKeys()
    {
        KeyboardState kb = KeyboardState;

        if (kb.IsKeyPressed(Keys.H)) ConsolePanel.PrintHelp();
        if (kb.IsKeyPressed(Keys.I)) PrintSceneInfo();

        if (kb.IsKeyPressed(Keys.Z)) Toggle(ref _depthTest, "test de adâncime");
        if (kb.IsKeyPressed(Keys.C)) Toggle(ref _faceCulling, "eliminarea fețelor din spate");
        if (kb.IsKeyPressed(Keys.F)) Toggle(ref _wireframe, "contur (wireframe)");

        // L: limitarea cadrelor pe secundă la frecvența monitorului (VSync). Fără VSync, numărul de
        // cadre crește mult; o cameră care folosește corect dt se mișcă la fel de repede în ambele cazuri.
        if (kb.IsKeyPressed(Keys.L))
        {
            VSync = VSync == VSyncMode.On ? VSyncMode.Off : VSyncMode.On;
            ConsolePanel.PrintMessage($"VSync (limitarea cadrelor): {(VSync == VSyncMode.On ? "pornit" : "oprit")}");
        }

        if (kb.IsKeyPressed(Keys.R))
        {
            _active.Reset();
            ConsolePanel.PrintMessage("camera a revenit în poziția inițială");
            _titleDirty = true;
        }

        if (kb.IsKeyPressed(Keys.G) && _active == _fps)
        {
            _fps.FreeFly = !_fps.FreeFly;
            ConsolePanel.PrintMessage($"mod cameră: {_fps.Name}");
            _titleDirty = true;
        }

        if (kb.IsKeyPressed(Keys.M))
        {
            CursorState = CursorState == CursorState.Grabbed ? CursorState.Normal : CursorState.Grabbed;
            _ignoreMouseOnce = true;
            ConsolePanel.PrintMessage(CursorState == CursorState.Grabbed ? "cursor capturat" : "cursor eliberat");
        }

        // TEMA: tasta V comută între camera FPS și camera orbită (vezi textul laboratorului).
        if (kb.IsKeyPressed(Keys.V))
        {
            ConsolePanel.PrintMessage("camera orbită nu este încă implementată (tema)");
        }
    }

    private void Toggle(ref bool flag, string name)
    {
        flag = !flag;
        ConsolePanel.PrintMessage($"{name}: {(flag ? "pornit" : "oprit")}");
    }

    /// <summary>Apelată la fiecare iterație a buclei: DESENAREA unui cadru.</summary>
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        _frames++;

        // 1. Ștergem imaginea veche: bufferul de culoare și bufferul de adâncime.
        GL.ClearColor(0.12f, 0.14f, 0.18f, 1.0f);
        // TODO 5.2: ștergerea bufferului de adâncime la fiecare cadru
        GL.Clear(ClearBufferMask.ColorBufferBit);

        // 2. Starea de rasterizare.
        SetCapability(EnableCap.DepthTest, _depthTest);   // fragmentul mai apropiat câștigă
        SetCapability(EnableCap.CullFace, _faceCulling);  // fețele văzute din spate nu se desenează
        SetWireframe(_wireframe);

        // 3. View și projection sunt aceleași pentru toate obiectele: le trimitem o singură dată.
        _shader.Use();
        _shader.SetMatrix4("uView", _active.ViewMatrix);
        _shader.SetMatrix4("uProjection", _active.ProjectionMatrix);

        // 4. Fiecare obiect: matricea lui model, nuanța, apoi desenarea.
        foreach (SceneObject obj in _scene)
        {
            _shader.SetMatrix4("uModel", obj.Transform.ModelMatrix);
            _shader.SetColor("uTint", obj.Tint);
            obj.Mesh.Draw();
        }

        // 5. Afișăm cadrul terminat: back buffer <-> front buffer.
        SwapBuffers();
    }

    /// <summary>Apelată O SINGURĂ DATĂ, la închiderea ferestrei: eliberăm resursele GPU.</summary>
    protected override void OnUnload()
    {
        foreach (Mesh mesh in _meshes)
        {
            mesh.Dispose();
        }
        _shader.Dispose();

        base.OnUnload();
    }

    // --- Titlul ferestrei și consola -------------------------------------------------------

    private void UpdateTitle(double elapsed)
    {
        _secondTimer += elapsed;
        if (_secondTimer >= 1.0)
        {
            _lastFrames = _frames;
            _frames = 0;
            _secondTimer -= 1.0;
            _titleDirty = true;
        }

        if (_titleDirty)
        {
            Title = $"{_baseTitle} | cameră: {_active.Name} | FOV {_active.FieldOfView:F0}° | FPS: {_lastFrames}";
            _titleDirty = false;
        }
    }

    private void PrintSceneInfo()
    {
        ConsolePanel.PrintInfo(SceneInfo());
    }

    private IEnumerable<string> SceneInfo()
    {
        yield return $"Camera activă  : {_active.Name}";
        foreach (string line in _active.Describe())
        {
            yield return line;
        }
        yield return $"FOV vertical   : {_active.FieldOfView,6:F1}°   orizontal: {_active.HorizontalFov,6:F1}°";
        yield return $"Aspect         : {_active.AspectRatio,6:F3}   ({ClientSize.X} × {ClientSize.Y} pixeli)";
        yield return $"Near / far     : {_active.Near} / {_active.Far}";

        int triangles = 0;
        foreach (SceneObject obj in _scene)
        {
            triangles += obj.Mesh.TriangleCount;
        }
        yield return $"Obiecte        : {_scene.Count}   triunghiuri desenate pe cadru: {triangles}";
        yield return $"Depth test     : {OnOff(_depthTest)}   culling: {OnOff(_faceCulling)}   wireframe: {OnOff(_wireframe)}";
        yield return $"VSync          : {OnOff(VSync == VSyncMode.On)}";
        yield return $"Cadre/secundă  : {_lastFrames}";
    }

    private static string OnOff(bool value) => value ? "pornit" : "oprit";

    // --- Funcții ajutătoare pentru starea OpenGL --------------------------------------------

    private static void SetCapability(EnableCap capability, bool enabled)
    {
        if (enabled) GL.Enable(capability);
        else GL.Disable(capability);
    }

    /// <summary>
    /// Contur / plin. Dacă Visual Studio semnalează o eroare la primul parametru,
    /// folosiți aici tipul acceptat de versiunea voastră (vezi laboratorul 2, Problema 3):
    /// TriangleFace.FrontAndBack sau MaterialFace.FrontAndBack.
    /// </summary>
    private static void SetWireframe(bool wireframe)
    {
        GL.PolygonMode(TriangleFace.FrontAndBack, wireframe ? PolygonMode.Line : PolygonMode.Fill);
    }
}
