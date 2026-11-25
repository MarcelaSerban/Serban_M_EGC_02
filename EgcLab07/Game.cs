using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

/// <summary>
/// Fereastra aplicației și bucla principală (structura din laboratorul 6). NOU în laboratorul 7:
///   - ShaderLibrary: toate programele shader, reîncărcate la salvarea fișierelor sau cu tasta R;
///   - uniformele CADRULUI (uView, uProjection, uTime, uShowUV, uResolution), trimise fiecărui program;
///   - ParamPanel: reglarea uniformelor materialelor cu săgețile;
///   - fundalul ferestrei, desenat de un fragment shader (background.frag), în locul lui GL.ClearColor.
/// </summary>
public class Game : GameWindow
{
    private readonly Clock _clock = new();
    private readonly Input _input = new();
    private readonly FixedCamera _camera = new();
    private readonly ShaderLibrary _shaders = new();
    private readonly ParamPanel _panel = new();
    private readonly UniformSet _frame = new();   // uniformele comune ale cadrului

    private readonly List<Scene> _scenes = new();
    private int _current;
    private Scene ActiveScene => _scenes[_current];

    // Fundalul: un dreptunghi care acoperă tot ecranul, desenat cu background.frag.
    private Mesh _screenQuad = null!;
    private Shader _background = null!;

    private bool _wireframe;
    private bool _showUV;

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

    protected override void OnLoad()
    {
        base.OnLoad();
        ConsolePanel.Init();
        Console.WriteLine($"OpenGL  : {GL.GetString(StringName.Version)}");
        Console.WriteLine($"Renderer: {GL.GetString(StringName.Renderer)}");
        Console.WriteLine($"GLSL    : {GL.GetString(StringName.ShadingLanguageVersion)}");
        Console.WriteLine($"Shadere : {_shaders.Folder}");

        GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
        _camera.Resize(ClientSize.X, ClientSize.Y);
        GL.Enable(EnableCap.DepthTest);

        _screenQuad = Shapes.Quad(2.0f, 2.0f);
        _background = _shaders.Get("background.vert", "background.frag");

        BindCommonActions();

        _scenes.Add(new LabScene());
        // TEMA: adăugați aici scena (sau scenele) temei; Tab trece de la o scenă la alta.

        foreach (Scene scene in _scenes)
        {
            scene.Init(_input, _shaders);
        }
        ActivateScene(0);
        PrintHelp();
    }

    private void BindCommonActions()
    {
        _input.Bind(Actions.Quit, Keys.Escape);
        _input.Bind(Actions.Help, Keys.H);
        _input.Bind(Actions.Info, Keys.I);
        _input.Bind(Actions.Pause, Keys.P);
        _input.Bind(Actions.Slower, Keys.LeftBracket, Keys.Minus, Keys.KeyPadSubtract);
        _input.Bind(Actions.Faster, Keys.RightBracket, Keys.Equal, Keys.KeyPadAdd);
        _input.Bind(Actions.NormalSpeed, Keys.N);
        _input.Bind(Actions.Wireframe, Keys.F);
        _input.Bind(Actions.VSync, Keys.L);
        _input.Bind(Actions.NextScene, Keys.Tab);

        // NOU: shaderele și uniformele.
        _input.Bind(Actions.ReloadShaders, Keys.R);
        _input.Bind(Actions.ShowUV, Keys.U);
        _input.Bind(Actions.ParamNext, Keys.Down);
        _input.Bind(Actions.ParamPrevious, Keys.Up);
        _input.Bind(Actions.ParamIncrease, Keys.Right);
        _input.Bind(Actions.ParamDecrease, Keys.Left);
        _input.Bind(Actions.ParamReset, Keys.Backspace);
        _input.Bind(Actions.Fine, Keys.LeftShift, Keys.RightShift);
    }

    private static readonly string[] CommonActions =
    {
        Actions.ReloadShaders, Actions.ShowUV,
        Actions.ParamPrevious, Actions.ParamNext, Actions.ParamDecrease, Actions.ParamIncrease, Actions.Fine, Actions.ParamReset,
        Actions.Pause, Actions.Slower, Actions.Faster, Actions.NormalSpeed,
        Actions.NextScene, Actions.Wireframe, Actions.VSync, Actions.Info, Actions.Help, Actions.Quit,
    };

