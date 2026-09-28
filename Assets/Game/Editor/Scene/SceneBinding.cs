using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// The one way scene builders write a component's serialized (usually private) fields. Goes
    /// through SerializedObject by field name, so a rename or type change at runtime is caught here:
    /// every failure throws with "Component.field" in the message, failing the batch build instead of
    /// logging an error and leaving the field silently empty.
    /// </summary>
    public static class SceneBinding
    {
        /// <summary>Assigns an object reference, enum, int, float or Vector3 to the named serialized field.</summary>
        public static void Bind(Object target, string fieldName, object value)
        {
            SerializedProperty prop = FindProperty(target, fieldName, out SerializedObject so);
            string label = Label(target, fieldName);
            if (value == null)
            {
                throw new System.InvalidOperationException("[SceneBinding] " + label + " was bound to null. Use BindIfNotNull when the value is genuinely optional.");
            }

            switch (value)
            {
                case Object unityObj:
                    RequireType(prop, SerializedPropertyType.ObjectReference, label, value);
                    prop.objectReferenceValue = unityObj;
                    RequireAssigned(prop, unityObj, label);
                    break;
                case System.Enum e:
                    RequireType(prop, SerializedPropertyType.Enum, label, value);
                    prop.enumValueIndex = System.Convert.ToInt32(e);
                    break;
                case int i:
                    RequireType(prop, SerializedPropertyType.Integer, label, value);
                    prop.intValue = i;
                    break;
                case float f:
                    RequireType(prop, SerializedPropertyType.Float, label, value);
                    prop.floatValue = f;
                    break;
                case Vector3 v3:
                    RequireType(prop, SerializedPropertyType.Vector3, label, value);
                    prop.vector3Value = v3;
                    break;
                default:
                    throw new System.InvalidOperationException("[SceneBinding] " + label + ": unsupported bind value type " + value.GetType());
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Same as Bind, but leaves the field at its serialized default when value is null. SceneWiringCheck still fails the build if the field ends up empty and is not allowlisted.</summary>
        public static void BindIfNotNull(Object target, string fieldName, Object value)
        {
            if (value == null)
            {
                return;
            }

            Bind(target, fieldName, value);
        }

        /// <summary>Assigns an array of object references, sizing the serialized array to match.</summary>
        public static void BindArray(Object target, string fieldName, Object[] values)
        {
            SerializedProperty prop = FindProperty(target, fieldName, out SerializedObject so);
            string label = Label(target, fieldName);
            if (!prop.isArray || prop.propertyType == SerializedPropertyType.String)
            {
                throw new System.InvalidOperationException("[SceneBinding] " + label + " is not an array field (it is " + prop.propertyType + ").");
            }

            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                SerializedProperty element = prop.GetArrayElementAtIndex(i);
                RequireType(element, SerializedPropertyType.ObjectReference, label + "[" + i + "]", values[i]);
                element.objectReferenceValue = values[i];
                RequireAssigned(element, values[i], label + "[" + i + "]");
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SerializedProperty FindProperty(Object target, string fieldName, out SerializedObject so)
        {
            if (target == null)
            {
                throw new System.InvalidOperationException("[SceneBinding] Cannot bind ." + fieldName + " on a null component.");
            }

            so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                throw new System.InvalidOperationException("[SceneBinding] Missing serialized field " + Label(target, fieldName) + ".");
            }

            return prop;
        }

        private static void RequireType(SerializedProperty prop, SerializedPropertyType expected, string label, object value)
        {
            if (prop.propertyType != expected)
            {
                throw new System.InvalidOperationException("[SceneBinding] " + label + " is a " + prop.propertyType + " field; cannot bind a "
                    + (value == null ? "null" : value.GetType().ToString()) + " to it.");
            }
        }

        /// <summary>SerializedProperty silently drops an object of the wrong type (the field reads back null), so read it back.</summary>
        private static void RequireAssigned(SerializedProperty prop, Object expected, string label)
        {
            if (prop.objectReferenceValue != expected)
            {
                throw new System.InvalidOperationException("[SceneBinding] " + label + " rejected " + (expected == null ? "null" : expected.GetType().ToString())
                    + " (" + (expected == null ? "null" : expected.name) + "): the field's type does not accept it.");
            }
        }

        private static string Label(Object target, string fieldName)
        {
            return target.GetType().Name + "." + fieldName;
        }
    }
}
