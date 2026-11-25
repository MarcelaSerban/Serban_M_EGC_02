#version 330 core

// Fundalul: un dreptunghi cu colturile in (-1, -1) si (1, 1), dat DIRECT in clip space
// (fara matrice): acopera tot ecranul. z = 0.9999 il pune "in spatele" oricarui obiect.
layout (location = 0) in vec3 aPosition;

void main()
{
    gl_Position = vec4(aPosition.xy, 0.9999, 1.0);
}
