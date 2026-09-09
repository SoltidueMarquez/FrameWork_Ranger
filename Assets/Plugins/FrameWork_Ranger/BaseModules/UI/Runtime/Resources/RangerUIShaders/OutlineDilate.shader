Shader "Hidden/RangerUI/OutlineDilate"
{
    Properties { _MainTex ("Texture", 2D) = "white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex, _Original;
        float4 _MainTex_TexelSize, _Direction, _OutlineColor;
        float _Radius;
        float dilate(float2 uv)
        {
            float a = 0;
            for (int n = -16; n <= 16; n++)
                if (abs(n) <= ceil(_Radius)) a = max(a, tex2D(_MainTex, uv + _MainTex_TexelSize.xy * _Direction.xy * n).r);
            return a;
        }
        fixed4 horizontal(v2f_img i) : SV_Target { float a = dilate(i.uv); return fixed4(a,a,a,a); }
        fixed4 vertical(v2f_img i) : SV_Target
        { float a = saturate(dilate(i.uv) - tex2D(_Original, i.uv).r); return fixed4(_OutlineColor.rgb, a * _OutlineColor.a); }
        ENDCG
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment horizontal
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment vertical
            ENDCG
        }
    }
}
