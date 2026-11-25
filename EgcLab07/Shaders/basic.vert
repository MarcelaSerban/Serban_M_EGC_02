#version 330 core

// Shaderul de baza (laboratoarele 4-6): culoarea varfului x nuanta obiectului.
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aColor;

uniform mat4 uModel;        // object space -> world space
uniform mat4 uView;         // world space  -> view space
uniform mat4 uProjection;   // view space   -> clip space

out vec3 vColor;

void main()
{
    vColor = aColor;
    gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);
}
