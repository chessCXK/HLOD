// From: https://github.com/Unity-Technologies/EditorVR

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityObject = UnityEngine.Object;

namespace Unity.HLOD
{
    public static class ObjectUtils
    {
        static List<GameObject> s_RootGameObjects = new List<GameObject>();

        static IEnumerable<Type> GetAssignableTypes(Type type, Func<Type, bool> predicate = null)
        {
            var list = new List<Type>();
            ForEachType(t =>
            {
                if (type.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract && (predicate == null || predicate(t))
                    && t.GetCustomAttribute<HideInInspector>() == null)
                    list.Add(t);
            });

            return list;
        }

        public static void ForEachAssembly(Action<Assembly> callback)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    callback(assembly);
                }
                catch (ReflectionTypeLoadException)
                {
                    // Skip any assemblies that don't load properly
                    continue;
                }
            }
        }

        public static void ForEachType(Action<Type> callback)
        {
            ForEachAssembly(assembly =>
            {
                var types = assembly.GetTypes();
                foreach (var t in types)
                    callback(t);
            });
        }
#if UNITY_EDITOR
        static IEnumerable<Type> GetImplementationsOfInterface(Type type)
        {
            if (type.IsInterface)
                return GetAssignableTypes(type);

            return Enumerable.Empty<Type>();
        }

        static List<Type> batchers
        {
            get
            {
                return GetImplementationsOfInterface(typeof(IBatcher)).ToList();
            }
        }
        public static Type batcherType
        {
            get
            {
                var type = Type.GetType(EditorPrefs.GetString("HLOD.DefaultBatcher", null));
                if (type == null && batchers.Count > 0)
                    type = Type.GetType(batchers[0].AssemblyQualifiedName);
                return type;
            }
        }
#endif
        public static IEnumerator FindObjectsOfType<T>(List<T> objects) where T : Component
        {
            var scene = SceneManager.GetActiveScene();
            s_RootGameObjects.Clear();
            scene.GetRootGameObjects(s_RootGameObjects);
            yield return null;

            foreach (var go in s_RootGameObjects)
            {
                var children = go.GetComponentsInChildren<T>();
                objects.AddRange(children);

                yield return null;
            }
        }
    }

}
