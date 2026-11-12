using System.Globalization;
using System.Text;

namespace Egc;

/// <summary>
/// Afișarea în fereastra consolă: meniul de taste (H) și parametrii scenei (I).
/// Consola nu se reîmprospătează continuu: textul se scrie doar la cerere sau la schimbarea
/// modului camerei, ca să poată fi citit.
/// </summary>
public static class ConsolePanel
{
    /// <summary>
    /// Pregătește consola: UTF-8 (pentru diacritice) și punctul ca separator zecimal
    /// (altfel, cu setările românești, "(1,70, 6,00)" s-ar confunda cu separatorul dintre coordonate).
    /// </summary>
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

    public static void PrintHelp()
    {
        Console.WriteLine();
        Console.WriteLine("=============== EGC – Laborator 05: comenzi ===============");
        Console.WriteLine(" Mouse          privire în jur (camera FPS; cursorul este captiv)");
        Console.WriteLine(" W A S D        deplasare (Shift = mai repede)");
        Console.WriteLine(" Space / Ctrl   sus / jos (doar în zbor liber)");
        Console.WriteLine(" Rotița         FOV (camera FPS) / distanța (camera orbită)");
        Console.WriteLine(" G              mers <-> zbor liber (FPS)");
        Console.WriteLine(" V              camera FPS <-> camera orbită (tema)");
        Console.WriteLine(" M              eliberează / capturează cursorul");
        Console.WriteLine(" Z              testul de adâncime (depth test) pornit / oprit");
        Console.WriteLine(" C              eliminarea fețelor din spate (face culling) pornit / oprit");
        Console.WriteLine(" F              contur (wireframe) / plin");
        Console.WriteLine(" L              VSync (limitarea cadrelor/secundă) pornit / oprit");
        Console.WriteLine(" R              camera revine în poziția inițială");
        Console.WriteLine(" I              parametrii scenei în consolă");
        Console.WriteLine(" H              acest meniu");
        Console.WriteLine(" Esc            ieșire");
        Console.WriteLine("============================================================");
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
