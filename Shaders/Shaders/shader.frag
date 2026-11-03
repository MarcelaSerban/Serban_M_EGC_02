#version 330 core

// Culoarea interpolata, primita de la vertex shader (acelasi nume si tip)
in vec3 vColor;

// Uniforma: o nuanta care se inmulteste cu culoarea (alb = culoarea ramane neschimbata)
uniform vec4 uTint;

// Culoarea finala a fragmentului
out vec4 FragColor;

void main()
{
    FragColor = vec4(vColor, 1.0) * uTint;
}
