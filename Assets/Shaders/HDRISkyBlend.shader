Shader "Custom/HDRISkyBlend"
{
    Properties
    {
        _Skybox1 ("HDRI Skybox 1", Cube) = "white" {}
        _Skybox2 ("HDRI Skybox 2", Cube) = "white" {}
        _Blend ("Blend", Range(0, 1)) = 0
        _Exposure ("Exposure", Float) = 1
        _Rotation ("Rotation", Range(0, 360)) = 0
        _Tint ("Tint Color", Color) = (1,1,1,1)
    }
    
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off
        
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            
            TEXTURECUBE(_Skybox1);
            SAMPLER(sampler_Skybox1);
            
            TEXTURECUBE(_Skybox2);
            SAMPLER(sampler_Skybox2);
            
            float _Blend;
            float _Exposure;
            float _Rotation;
            float4 _Tint;
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 texcoord : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 texcoord : TEXCOORD0;
            };
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.texcoord = input.texcoord;
                return output;
            }
            
            float3 RotateAroundYAxis(float3 dir, float angle)
            {
                float rad = radians(angle);
                float cosAngle = cos(rad);
                float sinAngle = sin(rad);
                
                return float3(
                    dir.x * cosAngle - dir.z * sinAngle,
                    dir.y,
                    dir.x * sinAngle + dir.z * cosAngle
                );
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                // Rotate the direction
                float3 dir = normalize(input.texcoord);
                dir = RotateAroundYAxis(dir, _Rotation);
                
                // Sample both skyboxes
                float4 sky1 = SAMPLE_TEXTURECUBE(_Skybox1, sampler_Skybox1, dir);
                float4 sky2 = SAMPLE_TEXTURECUBE(_Skybox2, sampler_Skybox2, dir);
                
                // Blend between them
                float4 color = lerp(sky1, sky2, _Blend);
                
                // Apply tint and exposure
                color.rgb *= _Tint.rgb * _Exposure;
                
                return color;
            }
            ENDHLSL
        }
    }
}