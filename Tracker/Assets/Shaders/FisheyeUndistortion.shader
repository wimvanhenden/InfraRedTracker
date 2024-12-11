Shader "Custom/FisheyeUndistortion"
{
    Properties
    {
        _DistortedTex ("Distorted Texture", 2D) = "white" {}
        _DebugMode ("Debug Mode", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _DistortedTex;
            float4 _CameraMatrix;
            float4 _DistCoeffs;
            float2 _ImageSize;
            float _DebugMode;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float2 undistortPoint(float2 distortedPoint)
            {
                float2 pp = float2(_CameraMatrix.z, _CameraMatrix.w);
                float2 f = float2(_CameraMatrix.x, _CameraMatrix.y);
                float2 dp = (distortedPoint - pp) / f;
                float r = length(dp);
                
                float theta = atan(r);
                float theta2 = theta * theta;
                float theta4 = theta2 * theta2;
                float theta6 = theta4 * theta2;
                float theta8 = theta4 * theta4;
                
                float thetad = theta * (1 + _DistCoeffs.x * theta2 + _DistCoeffs.y * theta4 + _DistCoeffs.z * theta6 + _DistCoeffs.w * theta8);
                
                float scale = (r > 0) ? thetad / r : 1.0;
                float2 undistortedPoint = pp + f * dp * scale;
                
                return undistortedPoint;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 distortedPoint = i.uv * _ImageSize;
                float2 undistortedPoint = undistortPoint(distortedPoint);
                float2 undistortedUV = undistortedPoint / _ImageSize;
                
                if (_DebugMode > 0.5)
                {
                    // Debug visualization
                    if (_DebugMode == 1)
                    {
                        // Visualize UV coordinates
                        return fixed4(i.uv.x, i.uv.y, 0, 1);
                    }
                    else if (_DebugMode == 2)
                    {
                        // Visualize undistorted UV coordinates
                        return fixed4(undistortedUV.x, undistortedUV.y, 0, 1);
                    }
                    else if (_DebugMode == 3)
                    {
                        // Visualize difference between distorted and undistorted
                        float2 diff = abs(i.uv - undistortedUV);
                        return fixed4(diff.x, diff.y, 0, 1);
                    }
                }
                
                // Check if the undistorted point is within the image bounds
                if (any(undistortedUV < 0) || any(undistortedUV > 1))
                {
                    return fixed4(1, 0, 0, 1); // Red color for out-of-bounds pixels
                }
                
                return tex2D(_DistortedTex, undistortedUV);
            }
            ENDCG
        }
    }
}