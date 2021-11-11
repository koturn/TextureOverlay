using UnityEditor;
using UnityEngine;


namespace Koturn.Overlay.Drawers
{
    /// <summary>
    /// Custom material property for 2D vector.
    /// </summary>
    public class Vector2Drawer : MaterialPropertyDrawer
    {
        /// <summary>
        /// <param name="position">Rectangle on the screen to use for the property GUI.</param>
        /// <param name="prop">The <see cref="MaterialProperty"/> to make the custom GUI for.</param>
        /// <param name="label">The label of this property.</param>
        /// <param name="editor">Current material editor.</param>
        /// </summary>
        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            using (var ccScope = new EditorGUI.ChangeCheckScope())
            {
                EditorGUI.showMixedValue = prop.hasMixedValue;
                var vec = EditorGUI.Vector2Field(position, label, prop.vectorValue);
                if (ccScope.changed)
                {
                    prop.vectorValue = new Vector4(vec.x, vec.y, prop.vectorValue.z, prop.vectorValue.w);
                }
            }
        }
    }
}
