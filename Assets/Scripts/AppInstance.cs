using System;
using System.IO;
using System.Text;
using UnityEngine;
using XLua;

namespace TT
{
    public class AppInstance : MonoBehaviour
    {
        private static AppInstance instance;
        [CSharpCallLua]
        public Action OnUpdate;
        [CSharpCallLua]
        public Action OnLateUpdate;
        public LuaEnv LuaEnv;

        [LuaCallCSharp]
        public static AppInstance Instance
        {
            get
            {
                //if (!instance)
                //{
                //    var gameObject = new GameObject("AppInstance");
                //    instance = gameObject.AddComponent<AppInstance>();
                //    instance.Initialized();
                //}
                return instance;
            }
        }

        private void Awake()
        {
            if (!instance)
            {
                instance = this;
                instance.Initialized();
            }
        }
        public bool IsLuaFile(ref string filepath)
        {
            if (string.IsNullOrEmpty(filepath))
                return false;
            filepath = filepath.Replace('.', '/');
            var Root = Application.dataPath + "/LuaScripts";
            filepath = $"{Root}/{filepath}.lua.txt";
            if (File.Exists(filepath))
                return true;
            return false;
        }

        private byte[] LoadLua(ref string filepath)
        {
            if (!IsLuaFile(ref filepath))
            {
                Debug.LogError($"Exception: Invalid FilePath {filepath}");
                return null;
            }
            return File.ReadAllBytes(filepath);
        }

        private void Initialized()
        {
            LuaEnv = new LuaEnv();
            LuaEnv.AddLoader(LoadLua);
            LuaEnv.DoString($"require 'Main'", "Main");
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            OnUpdate?.Invoke();
        }

        private void LateUpdate()
        {
            OnLateUpdate?.Invoke();
        }

        private void Dispose()
        {
            try
            {
                LuaEnv.Dispose();
                LuaEnv = null;
            }
            catch (Exception e)
            {
            }
        }

        private void OnDestroy()
        {
            Debug.LogError("OnDestroy========");
            Dispose();
        }

        private void OnApplicationQuit()
        {
            Debug.LogError("OnApplicationQuit========");
            //Dispose();
        }
    }
}
