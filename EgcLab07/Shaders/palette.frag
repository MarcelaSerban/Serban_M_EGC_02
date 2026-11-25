#version 330 core

// P5: paleta cosinus (Inigo Quilez): o singura formula, patru vectori, o infinitate de palete.

in vec3 vColor;
in vec2 vUV;

uniform bool uShowUV;
uniform float uTime;
uniform vec3 uA;          // luminozitatea medie a fiecarui canal (r, g, b)
uniform vec3 uB;          // amplitudinea (cat de mult variaza canalul)
uniform vec3 uC;          // frecventa (de cate ori se repeta canalul cand t creste cu 1)
uniform vec3 uD;          // faza (decalajul canalelor intre ele)
uniform float uRepeat;    // de cate ori se repeta paleta de la polul sud la polul nord
uniform float uSpeed;     // viteza de "curgere" a culorilor

out vec4 FragColor;

vec3 palette(float t)
{
   // TODO 7.6 (a): culoare(t) = a + b * cos(2 * pi * (c * t + d)), pe toate cele trei canale deodata
   //               (operatiile GLSL cu vec3 se fac pe componente).
   return vec3(0.35);
}

void main()
{
    if (uShowUV)
    {
        FragColor = vec4(vUV, 0.0, 1.0);
        return;
    }

   // TODO 7.6 (b): t creste de la polul sud la polul nord (vUV.y * uRepeat) si curge in timp (uTime * uSpeed).
   float t = vUV.y;
    vec3 color = palette(t);
    FragColor = vec4(color * vColor, 1.0);
}
