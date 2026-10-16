using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Inspector type picker for [SerializeReference, SubclassSelector] fields
/// (pact / meta-upgrade RunEffects). Shows a dropdown with every concrete
/// [Serializable] subclass of the field's type; picking one creates it and
/// its fields appear underneath. New effect classes show up automatically.
/// </summary>
[CustomPropertyDrawer(typeof(SubclassSelectorAttribute))]
public class SubclassSelectorDrawer : PropertyDrawer
{
    private static readonly Dictionary<Type, Type[]> cache = new Dictionary<Type, Type[]>();

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
        EditorGUI.GetPropertyHeight(property, label, true);

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.ManagedReference)
        {
            EditorGUI.PropertyField(position, property, label, true);
            return;
        }

        EditorGUI.BeginProperty(position, label, property);

        Rect button = new Rect(position.x + EditorGUIUtility.labelWidth + 2f, position.y,
                               position.width - EditorGUIUtility.labelWidth - 2f, EditorGUIUtility.singleLineHeight);
        string current = CurrentTypeName(property);
        if (EditorGUI.DropdownButton(button, new GUIContent(current), FocusType.Keyboard))
            ShowMenu(property);

        EditorGUI.PropertyField(position, property, label, true);
        EditorGUI.EndProperty();
    }

    private static string CurrentTypeName(SerializedProperty property)
    {
        string full = property.managedReferenceFullTypename;
        if (string.IsNullOrEmpty(full)) return "(None) — pick a type";
        int space = full.LastIndexOf(' ');
        string typeName = space >= 0 ? full.Substring(space + 1) : full;
        int dot = typeName.LastIndexOf('.');
        return ObjectNames.NicifyVariableName(dot >= 0 ? typeName.Substring(dot + 1) : typeName);
    }

    private void ShowMenu(SerializedProperty property)
    {
        Type baseType = ElementType(fieldInfo.FieldType);
        SerializedObject so = property.serializedObject;
        string path = property.propertyPath;

        var menu = new GenericMenu();
        menu.AddItem(new GUIContent("(None)"), string.IsNullOrEmpty(property.managedReferenceFullTypename),
                     () => Assign(so, path, null));
        menu.AddSeparator("");

        foreach (Type t in ConcreteTypes(baseType))
        {
            Type captured = t;
            menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(t.Name)), false, () => Assign(so, path, captured));
        }
        menu.ShowAsContext();
    }

    private static void Assign(SerializedObject so, string path, Type type)
    {
        so.Update();
        SerializedProperty p = so.FindProperty(path);
        if (p == null) return;
        p.managedReferenceValue = type != null ? Activator.CreateInstance(type) : null;
        so.ApplyModifiedProperties();
    }

    private static Type ElementType(Type fieldType)
    {
        if (fieldType.IsArray) return fieldType.GetElementType();
        if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
            return fieldType.GetGenericArguments()[0];
        return fieldType;
    }

    private static Type[] ConcreteTypes(Type baseType)
    {
        if (cache.TryGetValue(baseType, out Type[] found)) return found;
        found = TypeCache.GetTypesDerivedFrom(baseType)
            .Where(t => !t.IsAbstract && !t.IsGenericType && t.IsSerializable
                        && !typeof(UnityEngine.Object).IsAssignableFrom(t)
                        && t.GetConstructor(Type.EmptyTypes) != null)
            .OrderBy(t => t.Name)
            .ToArray();
        cache[baseType] = found;
        return found;
    }
}
