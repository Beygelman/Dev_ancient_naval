shader_type canvas_item;
render_mode unshaded;

float waves(vec2 p, float t) {
    return sin(p.x * 31.0 + p.y * 8.0 + t * 0.48) * 0.52
        + sin(p.x * 61.0 - p.y * 19.0 - t * 0.63) * 0.22
        + sin(p.x * 107.0 + p.y * 37.0 + t * 0.86) * 0.12
        + sin(p.x * 181.0 - p.y * 91.0 - t * 1.13) * 0.06;
}
void fragment() {
    vec2 p = UV * vec2(1.8, 2.6);
    float t = TIME;
    float h = waves(p, t);
    vec3 normal = normalize(vec3((h - waves(p + vec2(0.004, 0.0), t)) * 1.6,
        (h - waves(p + vec2(0.0, 0.004), t)) * 1.6, 1.0));
    vec3 light = normalize(vec3(-0.38, -0.18, 0.9));
    float sparkle = pow(max(dot(normal, light), 0.0), 95.0);
    vec3 deep = vec3(0.105, 0.245, 0.295);
    vec3 shallow = vec3(0.18, 0.36, 0.39);
    vec3 sea = mix(deep, shallow, 0.24 + h * 0.12 + (1.0 - UV.y) * 0.22);
    sea += vec3(0.45, 0.64, 0.62) * sparkle * 0.24;
    COLOR = vec4(sea, 1.0);
}
