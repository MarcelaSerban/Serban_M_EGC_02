using OpenTK.Mathematics;

namespace Egc;

/// <summary>
/// NOU: jucătorul din scena laboratorului (P4): un cub condus de la tastatură prin ACȚIUNI.
///  - deplasarea folosește acțiuni ȚINUTE (Input.Axis), cu viteză × dt;
///  - saltul "încărcat" folosește toate cele trei stări ale acțiunii Jump:
///    APĂSARE = începe încărcarea, ȚINERE = încărcarea crește, ELIBERARE = cubul sare,
///    cu atât mai sus cu cât a fost ținută tasta mai mult;
///  - zborul folosește gravitația: viteza verticală scade cu g · dt, poziția crește cu v · dt.
/// </summary>
public sealed class PlayerController
{
    public const float Speed = 4.0f;          // unități pe secundă, în plan
    public const float Gravity = 20.0f;       // unități / s²
    public const float MinJumpSpeed = 4.0f;   // viteza verticală la o apăsare scurtă (salt mic)
    public const float MaxJumpSpeed = 11.0f;  // viteza verticală după încărcarea completă
    public const float ChargeTime = 0.8f;     // secunde de ținere pentru încărcarea completă

    private readonly SceneObject _body;
    private readonly SceneObject _shadow;
    private readonly Vector3 _start;
    private readonly float _groundY;          // înălțimea centrului cubului când stă pe podea
    private readonly Vector2 _min, _max;      // limitele zonei de joc, în planul XZ

    private Vector3 _position;                // poziția "logică" (fără turtirea vizuală)
    private float _verticalSpeed;             // viteza pe Y (pozitivă = în sus)
    private bool _charging;                   // tasta de salt este ținută, pe sol
    private float _charge;                    // 0 .. 1

    public PlayerController(SceneObject body, SceneObject shadow, Vector2 min, Vector2 max)
    {
        _body = body;
        _shadow = shadow;
        _start = body.Transform.Position;
        _position = _start;
        _groundY = _start.Y;
        _min = min;
        _max = max;
    }

    public bool OnGround => _position.Y <= _groundY + 1e-4f;
    public Vector3 Position => _position;
    public float Charge => _charge;
    public float VerticalSpeed => _verticalSpeed;

    public void Update(float dt, Input input)
    {
        Vector3 p = _position;

        // 1. Deplasarea în plan (acțiuni ȚINUTE). Pe diagonală vectorul are lungimea √2:
        //    îl normalizăm, altfel cubul ar merge mai repede pe diagonală (ca la camera FPS).
        var direction = new Vector2(input.Axis(Actions.Left, Actions.Right),
                                    input.Axis(Actions.Forward, Actions.Back));   // înainte = -Z
        if (direction.LengthSquared > 1.0f)
        {
            direction.Normalize();
        }
        p.X = Math.Clamp(p.X + direction.X * Speed * dt, _min.X, _max.X);
        p.Z = Math.Clamp(p.Z + direction.Y * Speed * dt, _min.Y, _max.Y);

        // 2. Saltul încărcat: apăsare -> ținere -> eliberare.
        // TODO 6.5:
        //   (a) pe sol, la APĂSAREA acțiunii Jump: _charging = true și _charge = 0;
        //   (b) cât timp _charging și acțiunea este ȚINUTĂ: _charge crește cu dt / ChargeTime, cel mult 1;
        //   (c) la ELIBERARE (dacă _charging): _verticalSpeed = Lerp(MinJumpSpeed, MaxJumpSpeed, _charge),
        //       apoi _charging = false și _charge = 0.

        // (a) pe sol, la APĂSARE: începe încărcarea
        if (OnGround && input.WasPressed(Actions.Jump))
        {
            _charging = true;
            _charge = 0.0f;
        }

        // (b) cât timp se încarcă și tasta e ȚINUTĂ: încărcarea crește, cel mult 1
        if (_charging && input.IsDown(Actions.Jump))
        {
            _charge = MathF.Min(_charge + dt / ChargeTime, 1.0f);
        }

        // (c) la ELIBERARE: salt, cu atât mai înalt cu cât încărcarea e mai mare
        if (_charging && input.WasReleased(Actions.Jump))
        {
            _verticalSpeed = Easing.Lerp(MinJumpSpeed, MaxJumpSpeed, _charge);
            _charging = false;
            _charge = 0.0f;
        }

        // 3. Gravitația (metoda Euler semi-implicită): întâi viteza, apoi poziția.
        if (!OnGround || _verticalSpeed > 0.0f)
        {
            _verticalSpeed -= Gravity * dt;
            p.Y += _verticalSpeed * dt;
            if (p.Y <= _groundY)
            {
                p.Y = _groundY;          // a aterizat
                _verticalSpeed = 0.0f;
            }
        }
        _position = p;

        // 4. Efecte vizuale: cubul se "turtește" cât timp se încarcă saltul și se colorează spre galben;
        //    umbra rămâne pe podea și se micșorează cu înălțimea.
        float squash = 0.35f * _charge;
        _body.Transform.Scale = new Vector3(1.0f + 0.5f * squash, 1.0f - squash, 1.0f + 0.5f * squash);
        _body.Transform.Position = p - new Vector3(0.0f, 0.5f * squash, 0.0f);
        _body.Tint = new Color4(0.3f + 0.7f * _charge, 0.85f, 1.0f - 0.7f * _charge, 1.0f);

        float height = p.Y - _groundY;
        float shadowSize = 1.1f / (1.0f + 0.25f * height);
        _shadow.Transform.Position = new Vector3(p.X, 0.01f, p.Z);
        _shadow.Transform.Scale = new Vector3(shadowSize, 1.0f, shadowSize);
    }

    public void Reset()
    {
        _position = _start;
        _body.Transform.Position = _start;
        _verticalSpeed = 0.0f;
        _charging = false;
        _charge = 0.0f;
    }

    public string State => _charging ? "încarcă saltul" : OnGround ? "pe sol" : "în aer";
}
