#version 330 core

// P4: un disc cu margine neteda si unde concentrice animate.

in vec3 vColor;
in vec2 vUV;

uniform bool uShowUV;
uniform float uTime;           // timpul de joc, in secunde (se opreste in pauza)
uniform vec2 uCenter;          // centrul discului, in coordonate UV
uniform float uRadius;         // raza discului, in coordonate UV
uniform float uSoftness;       // latimea marginii netede
uniform float uFrequency;      // cate unde pe unitatea de distanta
uniform float uSpeed;          // viteza undelor
uniform vec3 uColorInside;     // culoarea undelor
uniform vec3 uColorOutside;    // culoarea din afara discului

out vec4 FragColor;

void main()
{
    if (uShowUV)
    {
        FragColor = vec4(vUV, 0.0, 1.0);
        return;
    }

    // Distanta fragmentului fata de centrul discului (0 in centru, ~0.5 la marginea panoului).
    float d = distance(vUV, uCenter);

   // TODO 7.5 (a): discul (sectiunea 6.2). inside = 1 in interior, 0 in afara.
   //   Incercati intai marginea "dura":   float inside = step(d, uRadius);
   //   apoi marginea neteda:              1.0 - smoothstep(uRadius - uSoftness, uRadius + uSoftness, d)
   float inside = 0.0;
   // TODO 7.5 (b): undele (sectiunea 6.3): o sinusoida a distantei, deplasata in timp, adusa in [0, 1]:
   //   0.5 + 0.5 * sin(6.2831853 * (d * uFrequency - uTime * uSpeed * 0.25))
   float wave = 1.0;

    vec3 ringColor = mix(uColorInside * 0.35, uColorInside, wave);
    vec3 color = mix(uColorOutside, ringColor, inside);
    FragColor = vec4(color * vColor, 1.0);
}
