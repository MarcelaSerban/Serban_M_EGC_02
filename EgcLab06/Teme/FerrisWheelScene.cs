// Varianta C: roata panoramica
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

/// <summary>
/// Tema 06, varianta C: roata panoramica.
///  - Butucul (pivot) se roteste si poarta spitele, janta si 8 cabine (ierarhie parinte-copil).
///  - Fiecare cabina are propriul pivot, care se roteste INVERS fata de roata: cabina ramane verticala.
///  - Sageti sus / jos: schimba viteza tinta. Viteza reala se apropie lin: v += (tinta - v) * (1 - e^(-k dt)).
///  - Space: opreste roata (lin) / o porneste la loc.
///  - Tastele 1-8: aduc cabina aleasa jos, la peron, cu un Tween pe unghiul rotii, pe drumul cel mai scurt.
///  - Bonus: cabinele se leagana (resort amortizat) cand roata accelereaza sau franeaza.
/// Toate miscarile folosesc doar timpul de joc dt.
/// </summary>
public sealed class FerrisWheelScene : Scene
{
    // --- Actiunile scenei (meniul H) ---
    private const string SpeedUp = "viteza tinta mai mare";
    private const string SpeedDown = "viteza tinta mai mica";
    private const string StopGo = "opreste / porneste roata";
    private static readonly string[] CabinActions =
        Enumerable.Range(1, 8).Select(k => $"cabina {k} jos, la peron").ToArray();

    // --- Dimensiuni ---
    private const int CabinCount = 8;
    private const float Radius = 4.8f;                 // raza rotii (pana la centrul jantei)
    private const float HubHeight = 6.2f;              // inaltimea butucului deasupra solului
    private const float CabinHang = 0.55f;             // distanta de la punctul de prindere la centrul cabinei

    // --- Viteza ---
    private const float MaxTargetSpeed = 60.0f;        // grade / secunda
    private const float TargetStep = 5.0f;             // o apasare = +/- 5 grade / secunda
    private const float Smoothing = 2.0f;              // k din formula v += (tinta - v)(1 - e^(-k dt))

    // --- Resortul cabinelor (leganarea) ---
    private const float SwayStiffness = 16.0f;         // k: forta de revenire
    private const float SwayDamping = 2.0f;            // amortizarea
    private const float SwayDrive = 0.4f;              // cat de mult le "trage" accelerarea rotii
    private const float SwayLimit = 35.0f;             // grade

    private SceneObject _hub = null!;
    private readonly SceneObject[] _cabinPivots = new SceneObject[CabinCount];
    private readonly float[] _swayAngle = new float[CabinCount];      // grade
    private readonly float[] _swayVelocity = new float[CabinCount];   // grade / secunda

    private float _angle;                 // unghiul rotii, in grade
    private float _speed;                 // viteza reala, grade / secunda (cu semn)
    private float _targetSpeed = 15.0f;   // viteza dorita
    private bool _stopped;                // Space: tinta efectiva devine 0

    private Tween? _goTo;                 // animatia "adu cabina jos" (valoarea X = unghiul rotii)
    private int _goToCabin = -1;
    private float _lastSpeed;             // viteza din cadrul anterior (pentru acceleratie)

    public override string Name => "roata panoramica";

    public override string Status =>
        _goTo != null && !_goTo.IsFinished ? $"cabina {_goToCabin + 1} coboara la peron"
        : _stopped ? $"roata oprita ({_speed:F0} grade/s)"
        : $"viteza {_speed:F0} / tinta {_targetSpeed:F0} grade/s";

    public override IEnumerable<string> ActionNames =>
        new[] { SpeedUp, SpeedDown, StopGo }.Concat(CabinActions);

    public override void Load(Input input)
    {
        CameraPosition = new Vector3(0.0f, 6.0f, 19.0f);
        CameraTarget = new Vector3(0.0f, 5.2f, 0.0f);

        // Taste -> actiuni (singurul loc unde apar tastele).
        input.Bind(SpeedUp, Keys.Up);
        input.Bind(SpeedDown, Keys.Down);
        input.Bind(StopGo, Keys.Space);
        for (int k = 0; k < CabinCount; k++)
        {
            input.Bind(CabinActions[k], Keys.D1 + k);
        }

        Mesh grid = AddMesh(Shapes.Grid(12, 1.0f, new Color4(0.30f, 0.33f, 0.38f, 1.0f)));
        Mesh cube = AddMesh(Shapes.ShadedCube());
        Mesh cylinder = AddMesh(Shapes.ShadedCylinder(0.5f, 1.0f, 24));

        AddObject("podea", grid);
        BuildSupport(cube);
        BuildWheel(cube, cylinder);
    }

