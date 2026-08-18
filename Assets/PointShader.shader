Shader "Custom/PointShader"
{
    Properties
    {
        _PointSize ("Point Size", Float) = 0.12
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // Instancing desteğini açıyoruz
            #pragma multi_compile_instancing 
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // C# tarafından gönderilecek olan 24 byte'lık veri yapımız
            struct PointData {
                float3 position;
                float3 color;
            };

            // Verileri tutan GPU belleği
            StructuredBuffer<PointData> _PointBuffer;
            float _PointSize;

            struct Attributes {
                float4 positionOS : POSITION;//lokal konumdur.
                uint instanceID : SV_InstanceID; // Hangi küpü çizdiğimizi veren indeks
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;//asıl konum burası oluyor positionOS dan hesaplanıyor.
                float3 color : TEXCOORD0;
            };

            Varyings vert(Attributes input) {
                Varyings output;
                
                // Buffer'dan bu indekse ait pozisyon ve renk verisini çekiyoruz
                PointData data = _PointBuffer[input.instanceID];
                
                // Gönderilen Mesh'in (Küp/Quad) köşelerini _PointSize ile küçültüp, asıl konuma öteliyoruz
                float3 worldPos = (input.positionOS.xyz * _PointSize) + data.position;
                
                output.positionCS = TransformWorldToHClip(worldPos);
                output.color = data.color;
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target {//her pikselde calıstıgı ıcın piksel degısımınde kullanılır.
                // Buffer'dan gelen rengi doğrudan ekrana bas
                return half4(input.color, 1.0);
            }
            ENDHLSL
        }
    }
}