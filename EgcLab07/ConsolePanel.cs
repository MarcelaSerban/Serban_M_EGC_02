using System.Globalization;
using System.Text;

namespace Egc;

/// <summary>
/// Afișarea în fereastra consolă: meniul de comenzi (H) și parametrii scenei (I).
/// Față de laboratorul 5, meniul nu mai este scris de mână: îl generează Input din legăturile
/// acțiunilor, deci rămâne corect și după ce schimbați tastele.
/// </summary>
public static class ConsolePanel
{
    /// <summary>Pregătește consola: UTF-8 (pentru diacritice) și punctul ca separator zecimal.</summary>
    public static void Init()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        try
        {
            Console.OutputEncoding = Encoding.UTF8;
        }
        catch (IOException)
        {
            // Ieșirea este redirecționată (de exemplu într-un fișier): nu contează.
        }
    }

    public static void PrintHelp(string title, IEnumerable<string> common, IEnumerable<string> scene)
    {
        Console.WriteLine();
        Console.WriteLine($"=============== {title}: comenzi ===============");
        foreach (string line in common) Console.WriteLine(" " + line);
        Console.WriteLine(" ---- scena curentă ----");
        foreach (string line in scene) Console.WriteLine(" " + line);
        Console.WriteLine("=================================================================");
    }

    public static void PrintInfo(IEnumerable<string> lines)
    {
        Console.WriteLine();
        Console.WriteLine("--------------------- parametrii scenei ---------------------");
        foreach (string line in lines)
        {
            Console.WriteLine(" " + line);
        }
        Console.WriteLine("-------------------------------------------------------------");
    }

    public static void PrintMessage(string message)
    {
        Console.WriteLine($" > {message}");
    }
}
