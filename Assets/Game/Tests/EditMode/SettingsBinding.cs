using Game.Core;
using NUnit.Framework;
using UnityEditor;

namespace Game.Tests.EditMode
{
    /// <summary>Binds a component's serialized `settings` reference, as the scene does for every component.</summary>
    internal static class SettingsBinding
    {
        public static void Bind(UnityEngine.Object component, GameSettings settings)
        {
            var so = new SerializedObject(component);
            SerializedProperty property = so.FindProperty("settings");
            Assert.IsNotNull(property, component.GetType().Name + " has no serialized 'settings' field");
            property.objectReferenceValue = settings;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetBool(GameSettings settings, string path, bool value)
        {
            var so = new SerializedObject(settings);
            SerializedProperty property = so.FindProperty(path);
            Assert.IsNotNull(property, "missing settings field " + path);
            property.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetInt(GameSettings settings, string path, int value)
        {
            var so = new SerializedObject(settings);
            SerializedProperty property = so.FindProperty(path);
            Assert.IsNotNull(property, "missing settings field " + path);
            property.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
