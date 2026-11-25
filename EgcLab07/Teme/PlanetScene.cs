// Varianta C: planeta
using OpenTK.Mathematics;

namespace Egc;

/// <summary>
/// Tema 07, varianta C: o planetă desenată în întregime de un fragment shader (planet.frag),
/// pe o sferă simplă. C# calculează doar valori și le trimite ca uniforme.
/// Pasul 1: scena, camera și sfera cu materialul planetei (shaderul arată deocamdată direcția
/// fiecărui punct al sferei, ca să verificăm că vLocalPos ajunge în fragment shader).
/// </summary>
public sealed class PlanetScene : Scene
{
    private Material _planet = null!;

    public override string Name => "planetă";

    protected override void Load(Input input)
    {
        CameraPosition = new Vector3(0.0f, 0.6f, 4.2f);
        CameraTarget = Vector3.Zero;

        Mesh sphere = AddMesh(Shapes.ShadedSphere(1.0f, 96, 48));

        _planet = AddMaterial("planetă", "procedural.vert", "planet.frag");

        SceneObject planet = AddObject("planetă", sphere, _planet);
        planet.Spin = new Vector3(0, 12, 0);   // se rotește încet în jurul axei Y
    }
}