    // --- Constructia ---------------------------------------------------------------------------

    /// <summary>Doua picioare inclinate (de la sol la butuc) si peronul din fata rotii.</summary>
    private void BuildSupport(Mesh cube)
    {
        float legSpread = 3.2f;
        float legLength = MathF.Sqrt(legSpread * legSpread + HubHeight * HubHeight);
        float legAngle = MathHelper.RadiansToDegrees(MathF.Atan2(legSpread, HubHeight));   // fata de verticala

        foreach (float side in new[] { -1.0f, 1.0f })
        {
            foreach (float z in new[] { -0.6f, 0.6f })
            {
                SceneObject leg = AddObject("picior", cube);
                leg.Transform.Position = new Vector3(side * legSpread / 2.0f, HubHeight / 2.0f, z);
                leg.Transform.Scale = new Vector3(0.18f, legLength, 0.18f);
                leg.Transform.Rotation = new Vector3(0.0f, 0.0f, side * legAngle);
                leg.Tint = new Color4(0.55f, 0.55f, 0.6f, 1.0f);
            }
        }

        // Peronul, in fata cabinei de jos (z > 0), ca roata sa nu il loveasca.
        SceneObject platform = AddObject("peron", cube);
        platform.Transform.Position = new Vector3(0.0f, 0.2f, 1.6f);
        platform.Transform.Scale = new Vector3(3.0f, 0.4f, 1.6f);
        platform.Tint = new Color4(0.8f, 0.7f, 0.45f, 1.0f);
    }

    /// <summary>Butucul (pivot) cu spite, janta si 8 cabine. Totul este copilul butucului.</summary>
    private void BuildWheel(Mesh cube, Mesh cylinder)
    {
        _hub = AddObject("butuc", null);
        _hub.Transform.Position = new Vector3(0.0f, HubHeight, 0.0f);

        // Discul butucului: cilindrul are axa Y, il culcam cu axa pe Z.
        SceneObject hubDisk = AddObject("disc butuc", cylinder, _hub);
        hubDisk.Transform.Rotation = new Vector3(90.0f, 0.0f, 0.0f);
        hubDisk.Transform.Scale = new Vector3(0.9f, 0.5f, 0.9f);
        hubDisk.Tint = new Color4(0.9f, 0.3f, 0.3f, 1.0f);

        // Spitele: 8, cate una spre fiecare cabina (cub lung 'Radius', rotit cu unghiul cabinei).
        for (int k = 0; k < CabinCount; k++)
        {
            float a = k * 360.0f / CabinCount;
            float rad = MathHelper.DegreesToRadians(a);
            SceneObject spoke = AddObject("spita", cube, _hub);
            spoke.Transform.Position = new Vector3(MathF.Cos(rad), MathF.Sin(rad), 0.0f) * (Radius / 2.0f);
            spoke.Transform.Scale = new Vector3(Radius, 0.07f, 0.07f);
            spoke.Transform.Rotation = new Vector3(0.0f, 0.0f, a);
            spoke.Tint = new Color4(0.85f, 0.85f, 0.9f, 1.0f);
        }

        // Janta: 24 de segmente drepte, fiecare tangent la cerc (aproximeaza cercul).
        const int segments = 24;
        float chord = 2.0f * Radius * MathF.Sin(MathF.PI / segments);
        for (int s = 0; s < segments; s++)
        {
            float a = s * 360.0f / segments;
            float rad = MathHelper.DegreesToRadians(a);
            SceneObject piece = AddObject("janta", cube, _hub);
            piece.Transform.Position = new Vector3(MathF.Cos(rad), MathF.Sin(rad), 0.0f) * Radius;
            piece.Transform.Scale = new Vector3(0.09f, chord * 1.05f, 0.09f);
            piece.Transform.Rotation = new Vector3(0.0f, 0.0f, a);   // segmentul lung (Y) devine tangent
            piece.Tint = new Color4(0.95f, 0.8f, 0.3f, 1.0f);
        }

        // Cabinele: pivotul sta pe janta si se roteste INVERS fata de roata; cabina atarna sub pivot.
        for (int k = 0; k < CabinCount; k++)
        {
            float rad = MathHelper.DegreesToRadians(k * 360.0f / CabinCount);
            SceneObject pivot = AddObject($"pivot cabina {k + 1}", null, _hub);
            pivot.Transform.Position = new Vector3(MathF.Cos(rad), MathF.Sin(rad), 0.0f) * Radius;
            _cabinPivots[k] = pivot;

            SceneObject hanger = AddObject("agatatoare", cube, pivot);
            hanger.Transform.Position = new Vector3(0.0f, -CabinHang / 2.0f, 0.0f);
            hanger.Transform.Scale = new Vector3(0.05f, CabinHang, 0.05f);
            hanger.Tint = new Color4(0.8f, 0.8f, 0.85f, 1.0f);

            SceneObject cabin = AddObject($"cabina {k + 1}", cube, pivot);
            cabin.Transform.Position = new Vector3(0.0f, -CabinHang - 0.3f, 0.0f);
            cabin.Transform.Scale = new Vector3(1.0f, 0.7f, 0.8f);
            cabin.Tint = ColorUtil.FromHsv(k / (float)CabinCount, 0.7f, 1.0f);
        }

        ApplyWheelTransforms();
    }

