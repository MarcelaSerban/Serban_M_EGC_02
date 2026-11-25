namespace Egc;

/// <summary>
/// NOU: reglarea uniformelor în timpul rulării (secțiunea 3.5). Lucrează cu parametrii materialelor
/// scenei active: săgeată sus / jos = parametrul anterior / următor, săgeată stânga / dreapta = valoarea
/// mai mică / mai mare (ținută = continuu; Shift = reglaj fin), Backspace = valoarea implicită.
/// Parametrul ales și valoarea lui apar în titlul ferestrei. Folosește timpul REAL: merge și în pauză.
/// </summary>
public sealed class ParamPanel
{
    private List<UniformParam> _params = new();
    private int _index;

    public UniformParam? Selected => _params.Count > 0 ? _params[_index] : null;

    /// <summary>Apelată la schimbarea scenei: parametrii tuturor materialelor ei.</summary>
    public void SetParams(IEnumerable<UniformParam> parameters)
    {
        _params = parameters.ToList();
        _index = 0;
    }

    public void Update(Input input, float realDt)
    {
        if (_params.Count == 0) return;

        if (input.WasPressed(Actions.ParamNext)) Select(_index + 1);
        if (input.WasPressed(Actions.ParamPrevious)) Select(_index - 1);

        UniformParam p = _params[_index];
        float direction = input.Axis(Actions.ParamDecrease, Actions.ParamIncrease);
        if (p.IsInteger)
        {
            // Valorile întregi se schimbă cu câte 1, la fiecare apăsare.
            if (input.WasPressed(Actions.ParamIncrease)) p.Value += 1;
            if (input.WasPressed(Actions.ParamDecrease)) p.Value -= 1;
        }
        else if (direction != 0.0f)
        {
            // Valorile reale se schimbă continuu: tot intervalul în aproximativ 3 s (15 s cu Shift).
            float speed = (p.Max - p.Min) / 3.0f * (input.IsDown(Actions.Fine) ? 0.2f : 1.0f);
            p.Value += direction * speed * realDt;
        }

        if (input.WasPressed(Actions.ParamReset)) p.Value = p.Default;

        // La eliberarea tastei, valoarea finală apare și în consolă (o puteți copia în cod).
        if (input.WasReleased(Actions.ParamIncrease) || input.WasReleased(Actions.ParamDecrease) ||
            input.WasPressed(Actions.ParamReset))
        {
            ConsolePanel.PrintMessage(p.Describe());
        }
    }

    private void Select(int index)
    {
        _index = (index % _params.Count + _params.Count) % _params.Count;
        ConsolePanel.PrintMessage($"parametru: {_params[_index].Describe()}");
    }

    /// <summary>Textul din titlul ferestrei.</summary>
    public string Status
    {
        get
        {
            UniformParam? p = Selected;
            if (p == null) return "";
            string value = p.IsInteger ? $"{p.Value:0}" : $"{p.Value:0.###}";
            return $"{p.Label} = {value} ({_index + 1}/{_params.Count})";
        }
    }

    /// <summary>Rândurile afișate la tasta I.</summary>
    public IEnumerable<string> Describe()
    {
        for (int i = 0; i < _params.Count; i++)
        {
            yield return $"{(i == _index ? "►" : " ")} {_params[i].Describe()}";
        }
    }
}
