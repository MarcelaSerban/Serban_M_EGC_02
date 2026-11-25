#version 330 core

// Fundalul ferestrei, calculat pentru fiecare pixel (exemplu complet, sectiunea 4.4).
// gl_FragCoord.xy = pozitia pixelului in fereastra, in pixeli (0, 0 = coltul stanga-jos).
// Impartita la uResolution (latimea si inaltimea ferestrei) devine o coordonata in [0, 1].

uniform vec2 uResolution;   // dimensiunea ferestrei, in pixeli (din C#: ClientSize)

out vec4 FragColor;

void main()
{
    vec2 uv = clamp(gl_FragCoord.xy / max(uResolution, vec2(1.0)), 0.0, 1.0);

    // Degrade vertical: mai deschis sus, mai inchis jos.
    vec3 top = vec3(0.17, 0.20, 0.27);
    vec3 bottom = vec3(0.06, 0.07, 0.09);
    vec3 color = mix(bottom, top, uv.y);

    // Vigneta: colturile ecranului putin mai intunecate.
    float d = distance(uv, vec2(0.5));
    color *= 1.0 - 0.6 * smoothstep(0.4, 0.9, d);

    FragColor = vec4(color, 1.0);
}
