#version 330 core

// P3: modele repetitive. Coordonatele UV se inmultesc cu uScale, deci pe panou
// incap uScale "celule"; fiecare functie intoarce 0 (culoarea A) sau 1 (culoarea B).

in vec3 vColor;
in vec2 vUV;

uniform bool uShowUV;
uniform vec3 uColorA;
uniform vec3 uColorB;
uniform int uPattern;       // 0 = dungi, 1 = tabla de sah, 2 = grila
uniform float uScale;       // cate celule pe latura panoului
uniform float uLineWidth;   // grosimea liniilor grilei, ca fractiune din celula

out vec4 FragColor;

// Dungi verticale: jumatate din fiecare celula A, jumatate B.
float stripes(vec2 p)
{
    // TODO 7.4 (a): pozitia in celula este fract(p.x), in [0, 1);
    // step(0.5, x) da 0 pentru x < 0.5 si 1 pentru x >= 0.5: prima jumatate A, a doua B.
    return step(0.5, fract(p.x));
}

// Tabla de sah: celulele alterneaza pe ambele axe.
float checker(vec2 p)
{
    // TODO 7.4 (b): floor(p.x) si floor(p.y) = indicii celulei (coloana, randul);
    // suma lor este para sau impara: mod(..., 2.0) da 0 sau 1.
    return mod(floor(p.x) + floor(p.y), 2.0);
}

// Grila: linii subtiri la marginea stanga si la marginea de jos a fiecarei celule.
float gridLines(vec2 p)
{
    // TODO 7.4 (c): f = pozitia in celula; punctul e pe o linie daca f.x < uLineWidth SAU f.y < uLineWidth.
    // Fara if: step(w, f.x) este 1 in afara liniei verticale (la fel pentru y), deci
    // 1.0 - step(w, f.x) * step(w, f.y) este 1 pe linii.
    vec2 f = fract(p);
    return 1.0 - step(uLineWidth, f.x) * step(uLineWidth, f.y);
}

void main()
{
    if (uShowUV)
    {
        FragColor = vec4(vUV, 0.0, 1.0);
        return;
    }

    vec2 p = vUV * uScale;
    float m;
    if (uPattern == 0)
        m = stripes(p);
    else if (uPattern == 1)
        m = checker(p);
    else
        m = gridLines(p);

    vec3 color = mix(uColorA, uColorB, m);
    FragColor = vec4(color * vColor, 1.0);
}
