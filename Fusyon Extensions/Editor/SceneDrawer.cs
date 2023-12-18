using UnityEditor;
using UnityEngine;

namespace Fusyon.Extensions
{
    /// <summary>
    /// Draws a scene drawer in the inspector.
    /// </summary>
    [CustomPropertyDrawer(typeof(SceneAttribute))]
    public class SceneDrawer : PropertyDrawer
    {
        #region Overrides
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.String)
            {
                SceneAsset sceneObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(property.stringValue);

                if (sceneObject == null && !string.IsNullOrWhiteSpace(property.stringValue))
                {
                    Debug.LogError($"Could not find the scene '{property.stringValue}' in '{property.propertyPath}'.");
                }

                SceneAsset scene = (SceneAsset)EditorGUI.ObjectField(position, label, sceneObject, typeof(SceneAsset), true);

                property.stringValue = AssetDatabase.GetAssetPath(scene);
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "Use [Scene] with strings.");
            }
        }
        #endregion
    }
}
