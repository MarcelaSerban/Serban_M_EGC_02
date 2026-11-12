using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

/// <summary>
/// NOU: gestionarea input-ului prin ACȚIUNI (secțiunea 5). Restul programului nu întreabă
/// "e apăsată tasta Space?", ci "e activă acțiunea Salt?". Legătura acțiune → taste / butoane
/// de mouse se face într-un singur loc (Bind), deci tastele se pot schimba fără a modifica logica.
/// O acțiune poate avea mai multe taste (de exemplu A și săgeata stânga).
/// </summary>
public sealed class Input
{
    private readonly Dictionary<string, List<Keys>> _keys = new();
    private readonly Dictionary<string, List<MouseButton>> _buttons = new();

    // Starea dispozitivelor la cadrul curent (o dă GameWindow; o primim prin Update).
    private KeyboardState? _keyboard;
    private MouseState? _mouse;

    /// <summary>Apelată O DATĂ pe cadru, din Game.OnUpdateFrame, înaintea oricărei interogări.</summary>
    public void Update(KeyboardState keyboard, MouseState mouse)
    {
        _keyboard = keyboard;
        _mouse = mouse;
    }

    // --- Legarea acțiunilor ---------------------------------------------------------------

    /// <summary>Leagă acțiunea <paramref name="action"/> de una sau mai multe taste.</summary>
    public void Bind(string action, params Keys[] keys)
    {
        if (!_keys.TryGetValue(action, out List<Keys>? list))
        {
            list = new List<Keys>();
            _keys[action] = list;
        }
        foreach (Keys key in keys)
        {
            if (!list.Contains(key)) list.Add(key);
        }
    }

    /// <summary>Leagă acțiunea <paramref name="action"/> de un buton al mouse-ului.</summary>
    public void Bind(string action, MouseButton button)
    {
        if (!_buttons.TryGetValue(action, out List<MouseButton>? list))
        {
            list = new List<MouseButton>();
            _buttons[action] = list;
        }
        if (!list.Contains(button)) list.Add(button);
    }

    /// <summary>Șterge toate legăturile unei acțiuni (de exemplu înainte de a o lega altfel).</summary>
    public void Unbind(string action)
    {
        _keys.Remove(action);
        _buttons.Remove(action);
    }

    // --- Interogări: cele trei stări ale unei acțiuni ---------------------------------------

    /// <summary>ȚINUTĂ: cel puțin o tastă / un buton al acțiunii este apăsat în acest cadru.</summary>
    public bool IsDown(string action) =>
        AnyKey(action, k => _keyboard!.IsKeyDown(k)) || AnyButton(action, b => _mouse!.IsButtonDown(b));

    /// <summary>APĂSATĂ ACUM: o tastă a acțiunii era liberă în cadrul anterior și e apăsată în acesta.</summary>
    public bool WasPressed(string action) =>
        AnyKey(action, k => _keyboard!.IsKeyPressed(k)) || AnyButton(action, b => _mouse!.IsButtonPressed(b));

    /// <summary>ELIBERATĂ ACUM: o tastă a acțiunii era apăsată în cadrul anterior și e liberă în acesta.</summary>
    public bool WasReleased(string action)
    {
        // TODO 6.4 (a): după modelul lui WasPressed, cu IsKeyReleased și IsButtonReleased.
        return false;   // provizoriu
    }

    /// <summary>
    /// O AXĂ formată din două acțiuni opuse: -1 (doar <paramref name="negative"/> ținută),
    /// +1 (doar <paramref name="positive"/> ținută), 0 (niciuna sau amândouă).
    /// </summary>
    public float Axis(string negative, string positive)
    {
        // TODO 6.4 (b): (positive ținută ? 1 : 0) - (negative ținută ? 1 : 0)
        return 0.0f;   // provizoriu
    }

    // --- Mouse ----------------------------------------------------------------------------

    /// <summary>Poziția cursorului în fereastră, în pixeli (originea în colțul stânga-sus).</summary>
    public Vector2 MousePosition => _mouse?.Position ?? Vector2.Zero;

    /// <summary>Deplasarea cursorului față de cadrul anterior, în pixeli.</summary>
    public Vector2 MouseDelta => _mouse?.Delta ?? Vector2.Zero;

    /// <summary>Rotația rotiței în acest cadru (pozitivă = în sus).</summary>
    public float Wheel => _mouse?.ScrollDelta.Y ?? 0.0f;

    // --- Afișare (meniul H) -----------------------------------------------------------------

    /// <summary>Un rând pentru fiecare acțiune: numele și tastele ei.</summary>
    public IEnumerable<string> Describe(IEnumerable<string> actions)
    {
        foreach (string action in actions)
        {
            var names = new List<string>();
            if (_keys.TryGetValue(action, out List<Keys>? keys)) names.AddRange(keys.Select(KeyName));
            if (_buttons.TryGetValue(action, out List<MouseButton>? buttons)) names.AddRange(buttons.Select(ButtonName));
            yield return $"{string.Join(" / ", names),-26} {action}";
        }
    }

    private static string KeyName(Keys key) => key switch
    {
        Keys.LeftBracket => "[",
        Keys.RightBracket => "]",
        Keys.Minus => "-",
        Keys.Equal => "=",
        Keys.KeyPadSubtract => "Num -",
        Keys.KeyPadAdd => "Num +",
        Keys.Backspace => "Backspace",
        Keys.Escape => "Esc",
        Keys.KeyPadEnter => "Num Enter",
        _ when key >= Keys.D0 && key <= Keys.D9 => ((int)(key - Keys.D0)).ToString(),
        _ => key.ToString(),
    };

    private static string ButtonName(MouseButton button) => button switch
    {
        MouseButton.Left => "clic stânga",
        MouseButton.Right => "clic dreapta",
        MouseButton.Middle => "clic rotiță",
        _ => $"mouse {button}",
    };

    // --- Funcții ajutătoare -------------------------------------------------------------------

    private bool AnyKey(string action, Func<Keys, bool> test) =>
        _keyboard != null && _keys.TryGetValue(action, out List<Keys>? keys) && keys.Any(test);

    private bool AnyButton(string action, Func<MouseButton, bool> test) =>
        _mouse != null && _buttons.TryGetValue(action, out List<MouseButton>? buttons) && buttons.Any(test);
}

/// <summary>
/// Numele acțiunilor comune tuturor scenelor. Folosim constante în loc de șiruri scrise de mână:
/// o greșeală de scriere devine eroare de compilare, nu o tastă care "nu merge".
/// </summary>
public static class Actions
{
    public const string Quit = "ieșire";
    public const string Help = "meniul de comenzi";
    public const string Info = "parametrii scenei";
    public const string Pause = "pauză";
    public const string Slower = "timpul mai încet (× 0.5)";
    public const string Faster = "timpul mai repede (× 2)";
    public const string NormalSpeed = "timpul normal (× 1)";
    public const string Wireframe = "contur / plin";
    public const string VSync = "VSync pornit / oprit";
    public const string NextScene = "scena următoare";
    public const string Freeze = "simulează o blocare de 0,5 s";

    // Acțiunile jucătorului (scena laboratorului, P4).
    public const string Left = "stânga";
    public const string Right = "dreapta";
    public const string Forward = "înainte";
    public const string Back = "înapoi";
    public const string Jump = "salt (ține = mai sus)";
}
