Shader "Hidden/RangerUI/OutlineMask"
{
    Properties { _MainTex ("Texture", 2D) = "white" {} _Stencil ("Stencil", Float) = 0 }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex;
        float4x4 _ToGroup, _Projection;
        float4 _ClipRect;
        float _Opacity;
        struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
        struct v2f { float4 vertex : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; float2 group : TEXCOORD1; };
        v2f vert(appdata v)
        {
            v2f o; float4 p = mul(_ToGroup, v.vertex);
            o.vertex = mul(_Projection, p); o.group = p.xy; o.uv = v.uv; o.color = v.color; return o;
        }
        float alpha(v2f i)
        {
            clip(i.group - _ClipRect.xy); clip(_ClipRect.zw - i.group);
            return tex2D(_MainTex, i.uv).a * i.color.a * _Opacity;
        }
        fixed4 stencilFrag(v2f i) : SV_Target { clip(alpha(i) - 0.001); return 0; }
        fixed4 sourceFrag(v2f i) : SV_Target { float a = alpha(i); return fixed4(a,a,a,a); }
        ENDCG
        Pass
        {
            ColorMask 0
            Stencil { Ref [_Stencil] Comp Equal Pass IncrSat }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment stencilFrag
            ENDCG
        }
        Pass
        {
            Blend One One
            BlendOp Max
            Stencil { Ref [_Stencil] Comp Equal Pass Keep }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment sourceFrag
            ENDCG
        }
    }
}
