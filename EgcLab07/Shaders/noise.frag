#version 330 core

// P6* (facultativ): zgomotul "valoare" (value noise) si marmura.

in vec3 vColor;
in vec2 vUV;

uniform bool uShowUV;
uniform float uTime;
uniform vec3 uColorA;        // culoarea de baza a marmurei
uniform vec3 uColorB;        // culoarea vinelor
uniform float uScale;        // marimea "granulelor" zgomotului
uniform float uTurbulence;   // cat de mult deformeaza zgomotul vinele

out vec4 FragColor;

// Un numar pseudo-aleator in [0, 1), acelasi de fiecare data pentru acelasi punct p.
float hash(vec2 p)
{
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

// Zgomotul: valori aleatoare in colturile unei grile, interpolate lin intre ele.
float valueNoise(vec2 p)
{
   // TODO 7.7 (a): (sectiunea 7.2)
   //   i = floor(p), f = fract(p); valorile din colturi: a = hash(i), b = hash(i + (1, 0)),
   //   c = hash(i + (0, 1)), d = hash(i + (1, 1)); u = f * f * (3.0 - 2.0 * f);
   //   rezultatul: mix(mix(a, b, u.x), mix(c, d, u.x), u.y).
   return hash(floor(p));   // deocamdata: o valoare constanta pe fiecare celula
}

// "Fractal Brownian motion": 5 straturi (octave) de zgomot, fiecare de doua ori mai fin si mai slab.
float fbm(vec2 p)
{
    float sum = 0.0;
    float amplitude = 0.5;
    for (int i = 0; i < 5; i++)
    {
        sum += amplitude * valueNoise(p);
        p *= 2.0;
        amplitude *= 0.5;
    }
    return sum;
}

void main()
{
    if (uShowUV)
    {
        FragColor = vec4(vUV, 0.0, 1.0);
        return;
    }

    vec2 p = vUV * uScale + vec2(uTime * 0.05, 0.0);

   // TODO 7.7 (b): marmura: dungi sinusoidale deformate de zgomot:
   //   float veins = 0.5 + 0.5 * sin(p.x * 3.0 + uTurbulence * fbm(p));
   //   float m = pow(1.0 - veins, 3.0);   // vine subtiri si intense
   float m = fbm(p);
    vec3 color = mix(uColorA, uColorB, m);
    FragColor = vec4(color * vColor, 1.0);
}
