using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

/// <summary>
/// Fereastra aplicației și bucla principală (main game loop).
/// Față de laboratorul 5, Game este mai "subțire": nu mai conține scena, ci doar leagă
/// componentele între ele. La fiecare cadru:
///   1. Clock  – transformă timpul real în timp de joc (pauză, scara timpului);
///   2. Input  – citește starea tastaturii și a mouse-ului; restul programului întreabă de ACȚIUNI;
///   3. Scene  – își actualizează animațiile cu timpul de joc și se desenează.
/// </summary>
public class Game : GameWindow
{
    private readonly Clock _clock = new();
    private readonly Input _input = new();
    private readonly FixedCamera _camera = new();

    // Scenele (Tab trece la următoarea). Se încarcă în OnLoad, după crearea contextului OpenGL.
    private readonly List<Scene> _scenes = new();
    private int _current;
    private Scene ActiveScene => _scenes[_current];

    private Shader _shader = null!;
    private bool _wireframe;

    // Titlul ferestrei: se reface de câteva ori pe secundă, nu la fiecare cadru.
    private readonly string _baseTitle;
    private double _titleTimer, _fpsTimer;
    private int _frames, _lastFrames;

    public Game(int width, int height, string title)
        : base(GameWindowSettings.Default,
               new NativeWindowSettings
               {
                   ClientSize = new Vector2i(width, height),
                   Title = title,
                   APIVersion = new Version(3, 3),
                   Profile = ContextProfile.Core,
                   Flags = ContextFlags.ForwardCompatible
               })
    {
        _baseTitle = title;
        VSync = VSyncMode.On;
    }

    /// <summary>Apelată O SINGURĂ DATĂ, după crearea ferestrei și a contextului OpenGL.</summary>
    protected override void OnLoad()
    {
        base.OnLoad();
        ConsolePanel.Init();
        Console.WriteLine($"OpenGL  : {GL.GetString(StringName.Version)}");
        Console.WriteLine($"Renderer: {GL.GetString(StringName.Renderer)}");

        GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
        _camera.Resize(ClientSize.X, ClientSize.Y);
        GL.Enable(EnableCap.DepthTest);

        _shader = Shader.FromFiles("Shaders/shader.vert", "Shaders/shader.frag");

        BindCommonActions();

        _scenes.Add(new LabScene());
        // TEMA: adăugați aici scena (sau scenele) temei; Tab trece de la o scenă la alta.

        foreach (Scene scene in _scenes)
        {
            scene.Load(_input);
        }
        ActivateScene(0);
        PrintHelp();
    }

    /// <summary>Acțiunile comune tuturor scenelor. Tastele se schimbă DOAR aici.</summary>
    private void BindCommonActions()
    {
        _input.Bind(Actions.Quit, Keys.Escape);
        _input.Bind(Actions.Help, Keys.H);
        _input.Bind(Actions.Info, Keys.I);
        _input.Bind(Actions.Pause, Keys.P);
        _input.Bind(Actions.Slower, Keys.LeftBracket, Keys.Minus, Keys.KeyPadSubtract);
        _input.Bind(Actions.Faster, Keys.RightBracket, Keys.Equal, Keys.KeyPadAdd);
        _input.Bind(Actions.NormalSpeed, Keys.N);
        _input.Bind(Actions.Freeze, Keys.B);
        _input.Bind(Actions.Wireframe, Keys.F);
        _input.Bind(Actions.VSync, Keys.L);
        _input.Bind(Actions.NextScene, Keys.Tab);
    }

    private static readonly string[] CommonActions =
    {
        Actions.Pause, Actions.Slower, Actions.Faster, Actions.NormalSpeed, Actions.Freeze,
        Actions.NextScene, Actions.Wireframe, Actions.VSync, Actions.Info, Actions.Help, Actions.Quit,
    };

    private void ActivateScene(int index)
    {
        _current = index;
        _camera.Position = ActiveScene.CameraPosition;
        _camera.Target = ActiveScene.CameraTarget;
        ConsolePanel.PrintMessage($"scena: {ActiveScene.Name}");
        _titleTimer = 1.0;   // titlul se reface imediat
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
        _camera.Resize(e.Width, e.Height);
    }

    /// <summary>Apelată la fiecare iterație a buclei: LOGICA aplicației.</summary>
    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        // 1. Timpul: real -> de joc.  2. Input: starea dispozitivelor pentru acest cadru.
        _clock.Tick(args.Time);
        _input.Update(KeyboardState, MouseState);

        // Un pas real neobișnuit de mare (blocare, fereastra trasă cu mouse-ul) este semnalat în consolă.
        if (_clock.RealDeltaTime > Clock.MaxDelta)
        {
            ConsolePanel.PrintMessage($"pas real {_clock.RealDeltaTime * 1000:F0} ms -> pas de joc {_clock.DeltaTime * 1000:F0} ms");
        }

