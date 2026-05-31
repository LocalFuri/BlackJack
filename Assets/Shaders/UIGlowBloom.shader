// Card inner glow: masked to the card sprite alpha so nothing spills onto the table.
// Assign to the Glow Image on CardView; set Image.sprite to the same sprite as the card face.
Shader "Blackjack/UIGlowBloom"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        [HDR] _Color ("Glow Color", Color) = (1, 0.85, 0.15, 1)

        _FaceGlow ("Face Glow Strength", Range(0.0, 1.0)) = 0.35
        _EdgeGlow ("Edge Glow Strength", Range(0.0, 1.0)) = 1.0
        _InnerEdge ("Edge Ramp Start", Range(0.0, 1.0)) = 0.55
        _OuterEdge ("Edge Ramp End", Range(0.0, 1.0)) = 0.92

        _StencilComp      ("Stencil Comparison", Float) = 8
        _Stencil          ("Stencil ID",         Float) = 0
        _StencilOp        ("Stencil Operation",  Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask  ("Stencil Read Mask",  Float) = 255
        _ColorMask        ("Color Mask",         Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "IgnoreProjector"   = "True"
            "RenderType"        = "Transparent"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref       [_Stencil]
            Comp      [_StencilComp]
            Pass      [_StencilOp]
            ReadMask  [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull     Off
        Lighting Off
        ZWrite   Off
        ZTest    [unity_GUIZTestMode]
        Blend    SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                float4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4    _MainTex_ST;
            float4    _Color;
            float4    _TextureSampleAdd;
            float4    _ClipRect;
            float     _FaceGlow;
            float     _EdgeGlow;
            float     _InnerEdge;
            float     _OuterEdge;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex        = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord      = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color         = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd;
                float mask = tex.a;
                clip(mask - 0.001);

                float2 centered = abs(IN.texcoord - 0.5) * 2.0;
                float  edgeDist = max(centered.x, centered.y);
                float  edgeT    = smoothstep(_InnerEdge, _OuterEdge, edgeDist);
                float  strength = lerp(_FaceGlow, _EdgeGlow, edgeT);

                fixed4 color;
                color.rgb = IN.color.rgb * strength;
                color.a   = mask * IN.color.a * strength;
                color.a  *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                clip(color.a - 0.001);
                return color;
            }
            ENDCG
        }
    }
}
