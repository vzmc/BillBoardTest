Shader "Custom/UnlitTemplate"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" {}
        [NoScaleOffset]_Mask("Mask", 2D) = "white" {}
        _Color("Color", Color) = (1, 1, 1, 1)
        _Dis("Dis", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Geometry" "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            //Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            //#pragma require geometry
            //#pragma geometry geom
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float3 normal       : NORMAL;
                uint vertexID       : SV_VertexID;
            };

            struct Varyings
            {
                float2 uv           : TEXCOORD0;
                float4 positionHCS  : SV_POSITION;
            };

            TEXTURE2D(_MainTex);
            TEXTURE2D(_Mask);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _Dis;
            CBUFFER_END

            //Attributes vert(Attributes input)
            //{
            //    return input;
            //}

            float _QuadRadius = 1;
            float _AngleStep = 1;

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);

                input.positionOS.xyz += input.normal * _Dis;

                int quadNum = input.vertexID / 6;
                float angle = (quadNum + 0.5) * _AngleStep;
                float3 quadCenterOS = float3(_QuadRadius * cos(angle), 0, _QuadRadius * sin(angle)) + input.normal * _Dis;
                float3 quadCenterWS = TransformObjectToWorld(quadCenterOS);
                float3 quadCenterVS = TransformWorldToView(quadCenterWS);

                float3 quadForward = -input.normal;
                float3 quadUp = float3(0, 1, 0);
                float3 quadRight = cross(quadUp, quadForward);
                float4x4 quadCoordsMatrix = float4x4(
                    quadRight.x, quadUp.x, quadForward.x, quadCenterOS.x,
                    quadRight.y, quadUp.y, quadForward.y, quadCenterOS.y,
                    quadRight.z, quadUp.z, quadForward.z, quadCenterOS.z,
                    0, 0, 0, 1
                );

                float4x4 vertexToQuadMatrix = transpose(quadCoordsMatrix);
                float3 vertexOffset = mul(vertexToQuadMatrix, input.positionOS).xyz;
                vertexOffset.z = 0;

                float3 positionVS = quadCenterVS + vertexOffset;
                output.positionHCS = TransformWViewToHClip(positionVS);
                
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);

                return output;
            }

            [maxvertexcount(3)]
            void geom(triangle Attributes input[3], uint pid : SV_PrimitiveID, inout TriangleStream<Varyings> output)
            {
                // Compute the average normal
                //float3 avgNormal_1 = float3(0, 0, 0);
                //float3 avgNormal_2 = float3(0, 0, 0);

                //int index_1[3] = {0, 2, 4};
                //int index_2[3] = {2, 3, 4};

                //for (int i = 0; i < 3; i++)
                //{
                //    int num = index_1[i];
                //    avgNormal_1 += SafeNormalize(input[num].normal);
                //}

                //for (int i = 0; i < 3; i++)
                //{
                //    int num = index_2[i];
                //    avgNormal_2 += SafeNormalize(input[num].normal);
                //}

                // Displace the triangle along the average normal
                //for (int i = 0; i < 3; i++)
                //{
                //    int num = index_1[i];
                //    Varyings v;
                //    float3 newPos = input[num].positionOS.xyz + avgNormal_1 * _Dis;
                //    v.positionHCS = TransformObjectToHClip(newPos);
                //    v.uv = input[num].uv;
                //    output.Append(v);
                //}

                //for (int i = 0; i < 3; i++)
                //{
                //    int num = index_2[i];
                //    Varyings v;
                //    float3 newPos = input[num].positionOS.xyz + avgNormal_2 * _Dis;
                //    v.positionHCS = TransformObjectToHClip(newPos);
                //    v.uv = input[num].uv;
                //    output.Append(v);
                //}

                //if (pid % 2 != 0) 
                //{ 
                //    return;
                //}

                float3 avgNormal = float3(0, 0, 0);
                for (int i = 0; i < 3; i++)
                {
                    avgNormal += SafeNormalize(input[i].normal);
                }

                for (int j = 0; j < 3; j++)
                {
                    Varyings v;
                    float3 newPos = input[j].positionOS.xyz + avgNormal * _Dis;
                    v.positionHCS = TransformObjectToHClip(newPos);
                    v.uv = input[j].uv;
                    output.Append(v);
                }

                //output.RestartStrip();
            }

            half4 frag(Varyings input, half facing : VFACE) : SV_Target
            {
                half mask = SAMPLE_TEXTURE2D(_Mask, sampler_LinearRepeat, input.uv).r;
                clip(step(0.01, mask) - 0.01);

                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_LinearRepeat, input.uv);
                half4 finalColor = texColor * _Color;

                // Multiply color by 0.5 for backface
                //half backfaceFactor = step(0, facing) * 0.5 + 0.5;
                //finalColor.rgb *= backfaceFactor;

                return finalColor;
            }
            ENDHLSL
        }
    }
}