    // --- Actualizarea, la fiecare cadru ----------------------------------------------------------

    public override void Update(float dt, Input input)
    {
        base.Update(dt, input);

        HandleInput(input);

        float previousAngle = _angle;

        if (_goTo != null)
        {
            // Animatia "adu cabina jos": unghiul rotii este valoarea Tween-ului (EaseInOut).
            _goTo.Update(dt);
            _angle = _goTo.Value.X;
            if (_goTo.IsFinished)
            {
                _goTo = null;
                _angle = NormalizeDegrees(_angle);
                _stopped = true;       // roata ramane la peron; Space o porneste din nou
                _speed = 0.0f;
                ConsolePanel.PrintMessage($"cabina {_goToCabin + 1} este la peron");
            }
            else if (dt > 1e-6f)
            {
                _speed = (_angle - previousAngle) / dt;     // viteza "efectiva", pentru leganare
            }
        }
        else
        {
            // Viteza reala se apropie lin de tinta, la orice numar de cadre pe secunda:
            //   v <- v + (v_tinta - v) * (1 - e^(-k dt))
            float target = _stopped ? 0.0f : _targetSpeed;
            _speed += (target - _speed) * (1.0f - MathF.Exp(-Smoothing * dt));
            _angle = NormalizeDegrees(_angle + _speed * dt);
        }

        // Acceleratia unghiulara a rotii (grade / s^2), folosita de resorturile cabinelor.
        float acceleration = 0.0f;
        if (dt > 1e-6f)
        {
            acceleration = (_speed - _lastSpeed) / dt;
        }
        _lastSpeed = _speed;

        UpdateSway(dt, acceleration);
        ApplyWheelTransforms();
    }

    private void HandleInput(Input input)
    {
        bool manual = false;

        if (input.WasPressed(SpeedUp)) { _targetSpeed = MathF.Min(_targetSpeed + TargetStep, MaxTargetSpeed); manual = true; }
        if (input.WasPressed(SpeedDown)) { _targetSpeed = MathF.Max(_targetSpeed - TargetStep, -MaxTargetSpeed); manual = true; }

        if (manual)
        {
            _goTo = null;            // un comandat manual anuleaza coborarea la peron
            _stopped = false;
            ConsolePanel.PrintMessage($"viteza tinta: {_targetSpeed:F0} grade/s");
        }

        if (input.WasPressed(StopGo))
        {
            _goTo = null;
            _stopped = !_stopped;
            ConsolePanel.PrintMessage(_stopped ? "roata se opreste" : "roata porneste");
        }

        for (int k = 0; k < CabinCount; k++)
        {
            if (input.WasPressed(CabinActions[k]))
            {
                StartGoTo(k);
            }
        }
    }

