using Godot;

namespace DevAncientNaval.Presentation.Map;
internal static class TargetHighlightArt
{
    private static readonly Shader Ink = new() { Code = """
        shader_type canvas_item;
        render_mode unshaded, blend_premul_alpha;
        uniform vec4 tint : source_color = vec4(1.0,0.18,0.13,0.8);
        uniform bool lethal = false;
        uniform bool mask_only = false;
        void fragment() {
            vec4 base = texture(TEXTURE, UV);
            vec2 p = TEXTURE_PIXEL_SIZE * 3.0;
            float near = 0.0;
            for(int i=0;i<12;i++) {
                float a=float(i)*0.523599;
                near=max(near,texture(TEXTURE,UV+vec2(cos(a),sin(a))*p).a);
            }
            float edge=max(0.0,near-base.a);
            float pulse=0.48+0.22*pow(max(0.0,sin(TIME*3.2)),4.0);
            float ray=0.0;
            if(lethal) {
                vec2 direction=UV-vec2(0.5,0.58);
                float angle=atan(direction.y,direction.x);
                float spoke=pow(max(0.0,cos(angle*12.0)),16.0);
                float wide=0.0;
                for(int i=1;i<5;i++) {
                    wide=max(wide,texture(TEXTURE,UV-normalize(direction+vec2(0.0001))*p*float(i)*1.6).a);
                }
                ray=spoke*wide*(1.0-base.a)*0.24;
            }
            float alpha=(edge*pulse+ray)*tint.a;
            vec4 glow=vec4(tint.rgb*alpha,alpha);
            COLOR=mask_only?glow:glow+base*(1.0-alpha);
        }
        """ };
    internal static ShaderMaterial Material(bool lethal, bool maskOnly = false, Color? tint = null)
    {
        var material = new ShaderMaterial { Shader = Ink };
        material.SetShaderParameter("lethal", lethal);
        material.SetShaderParameter("mask_only", maskOnly);
        material.SetShaderParameter("tint", tint ?? new Color(1,.18f,.13f,.9f));
        return material;
    }
}
