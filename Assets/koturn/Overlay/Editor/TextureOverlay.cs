using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;


namespace Koturn.Overlay
{
    /// <summary>
    /// <see cref="ShaderGUI"/> for TextureOverlay.shader.
    /// </summary>
    public sealed class TextureOverlayGUI : ShaderGUI
    {
        /// <summary>
        /// Blend Mode
        /// </summary>
        public enum RenderingMode
        {
            Opaque,
            Cutout,
            Fade,
            Transparent,
            Additive,
            Multiply,
            Custom
        }

        /// <summary>
        /// To define a custom shader GUI of TextureOverlay.shader use the methods of <paramref name="me"/> to render controls for <paramref name="mps"/>.
        /// </summary>
        /// <param name="me">The <see cref="MaterialEditor"/> that are calling this <see cref="OnGUI(MaterialEditor, MaterialProperty[])"/> (the 'owner').</param>
        /// <param name="mps">Material properties of the current selected shader.</param>
        public override void OnGUI(MaterialEditor me, MaterialProperty[] mps)
        {
            EditorGUILayout.LabelField("Main Texture & Color", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                TexturePropertySingleLine(me, mps, "_MainTex", "_Color");
            }

            EditorGUILayout.Space();

            GUILayout.Label("Overlay parameter", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                {
                    var mpClipByDistance = FindProperty("_ClipByDistance", mps);
                    ShaderProperty(me, mpClipByDistance);
                    if (mpClipByDistance.floatValue >= 0.5)
                    {
                        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                        {
                            ShaderProperty(me, mps, "_ClipDistance");
                            ShaderProperty(me, mps, "_DistanceType");
                        }
                    }
                }

                using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                {
                    var mpEnableROI = FindProperty("_EnableROI", mps);
                    ShaderProperty(me, mpEnableROI);
                    if (mpEnableROI.floatValue >= 0.5)
                    {
                        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                        {
                            ShaderProperty(me, mps, "_OverlaySize");
                            ShaderProperty(me, mps, "_OverlayOffset");
                        }
                    }
                    var mpCorrectTexSize = FindProperty("_CorrectTexSize", mps);
                    ShaderProperty(me, mpCorrectTexSize);
                }

                EditorGUILayout.Space();

                GUILayout.Label("Overlay switches", EditorStyles.boldLabel);
                using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                {
                    ShaderProperty(me, mps, "_EnableSwitchType");
                    ShaderProperty(me, mps, "_EnableVR");
                    ShaderProperty(me, mps, "_EnableDesktop");
                    ShaderProperty(me, mps, "_EnableSS");
                    ShaderProperty(me, mps, "_EnableOther");
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Rendering Options", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                DrawBlendMode(me, mps);
                ShaderProperty(me, mps, "_ZTest");
                ShaderProperty(me, mps, "_Cull", false);
                ShaderProperty(me, mps, "_AlphaToMask", false);

                EditorGUILayout.Space();

                GUILayout.Label("Advanced Options", EditorStyles.boldLabel);
                using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                {
                    me.RenderQueueField();
#if UNITY_5_6_OR_NEWER
                    // me.EnableInstancingField();
                    me.DoubleSidedGIField();
#endif  // UNITY_5_6_OR_NEWER
                }
            }
        }

        /// <summary>
        /// Draw inspector items of <see cref="RenderingMode"/>.
        /// </summary>
        /// <param name="me">A <see cref="MaterialEditor"/></param>
        /// <param name="mps"><see cref="MaterialProperty"/> array</param>
        private void DrawBlendMode(MaterialEditor me, MaterialProperty[] mps)
        {
            using (var ccScope = new EditorGUI.ChangeCheckScope())
            {
                var mpBlendMode = FindProperty("_RenderingMode", mps);
                var mode = (RenderingMode)EditorGUILayout.EnumPopup(mpBlendMode.displayName, (RenderingMode)mpBlendMode.floatValue);
                mpBlendMode.floatValue = (float)mode;

                if (ccScope.changed && mode != RenderingMode.Custom)
                {
                    foreach (var obj in mpBlendMode.targets)
                    {
                        ApplyBlendMode(obj as Material, mode);
                    }
                }

                using (new EditorGUI.DisabledScope(mode != RenderingMode.Custom))
                {
                    ShaderProperty(me, mps, "_ZWrite");
                    ShaderProperty(me, mps, "_SrcFactor");
                    ShaderProperty(me, mps, "_DstFactor");
                }
                using (new EditorGUI.DisabledScope(mode != RenderingMode.Cutout && mode != RenderingMode.Custom))
                {
                    var mpAlphaTest = FindProperty("_AlphaTest", mps);
                    ShaderProperty(me, mpAlphaTest);
                    using (new EditorGUI.IndentLevelScope())
                    using (new EditorGUI.DisabledScope(mpAlphaTest.floatValue < 0.5))
                    {
                        ShaderProperty(me, mps, "_Cutoff");
                    }
                }
            }
        }


        /// <summary>
        /// Change blend of <paramref name="material"/>.
        /// </summary>
        /// <param name="material">Target material</param>
        /// <param name="blendMode">Blend mode</param>
        private static void ApplyBlendMode(Material material, RenderingMode blendMode)
        {
            switch (blendMode)
            {
                case RenderingMode.Opaque:
                    material.SetOverrideTag("RenderType", "");
                    material.SetInt("_SrcFactor", (int)BlendMode.One);
                    material.SetInt("_DstFactor", (int)BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.SetInt("_AlphaTest", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.renderQueue = -1;
                    break;
                case RenderingMode.Cutout:
                    material.SetOverrideTag("RenderType", "TransparentCutout");
                    material.SetInt("_SrcFactor", (int)BlendMode.One);
                    material.SetInt("_DstFactor", (int)BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.SetInt("_AlphaTest", 1);
                    material.EnableKeyword("_ALPHATEST_ON");
                    material.renderQueue = (int)RenderQueue.AlphaTest;
                    break;
                case RenderingMode.Fade:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcFactor", (int)BlendMode.SrcAlpha);
                    material.SetInt("_DstFactor", (int)BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.SetInt("_AlphaTest", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.renderQueue = (int)RenderQueue.Transparent;
                    break;
                case RenderingMode.Transparent:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcFactor", (int)BlendMode.One);
                    material.SetInt("_DstFactor", (int)BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.SetInt("_AlphaTest", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.renderQueue = (int)RenderQueue.Transparent;
                    break;
                case RenderingMode.Additive:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcFactor", (int)BlendMode.SrcAlpha);
                    material.SetInt("_DstFactor", (int)BlendMode.One);
                    material.SetInt("_ZWrite", 0);
                    material.SetInt("_AlphaTest", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.renderQueue = (int)RenderQueue.Transparent;
                    break;
                case RenderingMode.Multiply:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcFactor", (int)BlendMode.DstColor);
                    material.SetInt("_DstFactor", (int)BlendMode.Zero);
                    material.SetInt("_ZWrite", 0);
                    material.SetInt("_AlphaTest", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.renderQueue = (int)RenderQueue.Transparent;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(blendMode), blendMode, null);
            }
        }

        /// <summary>
        /// Draw default item of specified shader property.
        /// </summary>
        /// <param name="me">A <see cref="MaterialEditor"/></param>
        /// <param name="mps"><see cref="MaterialProperty"/> array</param>
        /// <param name="propName">Name of shader property</param>
        /// <param name="isMandatory">If <c>true</c> then this method will throw an exception
        /// if a property with <<paramref name="propName"/> was not found.</param>
        private static void ShaderProperty(MaterialEditor me, MaterialProperty[] mps, string propName, bool isMandatory = true)
        {
            var prop = FindProperty(propName, mps, isMandatory);
            if (prop != null) {
                ShaderProperty(me, prop);
            }
        }

        /// <summary>
        /// Draw default item of specified shader property.
        /// </summary>
        /// <param name="me">A <see cref="MaterialEditor"/></param>
        /// <param name="mp">Target <see cref="MaterialProperty"/></param>
        private static void ShaderProperty(MaterialEditor me, MaterialProperty mp)
        {
            me.ShaderProperty(mp, mp.displayName);
        }

        /// <summary>
        /// Draw default texture and color pair.
        /// </summary>
        /// <param name="me">A <see cref="MaterialEditor"/></param>
        /// <param name="mps"><see cref="MaterialProperty"/> array</param>
        /// <param name="propNameTex">Name of shader property of texture</param>
        /// <param name="propNameColor">Name of shader property of color</param>
        private static void TexturePropertySingleLine(MaterialEditor me, MaterialProperty[] mps, string propNameTex, string propNameColor)
        {
            TexturePropertySingleLine(
                me,
                FindProperty(propNameTex, mps),
                FindProperty(propNameColor, mps));
        }

        /// <summary>
        /// Draw default texture and color pair.
        /// </summary>
        /// <param name="me">A <see cref="MaterialEditor"/></param>
        /// <param name="mpTex">Target <see cref="MaterialProperty"/> of texture</param>
        /// <param name="mpColor">Target <see cref="MaterialProperty"/> of color</param>
        private static void TexturePropertySingleLine(MaterialEditor me, MaterialProperty mpTex, MaterialProperty mpColor)
        {
            me.TexturePropertySingleLine(
                new GUIContent(mpTex.displayName, mpColor.displayName),
                mpTex,
                mpColor);
        }

        /// <summary>
        /// Draw default texture and HDR-color pair.
        /// </summary>
        /// <param name="me">A <see cref="MaterialEditor"/></param>
        /// <param name="label">Text label</param>
        /// <param name="toolTipText">Tooltip text</param>
        /// <param name="mpTex">Target <see cref="MaterialProperty"/> of texture</param>
        /// <param name="mpColor">Target <see cref="MaterialProperty"/> of texture</param>
        private static void TextureWithHdrColor(MaterialEditor me, string label, string toolTipText, MaterialProperty mpTex, MaterialProperty mpColor)
        {
            me.TexturePropertyWithHDRColor(
                new GUIContent(label, toolTipText),
                mpTex,
                mpColor,
#if !UNITY_2018_1_OR_NEWER
                new ColorPickerHDRConfig(
                    minBrightness: 0,
                    maxBrightness: 10,
                    minExposureValue: -10,
                    maxExposureValue: 10),
#endif  // !UNITY_2018_1_OR_NEWER
                showAlpha: false);
        }


        private static void ColorProperty(MaterialProperty[] mps, string propName)
        {
            ColorProperty(FindProperty(propName, mps));
        }

        private static void ColorProperty(MaterialProperty mp)
        {
            using (var ccScope = new EditorGUI.ChangeCheckScope())
            {
                var color = EditorGUILayout.ColorField(mp.colorValue);
                if (ccScope.changed)
                {
                    mp.colorValue = color;
                }
            }
        }
    }
}