    /// <summary>Porneste Tween-ul care aduce cabina k jos, pe drumul cel mai scurt (cel mult 180 de grade).</summary>
    private void StartGoTo(int cabin)
    {
        // Cabina k este la unghiul 45 * k in roata. Jos inseamna -90 in lume:
        //   unghiul rotii + 45 * k = -90 (mod 360)
        float wanted = -90.0f - cabin * 360.0f / CabinCount;
        float delta = NormalizeSigned(wanted - _angle);                 // in [-180, 180]: drumul cel mai scurt
        float duration = Math.Clamp(MathF.Abs(delta) / 90.0f, 0.4f, 2.0f);

        _goTo = new Tween(new Vector3(_angle, 0.0f, 0.0f), new Vector3(_angle + delta, 0.0f, 0.0f),
                          duration, Easing.EaseInOut);
        _goToCabin = cabin;
        _stopped = false;
        ConsolePanel.PrintMessage($"cabina {cabin + 1} coboara la peron ({delta:F0} grade)");
    }

    /// <summary>
    /// Bonus: fiecare cabina este un pendul cu resort amortizat. Cand roata accelereaza, punctul de prindere
    /// accelereaza tangential si cabina ramane in urma; resortul o aduce inapoi.
    /// </summary>
    private void UpdateSway(float dt, float wheelAcceleration)
    {
        if (dt <= 0.0f) return;

        float accelerationRad = MathHelper.DegreesToRadians(wheelAcceleration);
        for (int k = 0; k < CabinCount; k++)
        {
            float worldAngle = MathHelper.DegreesToRadians(_angle + k * 360.0f / CabinCount);
            // componenta orizontala a acceleratiei punctului de prindere: -alpha * R * sin(unghi)
            float horizontal = -accelerationRad * Radius * MathF.Sin(worldAngle);
            float drive = MathHelper.RadiansToDegrees(-horizontal / CabinHang) * SwayDrive;

            // Euler semi-implicit: intai viteza, apoi pozitia
            float force = drive - SwayStiffness * _swayAngle[k] - SwayDamping * _swayVelocity[k];
            _swayVelocity[k] += force * dt;
            _swayAngle[k] += _swayVelocity[k] * dt;
            _swayAngle[k] = Math.Clamp(_swayAngle[k], -SwayLimit, SwayLimit);
        }
    }

    /// <summary>Roata se roteste cu _angle, iar fiecare cabina cu -_angle (+ leganarea), deci ramane verticala.</summary>
    private void ApplyWheelTransforms()
    {
        _hub.Transform.Rotation = new Vector3(0.0f, 0.0f, _angle);
        for (int k = 0; k < CabinCount; k++)
        {
            _cabinPivots[k].Transform.Rotation = new Vector3(0.0f, 0.0f, -_angle + _swayAngle[k]);
        }
    }

    public override IEnumerable<string> Describe()
    {
        foreach (string line in base.Describe()) yield return line;
        yield return $"Roata          : unghi {_angle:F1} grade   viteza {_speed:F1} grade/s   tinta {(_stopped ? 0.0f : _targetSpeed):F0} grade/s" +
                     (_stopped ? "   (oprita)" : "");
        yield return $"Coborare (Tween): {(_goTo != null ? $"cabina {_goToCabin + 1}, progres {_goTo.Progress:P0}" : "inactiva")}";
        yield return "Leganare       : " + string.Join("  ", _swayAngle.Select((a, k) => $"{k + 1}:{a:F1}"));
    }

    // --- Functii ajutatoare ------------------------------------------------------------------

    /// <summary>Adus in [0, 360).</summary>
    private static float NormalizeDegrees(float degrees)
    {
        degrees %= 360.0f;
        return degrees < 0.0f ? degrees + 360.0f : degrees;
    }

    /// <summary>Adus in [-180, 180]: sensul cel mai scurt.</summary>
    private static float NormalizeSigned(float degrees)
    {
        degrees = NormalizeDegrees(degrees);
        return degrees > 180.0f ? degrees - 360.0f : degrees;
    }
}
