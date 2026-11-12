using OpenTK.Mathematics;

namespace Egc;

/// <summary>
/// NOU: o animație "de la A la B" (secțiunea 3.3): valoarea merge de la From la To în Duration secunde,
/// cu o funcție de easing. Tween-ul doar CALCULEAZĂ valoarea (Value); scena o copiază unde are nevoie
/// (poziție, scalare, unghiuri...). Cu PingPong = true, animația merge și se întoarce, la nesfârșit.
/// </summary>
public sealed class Tween
{
    public Vector3 From;
    public Vector3 To;
    public float Duration;                 // secunde
    public EasingFunction Ease;
    public bool PingPong;

    /// <summary>Timpul scurs de la pornire, în secunde.</summary>
    public float Elapsed { get; private set; }

    public Tween(Vector3 from, Vector3 to, float duration, EasingFunction ease, bool pingPong = false)
    {
        From = from;
        To = to;
        Duration = MathF.Max(duration, 0.001f);
        Ease = ease;
        PingPong = pingPong;
    }

    /// <summary>Animația a ajuns la capăt (doar fără PingPong).</summary>
    public bool IsFinished => !PingPong && Elapsed >= Duration;

    /// <summary>Cât din drum s-a parcurs, între 0 și 1 (fără easing).</summary>
    public float Progress
    {
        get
        {
            if (!PingPong)
            {
                return MathF.Min(Elapsed / Duration, 1.0f);
            }
            // dus-întors: 0 → 1 în prima durată, 1 → 0 în a doua, apoi din nou
            float p = Elapsed % (2.0f * Duration) / Duration;   // 0 .. 2
            return p <= 1.0f ? p : 2.0f - p;
        }
    }

    /// <summary>Valoarea curentă.</summary>
    public Vector3 Value
    {
        get
        {
            // TODO 6.2 (b): Vector3.Lerp(From, To, ...), cu Ease aplicat pe Progress
            //return From;   // provizoriu: obiectul stă pe loc
            return Vector3.Lerp(From, To, Ease(Progress));
        }

    }

    /// <summary>Avansează animația cu <paramref name="dt"/> secunde (timp de joc).</summary>
    public void Update(float dt)
    {
        if (!IsFinished)
        {
            Elapsed += dt;
        }
    }

    /// <summary>Pornește animația de la început.</summary>
    public void Restart() => Elapsed = 0.0f;

    /// <summary>Pornește o animație nouă, din valoarea curentă spre <paramref name="to"/>.</summary>
    public void GoTo(Vector3 to, float duration)
    {
        From = Value;
        To = to;
        Duration = MathF.Max(duration, 0.001f);
        Elapsed = 0.0f;
    }
}
