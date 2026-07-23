Shader "Marble Sort/Conveyor Animation Atlas"
{
    Properties
    {
        [PerRendererData] _MainTex ("Animation Atlas", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [PerRendererData] _FrameIndex ("Frame Index", Float) = 0
        _AtlasGeometry ("Frame XY, Cell ZW", Vector) = (797,207,805,215)
        _AtlasLayout ("Columns, Rows, Padding, Frames", Vector) = (8,24,4,192)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment ConveyorAtlasFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA

            #include "UnitySprites.cginc"

            float4 _MainTex_TexelSize;
            float _FrameIndex;
            float4 _AtlasGeometry;
            float4 _AtlasLayout;

            fixed4 ConveyorAtlasFrag(v2f input) : SV_Target
            {
                float2 atlasSize = _MainTex_TexelSize.zw;
                float2 frameSize = _AtlasGeometry.xy;
                float2 cellSize = _AtlasGeometry.zw;
                float columns = _AtlasLayout.x;
                float padding = _AtlasLayout.z;
                float frameCount = _AtlasLayout.w;

                // The geometry sprite's UVs describe frame zero in the
                // top-left atlas cell. Recover its frame-local UV first.
                float2 firstFrameOrigin = float2(
                    padding,
                    atlasSize.y - cellSize.y + padding);
                float2 frameLocalUv =
                    ((input.texcoord * atlasSize) - firstFrameOrigin) / frameSize;

                // Sampling through pixel centers keeps bilinear taps inside
                // the duplicated-edge gutter at every animation cell border.
                float2 localPixel = lerp(
                    float2(0.5, 0.5),
                    frameSize - float2(0.5, 0.5),
                    saturate(frameLocalUv));

                float frameIndex = clamp(floor(_FrameIndex + 0.5), 0, frameCount - 1);
                float column = fmod(frameIndex, columns);
                float row = floor(frameIndex / columns);
                float2 frameOrigin = float2(
                    (column * cellSize.x) + padding,
                    atlasSize.y - ((row + 1) * cellSize.y) + padding);
                float2 atlasUv = (frameOrigin + localPixel) / atlasSize;

                fixed4 color = SampleSpriteTexture(atlasUv) * input.color;
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
