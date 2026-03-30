using System.Collections.Generic;
using UnityEngine;
using XLua;

[LuaCallCSharp]
public class LuaBehavior : MonoBehaviour
{
    public string LuaFilePath;
    public Dictionary<string, Object> injections;
    private LuaTable luaInstance;
    private LuaFunction luaAwake;
    private LuaFunction luaStart;
    private LuaFunction luaUpdate;
    private LuaFunction luaOnDestroy;

    // Start is called before the first frame update
    private void Awake()
    {
        if (string.IsNullOrEmpty(LuaFilePath))
        {
            return;
        }
        luaAwake?.Call();
    }

    private void Start()
    {
        luaStart?.Call();
    }

    private void OnDestroy()
    {
        luaOnDestroy?.Call();
    }
}
