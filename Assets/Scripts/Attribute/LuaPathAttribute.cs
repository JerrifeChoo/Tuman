using UnityEngine;

namespace TT.Attr
{
    public class LuaPathAttribute : PropertyAttribute
    {
        public string CullingDir;

        public LuaPathAttribute(string cullingDir = "Assets/LuaScripts/") => CullingDir = cullingDir;
    }
}