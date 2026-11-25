#version 330 core

// Tema 07, varianta C: planeta. Tot desenul se calculeaza aici, din pozitia punctului pe sfera.
// Pasul 1: afisam directia punctului (vLocalPos normalizat) drept culoare.

in vec3 vColor;       // griul umbririi, de la vertex shader
in vec2 vUV;
in vec3 vLocalPos;    // pozitia punctului in object space (o sfera cu raza 1)

uniform bool uShowUV;

out vec4 FragColor;

void main()
{
    if (uShowUV)
    {
        FragColor = vec4(vUV, 0.0, 1.0);
        return;
    }

    vec3 p = normalize(vLocalPos);        // directia punctului: (x, y, z) in [-1, 1]
    vec3 color = p * 0.5 + 0.5;           // adusa in [0, 1] ca sa poata fi culoare

    FragColor = vec4(color * vColor, 1.0);
}
