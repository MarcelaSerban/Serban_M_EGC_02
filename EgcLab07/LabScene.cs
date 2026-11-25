using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

/// <summary>
/// Scena laboratorului: cinci "exponate", fiecare desenat de alt fragment shader (fișierele din Shaders/):
///   1. degradeul (gradient.frag)      – P2: uniforme vec3, float, int; culori noi cu Space (Tween);
///   2. modelele repetitive (pattern.frag) – P3: dungi, tablă de șah, grilă (fract, floor, mod, step);
///   3. discul cu unde (rings.frag)    – P4: distanța, smoothstep, animația cu uTime;
///   4. sfera cu paletă (palette.frag) – P5: paleta cosinus; C = paleta următoare;
///   5. cubul de marmură (noise.frag)  – P6*: zgomotul (facultativ).
/// Toate folosesc același vertex shader (procedural.vert), care trimite mai departe coordonatele UV.
/// </summary>
public sealed class LabScene : Scene
{
    private const string NewColors = "degrade: culori noi";
    private const string NextPalette = "sferă: paleta următoare";

    private Material _gradient = null!, _pattern = null!, _rings = null!, _palette = null!, _noise = null!;

    // P2 (b): tranziția lină între două perechi de culori (Tween pe Vector3 = culoare RGB).
    private Tween _colorA = null!, _colorB = null!;
    private readonly Random _random = new(7);

    // P5: paletele cosinus (a, b, c, d), după Inigo Quilez; trecerea de la una la alta durează 1 s.
    private static readonly (string Name, Vector3 A, Vector3 B, Vector3 C, Vector3 D)[] Palettes =
    {
        ("curcubeu",  new(0.5f, 0.5f, 0.5f), new(0.5f, 0.5f, 0.5f), new(1.0f, 1.0f, 1.0f), new(0.00f, 0.33f, 0.67f)),
        ("apus",      new(0.5f, 0.5f, 0.5f), new(0.5f, 0.5f, 0.5f), new(1.0f, 1.0f, 0.5f), new(0.80f, 0.90f, 0.30f)),
        ("ocean",     new(0.5f, 0.5f, 0.5f), new(0.5f, 0.5f, 0.5f), new(1.0f, 0.7f, 0.4f), new(0.00f, 0.15f, 0.20f)),
        ("foc",       new(0.5f, 0.3f, 0.1f), new(0.5f, 0.4f, 0.2f), new(1.0f, 1.0f, 1.0f), new(0.00f, 0.10f, 0.20f)),
    };
    private int _paletteIndex;
    private readonly Tween[] _paletteTweens = new Tween[4];
    private static readonly string[] PaletteUniforms = { "uA", "uB", "uC", "uD" };

    public override string Name => "laborator";

    public override IEnumerable<string> ActionNames => new[] { NewColors, NextPalette };

    public override string Status => $"paleta: {Palettes[_paletteIndex].Name}";

