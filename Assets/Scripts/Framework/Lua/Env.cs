using XLua;

namespace TT.Lua
{
    internal class Env
    {
        private int refCount = 0;
        public LuaEnv Lua;

        public Env()
        {
            Lua = new LuaEnv();
        }

        public void AddRef()
        {
            refCount+=1;
        }

        public void Dispose(bool force = false)
        {
            refCount -= 1;
            var clean = force || (refCount == 0);
            if (clean) 
            {
                Lua.Dispose();
                Lua = null;
            }
        }
    }
}
