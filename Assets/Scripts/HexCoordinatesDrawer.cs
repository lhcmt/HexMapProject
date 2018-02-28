using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(HexCoordinates))]
//在编辑器的Inspector上显示网格坐标
public class HexCoordinatesDrawer : PropertyDrawer
{
    //Property drawers render their contents via an OnGUI method. This method is provided the 
    //screen rectangle to draw inside, the serialized data of the property, and the label of 
    //the field it belongs to.
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label )
    {
        HexCoordinates coordinates = new HexCoordinates(
            property.FindPropertyRelative("x").intValue,
            property.FindPropertyRelative("z").intValue
        );
        position = EditorGUI.PrefixLabel(position, label);
        GUI.Label(position, coordinates.ToString());
    }
}