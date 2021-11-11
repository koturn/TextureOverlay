Shader "koturn/InfoDisplay/TextureOverlay_Fade"
{
    Properties
    {
        _MainTex("Main texture", 2D) = "white" {}

        _Color("Tint color for main texture", Color) = (1.0, 1.0, 1.0, 1.0)


        [Toggle]
        _ClipByDistance("Enable clipping by distance", Int) = 1

        _ClipDistance ("Clip distance from camera", Float) = 1.0

        [KeywordEnum(Manhattan, Euclidean)]
        _DistanceType("Distance type", Int) = 1

        [Toggle]
        _EnableROI("Enable ROI", Int) = 0

        [Toggle]
        _CorrectTexSize("Correct texture size", Int) = 0

        [Vector2]
        _OverlaySize ("Size of overlay text", Vector) = (1.0, 1.0, 0.0, 0.0)

        [Vector2]
        _OverlayOffset ("Position offset of overlay text", Vector) = (0.0, 0.0, 0.0, 0.0)


        [KeywordEnum(CompileTime, Runtime)]
        _EnableSwitchType("Switch type", Int) = 0

        [Toggle]
        _EnableVR("Enable Overlay in Player View (VR)", Int) = 1

        [Toggle]
        _EnableDesktop("Enable Overlay in Player View (Desktop)", Int) = 1

        [Toggle]
        _EnableSS("Enable Overlay in Screen Shot", Int) = 0

        [Toggle]
        _EnableOther("Enable Overlay for Others", Int) = 0


        [HideInInspector]
        _RenderingMode("Rendering Mode", Int) = 2

        [Enum(UnityEngine.Rendering.BlendMode)]
        _SrcFactor("Blend Source Factor", Int) = 5  // Default: SrcAlpha

        [Enum(UnityEngine.Rendering.BlendMode)]
        _DstFactor("Blend Destination Factor", Int) = 10  // Default: OneMinusSrcAlpha

        [Enum(Off, 0, On, 1)]
        _ZWrite("ZWrite", Int) = 0  // Default: Off


        [Enum(UnityEngine.Rendering.CullMode)]
        _Cull("Culling Mode", Int) = 0  // Default: Off

        [Enum(UnityEngine.Rendering.CompareFunction)]
        _ZTest("ZTest", Int) = 8  // Default: Always

        [Enum(Off, 0, On, 1)]
        _AlphaToMask("Alpha To Mask", Int) = 0  // Default: Off


        [Toggle]
        _AlphaTest("Alpha test", Int) = 0

        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+2000"
            "DisableBatching" = "True"
            "IgnoreProjector" = "True"
        }

        Cull [_Cull]
        ZTest [_ZTest]
        ZWrite [_ZWrite]
        Blend [_SrcFactor] [_DstFactor]
        AlphaToMask [_AlphaToMask]

        Pass
        {
            CGPROGRAM
            #pragma target 3.0

            #include "UnityCG.cginc"

            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local_vertex _ENABLESWITCHTYPE_COMPILETIME _ENABLESWITCHTYPE_RUNTIME
            #pragma shader_feature_local_vertex _ _ENABLEVR_ON
            #pragma shader_feature_local_vertex _ _ENABLEDESKTOP_ON
            #pragma shader_feature_local_vertex _ _ENABLESS_ON
            #pragma shader_feature_local_vertex _ _ENABLEOTHER_ON
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _ENABLEROI_ON
            #pragma shader_feature_local_fragment _ _CLIPBYDISTANCE_ON
            #pragma shader_feature_local_fragment _ _CORRECTTEXSIZE_ON
            #pragma shader_feature_local_fragment _DISTANCETYPE_MANHATTAN _DISTANCETYPE_EUCLIDEAN


#ifdef _ENABLESWITCHTYPE_RUNTIME
            uniform bool _EnableVR;
            uniform bool _EnableDesktop;
            uniform bool _EnableSS;
            uniform bool _EnableOther;
#endif  // _ENABLESWITCHTYPE_RUNTIME
            UNITY_DECLARE_TEX2D(_MainTex);
            uniform float4 _MainTex_TexelSize;
            uniform float4 _Color;
#ifdef _ALPHATEST_ON
            uniform float _Cutoff;
#endif  // _ALPHATEST_ON
#ifdef _CLIPBYDISTANCE_ON
            uniform float _ClipDistance;
#endif  // _CLIPBYDISTANCE_ON
#ifdef _ENABLEROI_ON
            uniform float2 _OverlaySize;
            uniform float2 _OverlayOffset;
#endif  // _ENABLEVR_ON

            /*!
             * @brief Input data type for vertex shader function
             */
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            /*!
             * @brief Input data type for fragment shader function
             */
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldCoord : TEXCOORD2;
            };


            inline bool doOverlay();
            inline bool isOrthographic();
            inline bool isInMirror();
            inline bool isPlayerView();
            inline bool isVRView();
            inline bool isSS();
            inline float manhattanDist(float3 x, float3 y);
            inline float sqdist(float3 x, float3 y);


            /*!
             * @brief Vertex shader function
             * @param [in] v  Input data
             * @return color of texel at (i.uv.x, i.uv.y)
             */
            v2f vert(appdata v)
            {
                static const float4 outsideVertPos = float4(-2.0, -2.0, -2.0, -2.0);

                v2f o;

                o.vertex = doOverlay() ? float4(v.uv * float2(2.0, -2.0) + float2(-1.0, 1.0), 0.0, 1.0) : outsideVertPos;
                float4 screenPos = ComputeScreenPos(o.vertex);
                screenPos.xy /= screenPos.w;
#if UNITY_SINGLE_PASS_STEREO
                const float4 scaleOffset = unity_StereoScaleOffset[unity_StereoEyeIndex];
                o.uv = (screenPos.xy - scaleOffset.zw) / scaleOffset.xy;
#else
                o.uv = screenPos.xy;
#endif  // UNITY_SINGLE_PASS_STEREO
                o.worldCoord = mul(unity_ObjectToWorld, float4(0.0, 0.0, 0.0, 1.0)).xyz;

                return o;
            }

            /*!
             * @brief Fragment shader function
             * @param [in] i  Input data from vertex shader
             * @return color of texel at (i.uv.x, i.uv.y)
             */
            fixed4 frag(v2f i) : SV_Target
            {
                static const float nFonts = 12.0;
                static const float2 div = float2(7.0, 3.0);
                static const float2 rDiv = 1.0 / div;

#ifdef _CLIPBYDISTANCE_ON
#    ifdef _DISTANCETYPE_MANHATTAN
                clip(_ClipDistance - manhattanDist(_WorldSpaceCameraPos, i.worldCoord));
#    else
                clip(_ClipDistance * _ClipDistance - sqdist(_WorldSpaceCameraPos, i.worldCoord));
#    endif  // _DISTANCETYPE_MANHATTAN
#endif  // _CLIPBYDISTANCE_ON

#ifdef _ENABLEROI_ON
#    ifdef _CORRECTTEXSIZE_ON
                const float correctRate = (_OverlaySize.y * _MainTex_TexelSize.z * _ScreenParams.y) / (_OverlaySize.x * _MainTex_TexelSize.w * _ScreenParams.x);
                const float2 size = _OverlaySize * min(1.0, float2(correctRate, 1.0 / correctRate));
                const float2 offset = _OverlayOffset + (_OverlaySize - size) * 0.5;
#    else
                const float2 size = _OverlaySize;
                const float2 offset = _OverlayOffset;
#    endif  // _CORRECTTEXSIZE_ON
                const float2 uv = (i.uv - offset) / size;
                clip(step(0.0, uv) * step(uv, 1.0) - 0.5);
#else
#    ifdef _CORRECTTEXSIZE_ON
                const float correctRate = (_MainTex_TexelSize.z * _ScreenParams.y) / (_MainTex_TexelSize.w * _ScreenParams.x);
                const float2 size = min(1.0, float2(correctRate, 1.0 / correctRate));
                const float2 offset = (float2(1.0, 1.0) - size) * 0.5;
                const float2 uv = (i.uv - offset) / size;
                clip(step(0.0, uv) * step(uv, 1.0) - 0.5);
#    else
                const float2 uv = i.uv;
#    endif  // _CORRECTTEXSIZE_ON
#endif  // _ENABLEROI_ON

                const float4 color = UNITY_SAMPLE_TEX2D(_MainTex, uv) * _Color;
#ifdef _ALPHATEST_ON
                clip(color.a - _Cutoff);
#endif  // _ALPHATEST_ON
                return color;
            }

            /*!
             * @brief Determine if overlay is necessary.
             * @return true if overlay is necessary, otherwise false.
             */
            inline bool doOverlay()
            {
#ifdef _ENABLESWITCHTYPE_RUNTIME
                if (isOrthographic() || isInMirror()) {
                    return 0;
                } else if (isVRView()) {
                    return _EnableVR;
                } else if (isPlayerView()) {
                    return _EnableDesktop;
                } else if (isSS()) {
                    return _EnableSS;
                } else {
                    return _EnableOther;
                }
#else
                if (isOrthographic() || isInMirror()) {
                    return 0;
                } else if (isVRView()) {
#   ifdef _ENABLEVR_ON
                    return 1;
#   else
                    return 0;
#   endif  // _ENABLEVR_ON
                } else if (isPlayerView()) {
#   ifdef _ENABLEDESKTOP_ON
                    return 1;
#   else
                    return 0;
#   endif  // _ENABLEDESKTOP_ON
                } else if (isSS()) {
#   ifdef _ENABLESS_ON
                    return 1;
#   else
                    return 0;
#   endif  // _ENABLESS_ON
                } else {
#   ifdef _ENABLEOTHER_ON
                    return 1;
#   else
                    return 0;
#   endif  // _ENABLEOTHER_ON
                }
#endif  // _ENABLESWITCHTYPE_RUNTIME
            }

            /*!
             * @brief Determine if the projection matrix is orthographic.
             * @return true if the projection matrix is orthographic, otherwise false.
             */
            inline bool isOrthographic()
            {
                return UNITY_MATRIX_P[3][3] == 1;
            }

            /*!
             * @brief Determine if in mirror.
             * @return true if in mirror, otherwise false.
             */
            inline bool isInMirror()
            {
                return unity_CameraProjection[2][0] != 0.0 || unity_CameraProjection[2][1] != 0.0;
            }

            /*!
             * @brief Determine if in player view.
             * @return true if in player view, otherwise false.
             */
            inline bool isPlayerView()
            {
#ifdef USING_STEREO_MATRICES
                return 1;
#else
                // FOV in Desktop mode
                static const float desktopFov = 60.0;
                // Tolerance for comparison operations of floating-point numbers in FOV calculation.
                static const float fovEps = 0.01;
                // Tolerance for comparison operations of floating-point numbers in offset calculation.
                static const float rotEps = 0.0001;

                if (isOrthographic()) {
                    return 0;
                }
                const float t = unity_CameraProjection[1][1];
                const float fov = degrees(atan(1.0 / t) * 2.0);
                if (abs(fov - desktopFov) >= fovEps) {
                    return 0;
                }
                const float4 center = UnityWorldToClipPos(_WorldSpaceCameraPos);
                const float4 projected = UnityWorldToClipPos(float3(0.0, 1.0, 0.0) + _WorldSpaceCameraPos);
                const float4 offset = center - projected;

                return abs(offset.x) < rotEps;
#endif  // USING_STEREO_MATRICES
            }

            /*!
             * @brief Determine if in VR view.
             * @return true if in VR view, otherwise false.
             */
            inline bool isVRView()
            {
#ifdef USING_STEREO_MATRICES
                return 1;
#else
                return 0;
#endif  // USING_STEREO_MATRICES
            }

            /*!
             * @brief Determine if in Screen Shot.
             * @return true if in Screen Shot, otherwise false.
             */
            inline bool isSS()
            {
                const float w = _ScreenParams.x;
                const float h = _ScreenParams.y;
                return (w == 1280.0 && h == 720.0)
                    || (w == 1920.0 && h == 1080.0)
                    || (w == 3840.0 && h == 2160.0);
            }

            /*!
             * @brief Calculate Manhattan Distance.
             * @param [in] First 3D-vector.
             * @param [in] Second 3D-vector.
             * @return Manhattan Distance.
             */
            inline float manhattanDist(float3 x, float3 y)
            {
                return dot(abs(x - y), float3(1.0, 1.0, 1.0));
            }

            /*!
             * @brief Calculate squared Euclidean Distance.
             * @param [in] First 3D-vector.
             * @param [in] Second 3D-vector.
             * @return Squared Euclidean Distance.
             */
            inline float sqdist(float3 x, float3 y)
            {
                const float3 v = x - y;
                return dot(v, v);
            }
            ENDCG
        }
    }

    CustomEditor "Koturn.Overlay.TextureOverlayGUI"
}
