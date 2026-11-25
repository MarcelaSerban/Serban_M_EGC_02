namespace Egc;

/// <summary>
/// NOU: materialul unui obiect = programul shader cu care se desenează + valorile uniformelor lui.
/// Mai multe obiecte pot folosi același program cu valori diferite (materiale diferite).
/// Parametrii (Param) sunt uniformele float / int pe care le puteți modifica în timpul rulării,
/// cu săgețile (secțiunea 3.5).
/// </summary>
public sealed class Material
{
    public string Name { get; }
    public Shader Shader { get; }
    public UniformSet Uniforms { get; } = new();

    /// <summary>Uniformele reglabile de la tastatură, în ordinea în care au fost adăugate.</summary>
    public List<UniformParam> Params { get; } = new();

    /// <summary>Obiectul este transparent (culoarea se amestecă cu ce este în spate, după alfa).</summary>
    public bool Transparent { get; set; }

    public Material(string name, Shader shader)
    {
        Name = name;
        Shader = shader;
    }

    /// <summary>Adaugă un parametru float reglabil, între <paramref name="min"/> și <paramref name="max"/>.</summary>
    public Material Param(string uniform, float min, float max, float value)
    {
        Params.Add(new UniformParam(this, uniform, min, max, value, integer: false));
        Uniforms.Set(uniform, value);
        return this;
    }

    /// <summary>Adaugă un parametru int reglabil (o apăsare = ±1).</summary>
    public Material Param(string uniform, int min, int max, int value)
    {
        Params.Add(new UniformParam(this, uniform, min, max, value, integer: true));
        Uniforms.Set(uniform, value);
        return this;
    }
}

/// <summary>O uniformă float sau int a unui material, cu limite și valoare implicită.</summary>
public sealed class UniformParam
{
    public Material Owner { get; }
    public string Uniform { get; }
    public float Min { get; }
    public float Max { get; }
    public float Default { get; }
    public bool IsInteger { get; }

    public UniformParam(Material owner, string uniform, float min, float max, float value, bool integer)
    {
        Owner = owner;
        Uniform = uniform;
        Min = min;
        Max = max;
        Default = value;
        IsInteger = integer;
    }

    public string Label => $"{Owner.Name}.{Uniform}";

    /// <summary>Valoarea curentă, citită din / scrisă în setul de uniforme al materialului.</summary>
    public float Value
    {
        get => IsInteger ? Owner.Uniforms.Get<int>(Uniform) : Owner.Uniforms.Get<float>(Uniform);
        set
        {
            float v = Math.Clamp(value, Min, Max);
            if (IsInteger) Owner.Uniforms.Set(Uniform, (int)MathF.Round(v));
            else Owner.Uniforms.Set(Uniform, v);
        }
    }

    public string Describe() =>
        IsInteger ? $"{Label} = {Value:0}   [{Min:0} .. {Max:0}]"
                  : $"{Label} = {Value:0.###}   [{Min:0.###} .. {Max:0.###}]";
}
