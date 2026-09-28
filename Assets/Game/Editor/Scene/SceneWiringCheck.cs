using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    /// <summary>
    /// Post-build guard for any scene builder: walks every game MonoBehaviour in the scene (active
    /// or not) and throws, failing the batch build, if a serialized UnityEngine.Object field or
    /// object array is unassigned, holds a reference to something destroyed, or is an empty array.
    /// Only components from Game.* namespaces are inspected (not Unity's own Button/Image/Text, whose
    /// null sprites and targets are normal), and only [SerializeField] or public fields.
    /// Fields a scene deliberately leaves empty are passed as "TypeName.fieldName" in the allowlist.
    /// </summary>
    public static class SceneWiringCheck
    {
        private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        public static void Verify(Scene scene, params string[] allowedEmptyFields)
        {
            var allowed = new HashSet<string>(allowedEmptyFields);
            var problems = new List<string>();

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour == null)
                    {
                        // A missing script deserialises as a null component, which has no owner to name.
                        problems.Add(HierarchyPath(root.transform) + " (or a child): a component's script is missing");
                        continue;
                    }

                    CheckBehaviour(behaviour, allowed, problems);
                }
            }

            if (problems.Count > 0)
            {
                var message = new StringBuilder();
                message.Append("[SceneWiringCheck] ").Append(scene.name).Append(" has ").Append(problems.Count).AppendLine(" unwired field(s):");
                foreach (string problem in problems)
                {
                    message.Append("  ").AppendLine(problem);
                }

                throw new System.InvalidOperationException(message.ToString());
            }

            Debug.Log("[SceneWiringCheck] " + scene.name + ": all object fields wired.");
        }

        private static void CheckBehaviour(MonoBehaviour behaviour, HashSet<string> allowed, List<string> problems)
        {
            System.Type type = behaviour.GetType();
            if (type.Namespace == null || !type.Namespace.StartsWith("Game.") || type.Namespace.StartsWith("Game.Editor"))
            {
                return;
            }

            var so = new SerializedObject(behaviour);
            for (System.Type t = type; t != null && t != typeof(MonoBehaviour); t = t.BaseType)
            {
                foreach (FieldInfo field in t.GetFields(FieldFlags))
                {
                    if (!IsSerializedObjectField(field) || allowed.Contains(type.Name + "." + field.Name))
                    {
                        continue;
                    }

                    SerializedProperty prop = so.FindProperty(field.Name);
                    string where = HierarchyPath(behaviour.transform) + " " + type.Name + "." + field.Name;
                    if (prop == null)
                    {
                        problems.Add(where + ": not serialized");
                    }
                    else if (prop.isArray)
                    {
                        CheckArray(prop, where, problems);
                    }
                    else
                    {
                        CheckReference(prop, where, problems);
                    }
                }
            }
        }

        private static void CheckArray(SerializedProperty prop, string where, List<string> problems)
        {
            if (prop.arraySize == 0)
            {
                problems.Add(where + ": array is empty");
                return;
            }

            for (int i = 0; i < prop.arraySize; i++)
            {
                CheckReference(prop.GetArrayElementAtIndex(i), where + "[" + i + "]", problems);
            }
        }

        private static void CheckReference(SerializedProperty prop, string where, List<string> problems)
        {
            if (prop.objectReferenceValue != null)
            {
                return;
            }

            // A non-zero instance id with a null object means the target was destroyed or its asset is gone.
            problems.Add(where + (prop.objectReferenceInstanceIDValue != 0 ? ": reference is missing (target destroyed or asset removed)" : ": not assigned"));
        }

        /// <summary>A serialized (public, or [SerializeField]) field that is a UnityEngine.Object or an array/list of them.</summary>
        private static bool IsSerializedObjectField(FieldInfo field)
        {
            if (field.IsStatic || field.IsNotSerialized || (!field.IsPublic && field.GetCustomAttribute<SerializeField>() == null))
            {
                return false;
            }

            System.Type fieldType = field.FieldType;
            if (fieldType.IsArray)
            {
                fieldType = fieldType.GetElementType();
            }
            else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
            {
                fieldType = fieldType.GetGenericArguments()[0];
            }

            return typeof(Object).IsAssignableFrom(fieldType);
        }

        private static string HierarchyPath(Transform transform)
        {
            string path = transform.name;
            for (Transform parent = transform.parent; parent != null; parent = parent.parent)
            {
                path = parent.name + "/" + path;
            }

            return path;
        }
    }
}
