#if USE_UNI_LUA
using LuaAPI = UniLua.Lua;
using RealStatePtr = UniLua.ILuaState;
using LuaCSFunction = UniLua.CSharpFunctionDelegate;
#else
using LuaAPI = XLua.LuaDLL.Lua;
using RealStatePtr = System.IntPtr;
using LuaCSFunction = XLua.LuaDLL.lua_CSFunction;
#endif

using XLua;
using System.Collections.Generic;


namespace XLua.CSObjectWrap
{
    using Utils = XLua.Utils;
    public class AesDecryptorStreamWrap 
    {
        public static void __Register(RealStatePtr L)
        {
			ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			System.Type type = typeof(AesDecryptorStream);
			Utils.BeginObjectRegister(type, L, translator, 0, 1, 0, 0);
			
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "Read", _m_Read);
			
			
			
			
			
			Utils.EndObjectRegister(type, L, translator, null, null,
			    null, null, null);

		    Utils.BeginClassRegister(type, L, __CreateInstance, 1, 0, 0);
			
			
            
			
			
			
			Utils.EndClassRegister(type, L, translator);
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int __CreateInstance(RealStatePtr L)
        {
            
			try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
				if(LuaAPI.lua_gettop(L) == 8 && (LuaAPI.lua_isnil(L, 2) || LuaAPI.lua_type(L, 2) == LuaTypes.LUA_TSTRING) && translator.Assignable<System.IO.FileMode>(L, 3) && translator.Assignable<System.IO.FileAccess>(L, 4) && translator.Assignable<System.IO.FileShare>(L, 5) && (LuaAPI.lua_isnil(L, 6) || LuaAPI.lua_type(L, 6) == LuaTypes.LUA_TSTRING) && (LuaAPI.lua_isnil(L, 7) || LuaAPI.lua_type(L, 7) == LuaTypes.LUA_TSTRING) && LuaTypes.LUA_TNUMBER == LuaAPI.lua_type(L, 8))
				{
					string _path = LuaAPI.lua_tostring(L, 2);
					System.IO.FileMode _mode;translator.Get(L, 3, out _mode);
					System.IO.FileAccess _access;translator.Get(L, 4, out _access);
					System.IO.FileShare _share;translator.Get(L, 5, out _share);
					byte[] _key = LuaAPI.lua_tobytes(L, 6);
					byte[] _iv = LuaAPI.lua_tobytes(L, 7);
					int _headerSize = LuaAPI.xlua_tointeger(L, 8);
					
					var gen_ret = new AesDecryptorStream(_path, _mode, _access, _share, _key, _iv, _headerSize);
					translator.Push(L, gen_ret);
                    
					return 1;
				}
				if(LuaAPI.lua_gettop(L) == 7 && (LuaAPI.lua_isnil(L, 2) || LuaAPI.lua_type(L, 2) == LuaTypes.LUA_TSTRING) && translator.Assignable<System.IO.FileMode>(L, 3) && translator.Assignable<System.IO.FileAccess>(L, 4) && translator.Assignable<System.IO.FileShare>(L, 5) && (LuaAPI.lua_isnil(L, 6) || LuaAPI.lua_type(L, 6) == LuaTypes.LUA_TSTRING) && (LuaAPI.lua_isnil(L, 7) || LuaAPI.lua_type(L, 7) == LuaTypes.LUA_TSTRING))
				{
					string _path = LuaAPI.lua_tostring(L, 2);
					System.IO.FileMode _mode;translator.Get(L, 3, out _mode);
					System.IO.FileAccess _access;translator.Get(L, 4, out _access);
					System.IO.FileShare _share;translator.Get(L, 5, out _share);
					byte[] _key = LuaAPI.lua_tobytes(L, 6);
					byte[] _iv = LuaAPI.lua_tobytes(L, 7);
					
					var gen_ret = new AesDecryptorStream(_path, _mode, _access, _share, _key, _iv);
					translator.Push(L, gen_ret);
                    
					return 1;
				}
				
			}
			catch(System.Exception gen_e) {
				return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
			}
            return LuaAPI.luaL_error(L, "invalid arguments to AesDecryptorStream constructor!");
            
        }
        
		
        
		
        
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_Read(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                AesDecryptorStream gen_to_be_invoked = (AesDecryptorStream)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    byte[] _array = LuaAPI.lua_tobytes(L, 2);
                    int _offset = LuaAPI.xlua_tointeger(L, 3);
                    int _count = LuaAPI.xlua_tointeger(L, 4);
                    
                        var gen_ret = gen_to_be_invoked.Read( _array, _offset, _count );
                        LuaAPI.xlua_pushinteger(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        
        
        
        
        
		
		
		
		
    }
}
