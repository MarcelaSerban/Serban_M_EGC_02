#version 330 core

// Vertex shaderul comun al materialelor procedurale (laboratorul 7).
// Nu calculeaza nicio culoare: transforma pozitia si trimite mai departe datele varfului.
// Rasterizarea le INTERPOLEAZA, deci fiecare fragment primeste valori "amestecate"
// din cele trei varfuri ale triunghiului sau.

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aColor;    // la formele "Shaded": griul umbririi
layout (location = 2) in vec2 aUV;       // NOU: coordonatele de suprafata, in [0, 1]

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

out vec3 vColor;      // griul umbririi (alb la Quad)
out vec2 vUV;         // coordonatele UV ale fragmentului
out vec3 vLocalPos;   // pozitia in object space (pentru modele 3D, de exemplu la tema C)

void main()
{
    vColor = aColor;
    vUV = aUV;
    vLocalPos = aPosition;
    gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);
}
