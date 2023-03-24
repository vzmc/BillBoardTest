Shader "ZYB/BillBaordUnlit"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" {}
        [NoScaleOffset]_Mask("Mask", 2D) = "white" {}
        _Color("Color", Color) = (1, 1, 1, 1)
        _Distance("Distance", Range(0, 1)) = 0
        [Toggle]_BillBoard("BillBoard", Float) = 0                  // ビルボード描画On/Off
        [Toggle]_AffectedByScale("Affected by scale", Float) = 0    // ビルボードがTransformのScaleを受けるかどうか
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline" }
        Cull Off
        
        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma target 4.0
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float3 uv5          : TEXCOORD5;    // 頂点所属のQuad中心座標
                float3 normal       : NORMAL;
            };

            struct Varyings
            {
                float2 uv           : TEXCOORD0;
                float4 positionHCS  : SV_POSITION;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_Mask);
            SAMPLER(sampler_Mask);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _Distance;
                float _BillBoard;
                float _AffectedByScale;
            CBUFFER_END

            // 頂点座標をQuad中心座標系に変換し、Quad中心座標系での頂点座標を求める(頂点と中心の差分ベクトル)
            float3 GetOffsetFromQuadCenter(float3 vertex, float3 quadCenter, float3 quadForward, float3 quadUp, float3 quadRight)
            {
                float3x3 rotateMatrix = float3x3(
                    quadRight.x, quadUp.x, quadForward.x,
                    quadRight.y, quadUp.y, quadForward.y,
                    quadRight.z, quadUp.z, quadForward.z
                );

                float3 moveVector = mul(rotateMatrix, -quadCenter);
                float4x4 quadCoordsMatrix = float4x4(
                    rotateMatrix._m00, rotateMatrix._m01, rotateMatrix._m02, moveVector.x,
                    rotateMatrix._m10, rotateMatrix._m11, rotateMatrix._m12, moveVector.y,
                    rotateMatrix._m20, rotateMatrix._m21, rotateMatrix._m22, moveVector.z,
                    0, 0, 0, 1
                );

                float3 offset = mul(quadCoordsMatrix, float4(vertex, 1)).xyz;
                return offset;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                input.positionOS.xyz += input.normal * _Distance;

                if (_BillBoard > 0)
                {
                    float3 quadCenterOS = input.uv5  + input.normal * _Distance;
                    float3 quadCenterWS = TransformObjectToWorld(quadCenterOS);
                    float3 quadCenterVS = TransformWorldToView(quadCenterWS);

                    float3 vertexOffset;
                    
                    // モデルのスケール変換を受けたい場合
                    // 頂点座標、およびQuad中心座標,Forward,Up,Rightを全部世界座標系に変換してから頂点の差分ベクトルを求める
                    if (_AffectedByScale > 0)
                    {
                        float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                        
                        float3 quadForwardWS = TransformObjectToWorldNormal(input.normal);
                        float3 quadUpWS = TransformObjectToWorldDir(float3(0, 1, 0));
                        float3 quadRightWS = cross(quadForwardWS, quadUpWS);

                        vertexOffset = GetOffsetFromQuadCenter(positionWS, quadCenterWS, quadForwardWS, quadUpWS, quadRightWS);
                    }
                    // スケール変換受けない場合、オブジェクト空間での情報をそのまま使って頂点差分ベクトルを求める
                    else
                    {
                        float3 positionOS = input.positionOS.xyz;
                        
                        float3 quadForwardOS = input.normal;
                        float3 quadUpOS = float3(0, 1, 0);
                        float3 quadRightOS = cross(quadForwardOS, quadUpOS);

                        vertexOffset = GetOffsetFromQuadCenter(positionOS, quadCenterOS, quadForwardOS, quadUpOS, quadRightOS);
                    }

                    // View空間で、Quad中心座標に頂点差分ベクトルを足して頂点の座標を求めることで、常にカメラに向くQuadを描画できる
                    float3 positionVS = quadCenterVS + vertexOffset;
                    output.positionHCS = TransformWViewToHClip(positionVS);
                }
                else
                {
                    output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                }
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);

                return output;
            }

            half4 frag(Varyings input, half facing : VFACE) : SV_Target
            {
                half mask = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, input.uv).r;
                clip(step(0.01, mask) - 0.01);
                
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 finalColor = texColor * _Color;

                // 表と裏面を視認できるように、裏面の色を暗くする
                half backfaceFactor = facing > 0 ? 1 : 0.1;
                finalColor.rgb *= backfaceFactor;
                
                return finalColor;
            }
            ENDHLSL
        }
    }
}
