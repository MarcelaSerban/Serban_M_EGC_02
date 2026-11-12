#version 330 core

// Atributele vertexului (din VBO, descrise in VAO)
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aColor;

// Cele trei matrice (laboratorul 4, sectiunile 5-7)
uniform mat4 uModel;        // object space -> world space (diferita pentru fiecare obiect)
uniform mat4 uView;         // world space  -> view space  (camera)
uniform mat4 uProjection;   // view space   -> clip space  (ortografica sau in perspectiva)

// Iesire catre fragment shader (valoarea va fi interpolata la rasterizare)
out vec3 vColor;

void main()
{
    vColor = aColor;

    // In GLSL matricele se aplica de la dreapta la stanga: intai model, apoi view, apoi projection.
    gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);
}
