using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    /// <summary>G2 follow-up: runtime guard that the built Level1 scene is fully wired.</summary>
    public sealed class Level1SceneWiringTests : Level1PlayModeTestBase
    {
        // "Type.field" entries the code explicitly treats as optional (null-safe) AND the builder may
        // leave empty. Keep this list short; anything else unbound is a wiring bug.
        private static readonly HashSet<string> Optional = new HashSet<string>
        {
        };

        [UnityTest]
        public IEnumerator EveryGameComponent_HasAllObjectReferencesBound_AndNoMissingScripts()
        {
            Assembly gameAssembly = typeof(Game.Core.GameSession).Assembly;
            var problems = new List<string>();
            int checkedComponents = 0;

            foreach (Transform t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.gameObject.scene != H.Scene)
                {
                    continue;
                }

                foreach (Component c in t.GetComponents<Component>())
                {
                    if (c == null)
                    {
                        problems.Add(Path(t) + ": missing script");
                        continue;
                    }

                    if (!(c is MonoBehaviour) || c.GetType().Assembly != gameAssembly)
                    {
                        continue;
                    }

                    checkedComponents++;
                    CheckComponent(c, problems);
                }
            }

            Assert.Greater(checkedComponents, 5, "suspiciously few Game components found");
            Assert.IsEmpty(problems, "Unbound serialized references in Level1:\n  " + string.Join("\n  ", problems));
            yield break;
        }

        private static void CheckComponent(Component c, List<string> problems)
        {
            for (Type type = c.GetType(); type != null && type != typeof(MonoBehaviour); type = type.BaseType)
            {
                foreach (FieldInfo f in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (!IsSerialized(f) || Optional.Contains(type.Name + "." + f.Name))
                    {
                        continue;
                    }

                    object value = f.GetValue(c);
                    string where = Path(c.transform) + " " + type.Name + "." + f.Name;
                    if (typeof(UnityEngine.Object).IsAssignableFrom(f.FieldType))
                    {
                        if ((UnityEngine.Object)value == null)
                        {
                            problems.Add(where + " is null");
                        }
                    }
                    else if (value is System.Collections.IEnumerable list && f.FieldType != typeof(string) && ElementIsObject(f.FieldType))
                    {
                        int i = 0;
                        foreach (object element in list)
                        {
                            if ((UnityEngine.Object)element == null)
                            {
                                problems.Add(where + "[" + i + "] is null");
                            }

                            i++;
                        }
                    }
                    else if (value == null && ElementIsObject(f.FieldType))
                    {
                        problems.Add(where + " (object collection) is null");
                    }
                }
            }
        }

        private static bool IsSerialized(FieldInfo f)
        {
            if (f.IsStatic || f.IsNotSerialized || f.IsInitOnly || f.IsLiteral)
            {
                return false;
            }

            return f.IsPublic || f.GetCustomAttribute<SerializeField>() != null;
        }

        private static bool ElementIsObject(Type collectionType)
        {
            Type element = collectionType.IsArray
                ? collectionType.GetElementType()
                : collectionType.IsGenericType ? collectionType.GetGenericArguments()[0] : null;
            return element != null && typeof(UnityEngine.Object).IsAssignableFrom(element);
        }

        private static string Path(Transform t)
        {
            string p = t.name;
            for (Transform parent = t.parent; parent != null; parent = parent.parent)
            {
                p = parent.name + "/" + p;
            }

            return p;
        }
    }
}