    protected override void Load(Input input)
    {
        CameraPosition = new Vector3(0.0f, 2.2f, 8.0f);
        CameraTarget = new Vector3(0.0f, 1.75f, 0.0f);

        input.Bind(NewColors, Keys.Space);
        input.Bind(NextPalette, Keys.C);

        Mesh grid = AddMesh(Shapes.Grid(10, 1.0f, new Color4(0.30f, 0.33f, 0.38f, 1.0f)));
        Mesh quad = AddMesh(Shapes.Quad(2.0f, 2.0f));
        Mesh sphere = AddMesh(Shapes.ShadedSphere(1.0f, 48, 24));
        Mesh cube = AddMesh(Shapes.ShadedCube());
        Mesh post = AddMesh(Shapes.ShadedCylinder(0.06f, 1.0f, 12));

        AddObject("podea", grid);

        // --- Materialele: programul + valorile inițiale ale uniformelor --------------------------
        // Param(...) = uniformă reglabilă cu săgețile: nume, minim, maxim, valoare inițială.

        // 1. Degradeul (P2).
        _gradient = AddMaterial("degrade", "procedural.vert", "gradient.frag")
            .Param("uAngle", 0.0f, 360.0f, 0.0f)
            .Param("uSteps", 0, 12, 0);
        var a0 = new Vector3(0.95f, 0.35f, 0.25f);
        var b0 = new Vector3(0.20f, 0.45f, 0.95f);
        _gradient.Uniforms.Set("uColorA", a0);
        _gradient.Uniforms.Set("uColorB", b0);
        _colorA = new Tween(a0, a0, 0.8f, Easing.EaseInOut);
        _colorB = new Tween(b0, b0, 0.8f, Easing.EaseInOut);

        // 2. Modelele repetitive (P3).
        _pattern = AddMaterial("model", "procedural.vert", "pattern.frag")
            .Param("uPattern", 0, 2, 0)
            .Param("uScale", 1.0f, 20.0f, 6.0f)
            .Param("uLineWidth", 0.01f, 0.5f, 0.08f);
        _pattern.Uniforms.Set("uColorA", new Vector3(0.95f, 0.92f, 0.85f));
        _pattern.Uniforms.Set("uColorB", new Vector3(0.15f, 0.17f, 0.22f));

        // 3. Discul cu unde (P4).
        _rings = AddMaterial("inele", "procedural.vert", "rings.frag")
            .Param("uRadius", 0.05f, 0.7f, 0.42f)
            .Param("uSoftness", 0.001f, 0.2f, 0.01f)
            .Param("uFrequency", 1.0f, 30.0f, 8.0f)
            .Param("uSpeed", 0.0f, 10.0f, 2.0f);
        _rings.Uniforms.Set("uCenter", new Vector2(0.5f, 0.5f));
        _rings.Uniforms.Set("uColorInside", new Vector3(1.0f, 0.75f, 0.2f));
        _rings.Uniforms.Set("uColorOutside", new Vector3(0.12f, 0.14f, 0.2f));

        // 4. Sfera cu paletă (P5).
        _palette = AddMaterial("paletă", "procedural.vert", "palette.frag")
            .Param("uRepeat", 0.2f, 4.0f, 1.0f)
            .Param("uSpeed", 0.0f, 2.0f, 0.15f);
        var p = Palettes[0];
        Vector3[] initial = { p.A, p.B, p.C, p.D };
        for (int i = 0; i < 4; i++)
        {
            _paletteTweens[i] = new Tween(initial[i], initial[i], 1.0f, Easing.EaseInOut);
            _palette.Uniforms.Set(PaletteUniforms[i], initial[i]);
        }

        // 5. Cubul de marmură (P6*).
        _noise = AddMaterial("zgomot", "procedural.vert", "noise.frag")
            .Param("uScale", 1.0f, 16.0f, 4.0f)
            .Param("uTurbulence", 0.0f, 10.0f, 4.0f);
        _noise.Uniforms.Set("uColorA", new Vector3(0.93f, 0.91f, 0.86f));
        _noise.Uniforms.Set("uColorB", new Vector3(0.25f, 0.28f, 0.35f));

        // --- Obiectele: cinci exponate pe un rând, fiecare pe un stâlp -----------------------------
        float[] xs = { -4.8f, -2.4f, 0.0f, 2.4f, 4.8f };
        const float centerY = 2.0f;

        AddObject("degrade", quad, _gradient).Transform.Position = new Vector3(xs[0], centerY, 0);
        AddObject("model", quad, _pattern).Transform.Position = new Vector3(xs[1], centerY, 0);
        AddObject("inele", quad, _rings).Transform.Position = new Vector3(xs[2], centerY, 0);

        SceneObject ball = AddObject("sferă", sphere, _palette);
        ball.Transform.Position = new Vector3(xs[3], centerY, 0);
        ball.Spin = new Vector3(0, 15, 0);

        SceneObject box = AddObject("cub", cube, _noise);
        box.Transform.Position = new Vector3(xs[4], centerY, 0);
        box.Transform.Scale = new Vector3(1.3f);
        box.Transform.Rotation = new Vector3(20, 30, 0);
        box.Spin = new Vector3(0, 20, 0);

        foreach (float x in xs)
        {
            SceneObject stand = AddObject("stâlp", post);
            stand.Transform.Position = new Vector3(x, 0.5f, -0.3f);
            stand.Tint = new Color4(0.55f, 0.57f, 0.62f, 1.0f);
        }
    }

    public override void Update(float dt, Input input)
    {
        base.Update(dt, input);

        // P2 (b): Space alege două culori noi; tween-urile pleacă din culorile curente spre ele.
        if (input.WasPressed(NewColors))
        {
            _colorA.GoTo(RandomColor(), 0.8f);
            _colorB.GoTo(RandomColor(), 0.8f);
        }
        _colorA.Update(dt);
        _colorB.Update(dt);
        // TODO 7.3 (b): la fiecare cadru, copiați valorile tween-urilor (_colorA.Value, _colorB.Value)
        //              în uniformele uColorA și uColorB ale materialului _gradient
        //              (după modelul paletei de mai jos).

        // P5: C trece la paleta următoare; cele patru uniforme vec3 se schimbă lin.
        if (input.WasPressed(NextPalette))
        {
            _paletteIndex = (_paletteIndex + 1) % Palettes.Length;
            var p = Palettes[_paletteIndex];
            Vector3[] target = { p.A, p.B, p.C, p.D };
            for (int i = 0; i < 4; i++) _paletteTweens[i].GoTo(target[i], 1.0f);
            ConsolePanel.PrintMessage($"paleta: {p.Name}");
        }
        for (int i = 0; i < 4; i++)
        {
            _paletteTweens[i].Update(dt);
            _palette.Uniforms.Set(PaletteUniforms[i], _paletteTweens[i].Value);
        }
    }

    /// <summary>O culoare vie, aleatoare: nuanță oarecare, saturație și luminozitate mari.</summary>
    private Vector3 RandomColor()
    {
        Color4 c = ColorUtil.FromHsv(_random.NextSingle(), 0.55f + 0.4f * _random.NextSingle(), 0.95f);
        return new Vector3(c.R, c.G, c.B);
    }

    public override IEnumerable<string> Describe()
    {
        foreach (string line in base.Describe()) yield return line;
        yield return $"Paleta sferei  : {Palettes[_paletteIndex].Name}";
    }
}
