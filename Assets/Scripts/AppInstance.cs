using System;
using System.IO;
using TT.Download;
using TT.Lua;
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
        private static bool disposed = false;
        public Env Env;

        [LuaCallCSharp]
        public static AppInstance Instance
        {
            get
            {
                if (!disposed && instance == null)
                {
                    var gameObject = new GameObject("AppInstance");
                    gameObject.AddComponent<AppInstance>();
                    gameObject.AddComponent<DownloadManager>();
                    DontDestroyOnLoad(gameObject);
                }
                return instance;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            Initialized();
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
            Env = new Env();
            Env.AddLoader(LoadLua);
            Env.DoString($"require 'Main'", "Main");
            Env.AddRef();
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
            disposed = true;
            Env?.Clean();
            Env = null;
        }

        private void OnDestroy()
        {
            Dispose();
            instance = null;
        }

        private void OnApplicationQuit()
        {
            //Dispose();
        }
    }
}
