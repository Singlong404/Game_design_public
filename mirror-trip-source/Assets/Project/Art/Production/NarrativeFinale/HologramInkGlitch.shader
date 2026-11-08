Shader "MirrorTrip/Round2/HologramInkGlitch"
{
    Properties {
        [PerRendererData] _MainTex ("Sprite",2D)="white"{}
        _Color ("Tint",Color)=(1,1,1,1)
        _Strength("Signal damage",Range(0,1))=.7
        _RectA("Frame A",Vector)=(0,0,1,1)
        _RectB("Frame B",Vector)=(0,0,1,1)
        _Blend("Frame blend",Range(0,1))=0
        _Clock("Animation clock",Float)=0
    }
    SubShader {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="True"}
        Pass {
            Tags {"LightMode"="Universal2D"}
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            float4 _Color,_RectA,_RectB;float _Strength,_Blend,_Clock;
            CBUFFER_END
            struct A{float4 positionOS:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
            struct V{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
            V vert(A a){V v;v.positionCS=TransformObjectToHClip(a.positionOS.xyz);v.uv=a.uv;v.color=a.color*_Color;return v;}
            float rnd(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            half4 frame(float2 p,float4 r){if(any(p<.001)||any(p>.999))return 0;return SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,r.xy+p*r.zw);}
            half4 blended(float2 p){return lerp(frame(p,_RectA),frame(p,_RectB),_Blend);}
            half4 frag(V i):SV_Target{
                float2 p=(i.uv-_RectA.xy)/_RectA.zw;
                float phase=floor(_Clock*8),row=floor(p.y*38);
                float burst=.25+.75*step(.65,rnd(float2(floor(_Clock*.7),7)));
                float band=step(.68,rnd(float2(row,phase)));
                float shift=(rnd(float2(row+2,phase))-.5)*.09*_Strength*burst*band;
                half4 c=blended(p+float2(shift,0));
                half4 ghost=blended(p+float2(.022*_Strength, .003*_Strength));
                float ghostA=ghost.a*.18*_Strength*(1-c.a*.6);
                c.rgb=lerp(c.rgb,ghost.rgb,ghostA);c.a=max(c.a,ghostA);
                float dropout=step(.93,rnd(floor(p*float2(22,31))+phase));
                c.a*=1-dropout*_Strength*burst;
                c.rgb*=.88+.12*sin(p.y*1100+_Clock*6);
                c.rgb+=(rnd(p*1024+phase)-.5)*.12*_Strength;
                c.rgb=dot(c.rgb,half3(.299,.587,.114)).xxx;
                return c*i.color;
            }
            ENDHLSL
        }
    }
}