    private void ActivateScene(int index)
    {
        _current = index;
        _camera.Position = ActiveScene.CameraPosition;
        _camera.Target = ActiveScene.CameraTarget;
        _panel.SetParams(ActiveScene.Params);
        ConsolePanel.PrintMessage($"scena: {ActiveScene.Name}");
        _titleTimer = 1.0;
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
        _camera.Resize(e.Width, e.Height);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        _clock.Tick(args.Time);
        _input.Update(KeyboardState, MouseState);

        if (_input.WasPressed(Actions.Quit))
        {
            Close();
            return;
        }
        HandleCommonActions();

        // Shaderele salvate între timp se reîncarcă singure (timp real: și în pauză).
        _shaders.Update(_clock.RealDeltaTime);
        _panel.Update(_input, _clock.RealDeltaTime);

        ActiveScene.Update(_clock.DeltaTime, _input);
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

        if (_input.WasPressed(Actions.ReloadShaders)) _shaders.ReloadAll();
        if (_input.WasPressed(Actions.ShowUV))
        {
            _showUV = !_showUV;
            ConsolePanel.PrintMessage($"uShowUV = {_showUV}");
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

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        _frames++;

        // Uniformele cadrului: aceleași pentru toate obiectele (și pentru toate programele).
        _frame.Set("uView", _camera.ViewMatrix);
        _frame.Set("uProjection", _camera.ProjectionMatrix);
        _frame.Set("uTime", (float)_clock.TotalTime);           // timpul de JOC: se oprește în pauză
        _frame.Set("uShowUV", _showUV);
        _frame.Set("uResolution", new Vector2(ClientSize.X, ClientSize.Y));

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // 1. Fundalul: tot ecranul, fără test de adâncime (orice obiect se va desena peste el).
        GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
        GL.Disable(EnableCap.DepthTest);
        _background.Use();
        _frame.Apply(_background);
        _screenQuad.Draw();
        GL.Enable(EnableCap.DepthTest);

        // 2. Scena.
        GL.PolygonMode(TriangleFace.FrontAndBack, _wireframe ? PolygonMode.Line : PolygonMode.Fill);
        ActiveScene.Draw(_frame);

        SwapBuffers();

        // Uniformele trimise din C# care lipsesc din shadere apar în consolă (o singură dată).
        _shaders.ReportMissingUniforms();
    }

    protected override void OnUnload()
    {
        foreach (Scene scene in _scenes)
        {
            scene.Dispose();
        }
        _screenQuad.Dispose();
        _shaders.Dispose();
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
        if (_titleTimer < 0.2) return;
        _titleTimer = 0.0;

        string pause = _clock.Paused ? " | PAUZĂ" : "";
        string param = _panel.Status.Length > 0 ? $" | {_panel.Status}" : "";
        string status = ActiveScene.Status.Length > 0 ? $" | {ActiveScene.Status}" : "";
        Title = $"{_baseTitle} | {ActiveScene.Name} | t = {_clock.TotalTime:F1} s × {_clock.TimeScale:0.###}{pause}{param}{status} | FPS: {_lastFrames}";
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
            $"Cadre/secundă  : {_lastFrames}   VSync: {(VSync == VSyncMode.On ? "pornit" : "oprit")}",
            $"Shadere        : {_shaders.All.Count()} programe, din {_shaders.Folder}",
        };
        lines.AddRange(ActiveScene.Describe());
        foreach (Material m in ActiveScene.Materials)
        {
            string values = string.Join(", ", m.Uniforms.Values.Select(kv => $"{kv.Key} = {UniformSet.Format(kv.Value)}"));
            lines.Add($"[{m.Name}] {m.Shader.Name}{(m.Shader.IsFallback ? " (REZERVĂ: eroare!)" : "")}: {values}");
        }
        lines.Add("Parametri reglabili (↑ ↓ alegere, ← → valoare):");
        lines.AddRange(_panel.Describe());
        ConsolePanel.PrintInfo(lines);
    }
}