        if (_input.WasPressed(Actions.Quit))
        {
            Close();
            return;
        }
        HandleCommonActions();

        // 3. Scena, cu timpul de JOC (0 în pauză, scalat cu TimeScale).
        ActiveScene.Update(_clock.DeltaTime, _input);

        // Titlul și contorul de cadre folosesc timpul REAL: trebuie să meargă și în pauză.
        UpdateTitle(_clock.RealDeltaTime);
    }

    private void HandleCommonActions()
    {
        if (_input.WasPressed(Actions.Help)) PrintHelp();
        if (_input.WasPressed(Actions.Info)) PrintInfo();

        if (_input.WasPressed(Actions.Pause))
        {
            _clock.Paused = !_clock.Paused;
            ConsolePanel.PrintMessage(_clock.Paused ? "pauză" : "continuare");
        }
        if (_input.WasPressed(Actions.Slower)) _clock.ChangeScale(0.5f);
        if (_input.WasPressed(Actions.Faster)) _clock.ChangeScale(2.0f);
        if (_input.WasPressed(Actions.NormalSpeed)) _clock.ResetScale();

        // B: simulăm o "înghețare" a programului (ca la tragerea ferestrei sau la o încărcare lungă).
        // Cadrul următor primește un args.Time de aproximativ 0,5 s.
        if (_input.WasPressed(Actions.Freeze))
        {
            ConsolePanel.PrintMessage("blocare simulată: 0,5 s");
            Thread.Sleep(500);
        }

        if (_input.WasPressed(Actions.Wireframe)) _wireframe = !_wireframe;
        if (_input.WasPressed(Actions.VSync))
        {
            VSync = VSync == VSyncMode.On ? VSyncMode.Off : VSyncMode.On;
            ConsolePanel.PrintMessage($"VSync: {(VSync == VSyncMode.On ? "pornit" : "oprit")}");
        }
        if (_input.WasPressed(Actions.NextScene))
        {
            ActivateScene((_current + 1) % _scenes.Count);
        }
    }

    /// <summary>Apelată la fiecare iterație a buclei: DESENAREA unui cadru.</summary>
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        _frames++;

        GL.ClearColor(0.12f, 0.14f, 0.18f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        GL.PolygonMode(TriangleFace.FrontAndBack, _wireframe ? PolygonMode.Line : PolygonMode.Fill);

        _shader.Use();
        _shader.SetMatrix4("uView", _camera.ViewMatrix);
        _shader.SetMatrix4("uProjection", _camera.ProjectionMatrix);
        ActiveScene.Draw(_shader);

        SwapBuffers();
    }

    protected override void OnUnload()
    {
        foreach (Scene scene in _scenes)
        {
            scene.Dispose();
        }
        _shader.Dispose();
        base.OnUnload();
    }

    // --- Titlul ferestrei și consola -------------------------------------------------------

    private void UpdateTitle(double realSeconds)
    {
        _fpsTimer += realSeconds;
        if (_fpsTimer >= 1.0)
        {
            _lastFrames = _frames;
            _frames = 0;
            _fpsTimer -= Math.Floor(_fpsTimer);
        }

        _titleTimer += realSeconds;
        if (_titleTimer < 0.25) return;
        _titleTimer = 0.0;

        string pause = _clock.Paused ? " | PAUZĂ" : "";
        string status = ActiveScene.Status.Length > 0 ? $" | {ActiveScene.Status}" : "";
        Title = $"{_baseTitle} | {ActiveScene.Name} | t = {_clock.TotalTime:F1} s | × {_clock.TimeScale:0.###}{pause}{status} | FPS: {_lastFrames}";
    }

    private void PrintHelp()
    {
        ConsolePanel.PrintHelp(_baseTitle, _input.Describe(CommonActions), _input.Describe(ActiveScene.ActionNames));
    }

    private void PrintInfo()
    {
        var lines = new List<string>
        {
            $"Scena          : {ActiveScene.Name}",
            $"Timp de joc    : {_clock.TotalTime:F2} s   scara: × {_clock.TimeScale:0.###}   pauză: {(_clock.Paused ? "da" : "nu")}",
            $"Δt real / joc  : {_clock.RealDeltaTime * 1000:F1} ms / {_clock.DeltaTime * 1000:F1} ms",
            $"Cadre/secundă  : {_lastFrames}   VSync: {(VSync == VSyncMode.On ? "pornit" : "oprit")}",
        };
        lines.AddRange(ActiveScene.Describe());
        ConsolePanel.PrintInfo(lines);
    }
}
