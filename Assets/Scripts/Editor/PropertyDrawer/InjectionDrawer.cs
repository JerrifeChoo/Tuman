using System;
using TT.Attr;
using TT.Lua;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace TT.Editor
{
    [CustomPropertyDrawer(typeof(InjectionAttribute))]
    public class InjectionDrawer : PropertyDrawer
    {
        private readonly string gotype = typeof(GameObject).Name;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginChangeCheck();
            var injection = (Injection)property.boxedValue;
            position.width = position.width / 3 - 10;
            injection.Key = EditorGUI.TextArea(position, injection.Key);
            position.x += position.width + 10;
            var (selections, objs) = GetComponnetTypes(injection.Value);
            var selected = EditorGUI.Popup(position, 0, selections);
            position.x += position.width + 10;
            var temp = EditorGUI.ObjectField(position, injection.Value, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck())
            {
                if (temp != injection.Value)
                    injection.Value = temp;
                else
                    injection.Value = objs[selected];
                property.boxedValue = injection;
            }
        }

        public (string[], UnityEngine.Object[] objs) GetComponnetTypes(UnityEngine.Object obj)
        {
            string[] selected;
            UnityEngine.Object[] objs = null;
            if (obj.IsUnityNull())
                selected = new string[] { "None" };
            else
            {
                var components = obj.GetComponents<Component>();
                selected = new string[] { obj.GetType().Name };
                int total = 1;
                GameObject gameObject = null;
                if (obj.GetType().Name != gotype)
                {
                    total += 1;
                    gameObject = ((Component)obj).gameObject;
                }
                objs = new UnityEngine.Object[] { obj };
                total += components.Length;
                if (components.Length > 0)
                {
                    Array.Resize(ref selected, total);
                    Array.Resize(ref objs, total);
                    int index = 1;
                    if (gameObject != null)
                    {
                        selected[index] = gotype;
                        objs[index] = gameObject;
                        index++;
                    }
                    foreach (var comp in components)
                    {
                        selected[index] = comp.GetType().Name;
                        objs[index] = comp;
                        index++;
                    }
                }
            }
            return (selected, objs);
        }
    }
}