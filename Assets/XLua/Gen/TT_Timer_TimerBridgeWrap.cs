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
    public class TTTimerTimerBridgeWrap 
    {
        public static void __Register(RealStatePtr L)
        {
			ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			System.Type type = typeof(TT.Timer.TimerBridge);
			Utils.BeginObjectRegister(type, L, translator, 0, 11, 1, 1);
			
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "Add", _m_Add);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "Remove", _m_Remove);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "RemoveAll", _m_RemoveAll);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "Pause", _m_Pause);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "PauseAll", _m_PauseAll);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "Resume", _m_Resume);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "ResumeAll", _m_ResumeAll);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "SetSystemEnabled", _m_SetSystemEnabled);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "IsSystemEnabled", _m_IsSystemEnabled);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "IsPaused", _m_IsPaused);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "IsValid", _m_IsValid);
			
			
			Utils.RegisterFunc(L, Utils.GETTER_IDX, "ecbSystem", _g_get_ecbSystem);
            
			Utils.RegisterFunc(L, Utils.SETTER_IDX, "ecbSystem", _s_set_ecbSystem);
            
			
			Utils.EndObjectRegister(type, L, translator, null, null,
			    null, null, null);

		    Utils.BeginClassRegister(type, L, __CreateInstance, 1, 1, 0);
			
			
            
			Utils.RegisterFunc(L, Utils.CLS_GETTER_IDX, "Instance", _g_get_Instance);
            
			
			
			Utils.EndClassRegister(type, L, translator);
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int __CreateInstance(RealStatePtr L)
        {
            
			try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
				if(LuaAPI.lua_gettop(L) == 1)
				{
					
					var gen_ret = new TT.Timer.TimerBridge();
					translator.Push(L, gen_ret);
                    
					return 1;
				}
				
			}
			catch(System.Exception gen_e) {
				return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
			}
            return LuaAPI.luaL_error(L, "invalid arguments to TT.Timer.TimerBridge constructor!");
            
        }
        
		
        
		
        
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_Add(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                TT.Timer.TimerBridge gen_to_be_invoked = (TT.Timer.TimerBridge)translator.FastGetCSObj(L, 1);
            
            
			    int gen_param_count = LuaAPI.lua_gettop(L);
            
                if(gen_param_count == 7&& LuaTypes.LUA_TNUMBER == LuaAPI.lua_type(L, 2)&& translator.Assignable<TT.Timer.HandleSystem.CallbackHandler>(L, 3)&& translator.Assignable<TT.Timer.HandleSystem.CallbackHandler>(L, 4)&& LuaTypes.LUA_TNUMBER == LuaAPI.lua_type(L, 5)&& LuaTypes.LUA_TBOOLEAN == LuaAPI.lua_type(L, 6)&& LuaTypes.LUA_TBOOLEAN == LuaAPI.lua_type(L, 7)) 
                {
                    float _interval = (float)LuaAPI.lua_tonumber(L, 2);
                    TT.Timer.HandleSystem.CallbackHandler _onCallback = translator.GetDelegate<TT.Timer.HandleSystem.CallbackHandler>(L, 3);
                    TT.Timer.HandleSystem.CallbackHandler _onDestroy = translator.GetDelegate<TT.Timer.HandleSystem.CallbackHandler>(L, 4);
                    int _repeatCount = LuaAPI.xlua_tointeger(L, 5);
                    bool _ignoreScale = LuaAPI.lua_toboolean(L, 6);
                    bool _ignoreGap = LuaAPI.lua_toboolean(L, 7);
                    
                        var gen_ret = gen_to_be_invoked.Add( _interval, _onCallback, _onDestroy, _repeatCount, _ignoreScale, _ignoreGap );
                        translator.Push(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                if(gen_param_count == 6&& LuaTypes.LUA_TNUMBER == LuaAPI.lua_type(L, 2)&& translator.Assignable<TT.Timer.HandleSystem.CallbackHandler>(L, 3)&& translator.Assignable<TT.Timer.HandleSystem.CallbackHandler>(L, 4)&& LuaTypes.LUA_TNUMBER == LuaAPI.lua_type(L, 5)&& LuaTypes.LUA_TBOOLEAN == LuaAPI.lua_type(L, 6)) 
                {
                    float _interval = (float)LuaAPI.lua_tonumber(L, 2);
                    TT.Timer.HandleSystem.CallbackHandler _onCallback = translator.GetDelegate<TT.Timer.HandleSystem.CallbackHandler>(L, 3);
                    TT.Timer.HandleSystem.CallbackHandler _onDestroy = translator.GetDelegate<TT.Timer.HandleSystem.CallbackHandler>(L, 4);
                    int _repeatCount = LuaAPI.xlua_tointeger(L, 5);
                    bool _ignoreScale = LuaAPI.lua_toboolean(L, 6);
                    
                        var gen_ret = gen_to_be_invoked.Add( _interval, _onCallback, _onDestroy, _repeatCount, _ignoreScale );
                        translator.Push(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                if(gen_param_count == 5&& LuaTypes.LUA_TNUMBER == LuaAPI.lua_type(L, 2)&& translator.Assignable<TT.Timer.HandleSystem.CallbackHandler>(L, 3)&& translator.Assignable<TT.Timer.HandleSystem.CallbackHandler>(L, 4)&& LuaTypes.LUA_TNUMBER == LuaAPI.lua_type(L, 5)) 
                {
                    float _interval = (float)LuaAPI.lua_tonumber(L, 2);
                    TT.Timer.HandleSystem.CallbackHandler _onCallback = translator.GetDelegate<TT.Timer.HandleSystem.CallbackHandler>(L, 3);
                    TT.Timer.HandleSystem.CallbackHandler _onDestroy = translator.GetDelegate<TT.Timer.HandleSystem.CallbackHandler>(L, 4);
                    int _repeatCount = LuaAPI.xlua_tointeger(L, 5);
                    
                        var gen_ret = gen_to_be_invoked.Add( _interval, _onCallback, _onDestroy, _repeatCount );
                        translator.Push(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                if(gen_param_count == 4&& LuaTypes.LUA_TNUMBER == LuaAPI.lua_type(L, 2)&& translator.Assignable<TT.Timer.HandleSystem.CallbackHandler>(L, 3)&& translator.Assignable<TT.Timer.HandleSystem.CallbackHandler>(L, 4)) 
                {
                    float _interval = (float)LuaAPI.lua_tonumber(L, 2);
                    TT.Timer.HandleSystem.CallbackHandler _onCallback = translator.GetDelegate<TT.Timer.HandleSystem.CallbackHandler>(L, 3);
                    TT.Timer.HandleSystem.CallbackHandler _onDestroy = translator.GetDelegate<TT.Timer.HandleSystem.CallbackHandler>(L, 4);
                    
                        var gen_ret = gen_to_be_invoked.Add( _interval, _onCallback, _onDestroy );
                        translator.Push(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
            return LuaAPI.luaL_error(L, "invalid arguments to TT.Timer.TimerBridge.Add!");
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_Remove(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                TT.Timer.TimerBridge gen_to_be_invoked = (TT.Timer.TimerBridge)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    Unity.Entities.Entity _entity;translator.Get(L, 2, out _entity);
                    
                    gen_to_be_invoked.Remove( _entity );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_RemoveAll(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                TT.Timer.TimerBridge gen_to_be_invoked = (TT.Timer.TimerBridge)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                    gen_to_be_invoked.RemoveAll(  );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_Pause(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                TT.Timer.TimerBridge gen_to_be_invoked = (TT.Timer.TimerBridge)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    Unity.Entities.Entity _entity;translator.Get(L, 2, out _entity);
                    
                    gen_to_be_invoked.Pause( _entity );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_PauseAll(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                TT.Timer.TimerBridge gen_to_be_invoked = (TT.Timer.TimerBridge)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                    gen_to_be_invoked.PauseAll(  );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_Resume(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                TT.Timer.TimerBridge gen_to_be_invoked = (TT.Timer.TimerBridge)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    Unity.Entities.Entity _entity;translator.Get(L, 2, out _entity);
                    
                    gen_to_be_invoked.Resume( _entity );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_ResumeAll(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                TT.Timer.TimerBridge gen_to_be_invoked = (TT.Timer.TimerBridge)translator.FastGetCSObj(L, 1);
            
            
			    int gen_param_count = LuaAPI.lua_gettop(L);
            
                if(gen_param_count == 2&& LuaTypes.LUA_TBOOLEAN == LuaAPI.lua_type(L, 2)) 
                {
                    bool _ignoreRestituion = LuaAPI.lua_toboolean(L, 2);
                    
                    gen_to_be_invoked.ResumeAll( _ignoreRestituion );
                    
                    
                    
                    return 0;
                }
                if(gen_param_count == 1) 
                {
                    
                    gen_to_be_invoked.ResumeAll(  );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
            return LuaAPI.luaL_error(L, "invalid arguments to TT.Timer.TimerBridge.ResumeAll!");
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_SetSystemEnabled(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                TT.Timer.TimerBridge gen_to_be_invoked = (TT.Timer.TimerBridge)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    bool _enabled = LuaAPI.lua_toboolean(L, 2);
                    
                    gen_to_be_invoked.SetSystemEnabled( _enabled );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_IsSystemEnabled(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                TT.Timer.TimerBridge gen_to_be_invoked = (TT.Timer.TimerBridge)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                        var gen_ret = gen_to_be_invoked.IsSystemEnabled(  );
                        LuaAPI.lua_pushboolean(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_IsPaused(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                TT.Timer.TimerBridge gen_to_be_invoked = (TT.Timer.TimerBridge)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    Unity.Entities.Entity _entity;translator.Get(L, 2, out _entity);
                    
                        var gen_ret = gen_to_be_invoked.IsPaused( _entity );
                        LuaAPI.lua_pushboolean(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_IsValid(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                TT.Timer.TimerBridge gen_to_be_invoked = (TT.Timer.TimerBridge)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    Unity.Entities.Entity _entity;translator.Get(L, 2, out _entity);
                    
                        var gen_ret = gen_to_be_invoked.IsValid( _entity );
                        LuaAPI.lua_pushboolean(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_Instance(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			    translator.Push(L, TT.Timer.TimerBridge.Instance);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_ecbSystem(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                TT.Timer.TimerBridge gen_to_be_invoked = (TT.Timer.TimerBridge)translator.FastGetCSObj(L, 1);
                translator.Push(L, gen_to_be_invoked.ecbSystem);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_ecbSystem(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                TT.Timer.TimerBridge gen_to_be_invoked = (TT.Timer.TimerBridge)translator.FastGetCSObj(L, 1);
                Unity.Entities.BeginSimulationEntityCommandBufferSystem.Singleton gen_value;translator.Get(L, 2, out gen_value);
				gen_to_be_invoked.ecbSystem = gen_value;
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
		
		
		
		
    }
}
