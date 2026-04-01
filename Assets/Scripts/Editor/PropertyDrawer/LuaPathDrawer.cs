using System.IO;
using UnityEditor;
using UnityEngine;
namespace TT.Editor
{
    [CustomPropertyDrawer(typeof(LuaPathAttribute))]
    public class LuaPathDrawer : PropertyDrawer
    {
        private TextAsset textAsset;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            string luaPath = property.stringValue;
            EditorGUI.BeginChangeCheck();
            var filePath = GetFilePath(luaPath);
            var isExist = File.Exists(filePath);
            if (string.IsNullOrEmpty(luaPath) || isExist)
            {
                if(isExist && textAsset == null)
                    textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(filePath);
                textAsset = (TextAsset)EditorGUI.ObjectField(position, label, textAsset, typeof(TextAsset), false);
                filePath = AssetDatabase.GetAssetPath(textAsset);
            }
            else
            {
                EditorGUI.PropertyField(position, property, label);
                filePath = property.stringValue;
            }
            if (EditorGUI.EndChangeCheck())
            {
                property.stringValue = GetLuaPath(filePath);
            }
            EditorGUI.EndProperty();
        }

        private string GetLuaPath(string filePath) 
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                var cullingDir = ((LuaPathAttribute)attribute).CullingDir;
                int start = 0, end = filePath.Length;
                if (filePath.StartsWith(cullingDir))
                    start = cullingDir.Length;
                if (filePath.EndsWith(".lua.txt"))
                    end = filePath.Length - start - ".lua.txt".Length;
                filePath = filePath.Substring(start, end);
                filePath = filePath.Replace("/", ".");
            }
            return filePath;
        }

        private string GetFilePath(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                var cullingDir = ((LuaPathAttribute)attribute).CullingDir;
                return cullingDir + filePath + ".lua.txt";
            }
            return null;
        }
    }
}
