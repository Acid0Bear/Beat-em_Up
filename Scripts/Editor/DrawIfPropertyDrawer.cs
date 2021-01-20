using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions.Must;

/// <summary>
/// Based on: https://forum.unity.com/threads/draw-a-field-only-if-a-condition-is-met.448855/
/// </summary>
[CustomPropertyDrawer(typeof(DrawIfAttribute))]
public class DrawIfPropertyDrawer : PropertyDrawer
{
    #region Fields

    // Reference to the attribute on the property.
    DrawIfAttribute drawIf;

    // Field that is being compared.
    SerializedProperty comparedField;

    #endregion

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!ShowMe(property) && drawIf.disablingType == DrawIfAttribute.DisablingType.DontDraw)
            return 0f;

        // The height of the property should be defaulted to the default height.
        return base.GetPropertyHeight(property, label);
    }

    /// <summary>
    /// Errors default to showing the property.
    /// </summary>
    private bool ShowMe(SerializedProperty property)
    {
        drawIf = attribute as DrawIfAttribute;
        // Replace propertyname to the value from the parameter
        string path = property.propertyPath.Contains(".") ? property.propertyPath.Substring(0, property.propertyPath.IndexOf(']') + 2) + drawIf.comparedPropertyName : drawIf.comparedPropertyName;
        comparedField = property.serializedObject.FindProperty(path);

        //Debug.Log(path);
        if (comparedField == null)
        {
            Debug.LogError("Cannot find property with name: " + path);
                return true;
        }

        // get the value & compare based on types
        switch (comparedField.type)
        { // Possible extend cases to support your own type
            case "bool":
                return comparedField.boolValue.Equals(drawIf.comparedValue);
            case "Enum":
                return comparedField.enumValueIndex.Equals((int)drawIf.comparedValue);
            case "PPtr<$Joint2D>":
                return (comparedField.objectReferenceValue != null)?comparedField.objectReferenceValue.GetType().Equals(drawIf.comparedValue):false;
            default:
                Debug.LogError("Error: " + comparedField.type + " is not supported of " + path);
                return true;
        }
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // If the condition is met, simply draw the field.
        if ((ShowMe(property) && drawIf.disablingType != DrawIfAttribute.DisablingType.Draw) || comparedField == null)
        {
            EditorGUI.PropertyField(position, property);
        } //...check if the disabling type is read only. If it is, draw it disabled
        else if (drawIf.disablingType == DrawIfAttribute.DisablingType.ReadOnly)
        {
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property);
            GUI.enabled = true;
        }
        else if (!ShowMe(property) && drawIf.disablingType == DrawIfAttribute.DisablingType.Draw)
        {
            EditorGUI.PropertyField(position, property);
        }
    }

}