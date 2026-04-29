using XLua;

namespace TT.Lua
{
    public class Env: LuaEnv
    {
        private int refCount = 0;

        public void AddRef()
        {
            refCount+=1;
        }

        public void Clean(bool force = false)
        {
            refCount -= 1;
            var clean = force || (refCount == 0);
            if (clean) 
            {
                base.Dispose();
            }
        }
    }
}
