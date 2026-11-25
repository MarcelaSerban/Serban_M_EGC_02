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
   //               intoarceti 0 in prima jumatate si 1 in a doua: step(0.5, ...).
   return 0.0;
}

// Tabla de sah: celulele alterneaza pe ambele axe.
float checker(vec2 p)
{
   // TODO 7.4 (b): floor(p.x) si floor(p.y) = indicii celulei (coloana, randul);
   //               suma lor este para sau impara: mod(..., 2.0) da 0 sau 1.
   return 0.0;
}

// Grila: linii subtiri la marginea stanga si la marginea de jos a fiecarei celule.
float gridLines(vec2 p)
{
   // TODO 7.4 (c): vec2 f = fract(p);  punctul este pe o linie daca f.x < uLineWidth SAU f.y < uLineWidth.
   //               Fara if: step(uLineWidth, f.x) este 1 in afara liniei verticale (la fel pentru y),
   //               deci 1.0 - step(uLineWidth, f.x) * step(uLineWidth, f.y) este 1 pe linii.
   return 0.0;
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
