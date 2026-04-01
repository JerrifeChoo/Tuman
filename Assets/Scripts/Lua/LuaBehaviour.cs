using System;
using System.Collections.Generic;
using UnityEngine;
using XLua;

namespace TT.Lua
{
    public class LuaBehaviour : MonoBehaviour
    {
        [SerializeField, LuaPath()]
        private string luaFile;
        [SerializeField]
        private Dictionary<string, UnityEngine.Object> injections;

        private LuaTable luaInstance;
        private LuaFunction luaAwake;
        private LuaFunction luaStart;
        private LuaFunction luaUpdate;
        private LuaFunction luaLateUpdate;
        private LuaFunction luaOnDestroy;
        private Action onLuaUpdate;
        private Action onLuaLateUpdate;

        private LuaEnv luaEnv;

        private void Awake()
        {
            LoadLuaInstance();
            luaAwake?.Call(luaInstance);
        }

        [CSharpCallLua]
        private void LoadLuaInstance()
        {
            if (string.IsNullOrEmpty(luaFile))
                return;
            luaEnv = AppInstance.Instance.LuaEnv;
            luaEnv.DoString($"require '{luaFile}'", luaFile);
            luaEnv.Global.Get("package", out LuaTable luapackage);
            luapackage.Get("loaded", out LuaTable luaLoaded);
            luapackage.Dispose();
            luaLoaded.Get(luaFile, out luaInstance);
            luaLoaded.Dispose();
            using (LuaTable meta = luaEnv.NewTable())
            {
                meta.Set("__index", luaEnv.Global);
                luaInstance.SetMetaTable(meta);
            }
            luaInstance.Get("New", out LuaFunction luaConstructor);
            if (luaConstructor != null)
            {
                luaInstance.Dispose();
                luaInstance = null;
                luaInstance = luaConstructor.Call()[0] as LuaTable;
                luaConstructor.Dispose();
                luaConstructor = null;
            }

            // 将所需值注入到 Lua 脚本域中
            luaInstance.Set("gameObject", gameObject);
            luaInstance.Set("transform", transform);

            if (injections != null)
            {
                foreach (var injection in injections)
                {
                    luaInstance.Set(injection.Key, injection.Value);
                }
            }

            luaInstance.Get("Awake", out luaAwake);
            luaInstance.Get("Start", out luaStart);
            luaInstance.Get("Update", out luaUpdate);
            if (luaUpdate != null)
            {
                onLuaUpdate = () => { luaUpdate.Call(luaInstance); };
                AppInstance.Instance.OnUpdate += onLuaUpdate;
            }
            luaInstance.Get("LateUpdate", out luaLateUpdate);
            if (luaLateUpdate != null)
            {
                onLuaLateUpdate = () => { luaLateUpdate.Call(luaInstance); };
                AppInstance.Instance.OnLateUpdate += onLuaLateUpdate;
            }
            luaInstance.Get("OnDestroy", out luaOnDestroy);
        }

        private void Start()
        {
            luaStart?.Call(luaInstance);
        }

        private void Dispose()
        {
            if (luaInstance == null)
                return;
            luaAwake?.Dispose();
            luaAwake = null;
            luaStart?.Dispose();
            luaStart = null;
            if (onLuaUpdate != null)
            {
                AppInstance.Instance.OnUpdate -= onLuaUpdate;
            }
            if (onLuaLateUpdate != null)
            {
                AppInstance.Instance.OnLateUpdate -= onLuaLateUpdate;
            }
            luaUpdate?.Dispose();
            luaUpdate = null;
            luaLateUpdate?.Dispose();
            luaLateUpdate = null;
            luaOnDestroy?.Dispose();
            luaOnDestroy = null;
            luaInstance?.Dispose();
            luaInstance = null;
        }

        private void OnDestroy()
        {
            if (luaInstance == null || luaEnv == null || luaEnv.rawL == IntPtr.Zero) return;
            luaOnDestroy?.Call(luaInstance);
            Dispose();
        }
    }
}
