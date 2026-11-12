using OpenTK.Mathematics;

namespace Egc;

/// <summary>Funcții ajutătoare pentru culori.</summary>
public static class ColorUtil
{
    /// <summary>
    /// Convertește o culoare din HSV în RGB.
    /// h = nuanța (hue), în [0, 1): 0 = roșu, 1/6 = galben, 1/3 = verde, 1/2 = cyan, 2/3 = albastru, 5/6 = magenta;
    ///     valorile în afara intervalului sunt aduse în [0, 1) (1.25 devine 0.25), deci h poate crește oricât în timp.
    /// s = saturația, în [0, 1): 0 = gri, 1 = culoare "pură".
    /// v = luminozitatea (value), în [0, 1]: 0 = negru.
    /// </summary>
    public static Color4 FromHsv(float h, float s, float v)
    {
        h -= MathF.Floor(h);                    // aducem h în [0, 1)

        float scaled = h * 6.0f;                // cercul culorilor are 6 sectoare de câte 60 de grade
        float whole = MathF.Floor(scaled);
        float f = scaled - whole;               // poziția în interiorul sectorului, în [0, 1)
        int sector = (int)whole % 6;            // sectorul: 0..5

        float p = v * (1.0f - s);
        float q = v * (1.0f - s * f);
        float t = v * (1.0f - s * (1.0f - f));

        return sector switch
        {
            0 => new Color4(v, t, p, 1.0f),
            1 => new Color4(q, v, p, 1.0f),
            2 => new Color4(p, v, t, 1.0f),
            3 => new Color4(p, q, v, 1.0f),
            4 => new Color4(t, p, v, 1.0f),
            _ => new Color4(v, p, q, 1.0f),
        };
    }
}
