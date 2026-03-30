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
    public class TimerTestBehaviourWrap 
    {
        public static void __Register(RealStatePtr L)
        {
			ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			System.Type type = typeof(TimerTestBehaviour);
			Utils.BeginObjectRegister(type, L, translator, 0, 4, 5, 5);
			
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "OnButtonClick", _m_OnButtonClick);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "OnButtonAdd1000", _m_OnButtonAdd1000);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "OnButtonRemoveAll", _m_OnButtonRemoveAll);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "OnScaleChanged", _m_OnScaleChanged);
			
			
			Utils.RegisterFunc(L, Utils.GETTER_IDX, "btn", _g_get_btn);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "text", _g_get_text);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "currentText", _g_get_currentText);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "scaleText", _g_get_scaleText);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "slider", _g_get_slider);
            
			Utils.RegisterFunc(L, Utils.SETTER_IDX, "btn", _s_set_btn);
            Utils.RegisterFunc(L, Utils.SETTER_IDX, "text", _s_set_text);
            Utils.RegisterFunc(L, Utils.SETTER_IDX, "currentText", _s_set_currentText);
            Utils.RegisterFunc(L, Utils.SETTER_IDX, "scaleText", _s_set_scaleText);
            Utils.RegisterFunc(L, Utils.SETTER_IDX, "slider", _s_set_slider);
            
			
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
				if(LuaAPI.lua_gettop(L) == 1)
				{
					
					var gen_ret = new TimerTestBehaviour();
					translator.Push(L, gen_ret);
                    
					return 1;
				}
				
			}
			catch(System.Exception gen_e) {
				return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
			}
            return LuaAPI.luaL_error(L, "invalid arguments to TimerTestBehaviour constructor!");
            
        }
        
		
        
		
        
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_OnButtonClick(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                TimerTestBehaviour gen_to_be_invoked = (TimerTestBehaviour)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                    gen_to_be_invoked.OnButtonClick(  );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_OnButtonAdd1000(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                TimerTestBehaviour gen_to_be_invoked = (TimerTestBehaviour)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                    gen_to_be_invoked.OnButtonAdd1000(  );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_OnButtonRemoveAll(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                TimerTestBehaviour gen_to_be_invoked = (TimerTestBehaviour)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                    gen_to_be_invoked.OnButtonRemoveAll(  );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_OnScaleChanged(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                TimerTestBehaviour gen_to_be_invoked = (TimerTestBehaviour)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                    gen_to_be_invoked.OnScaleChanged(  );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_btn(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                TimerTestBehaviour gen_to_be_invoked = (TimerTestBehaviour)translator.FastGetCSObj(L, 1);
                translator.Push(L, gen_to_be_invoked.btn);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_text(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                TimerTestBehaviour gen_to_be_invoked = (TimerTestBehaviour)translator.FastGetCSObj(L, 1);
                translator.Push(L, gen_to_be_invoked.text);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_currentText(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                TimerTestBehaviour gen_to_be_invoked = (TimerTestBehaviour)translator.FastGetCSObj(L, 1);
                translator.Push(L, gen_to_be_invoked.currentText);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_scaleText(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                TimerTestBehaviour gen_to_be_invoked = (TimerTestBehaviour)translator.FastGetCSObj(L, 1);
                translator.Push(L, gen_to_be_invoked.scaleText);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_slider(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                TimerTestBehaviour gen_to_be_invoked = (TimerTestBehaviour)translator.FastGetCSObj(L, 1);
                translator.Push(L, gen_to_be_invoked.slider);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_btn(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                TimerTestBehaviour gen_to_be_invoked = (TimerTestBehaviour)translator.FastGetCSObj(L, 1);
                gen_to_be_invoked.btn = (UnityEngine.UI.Button)translator.GetObject(L, 2, typeof(UnityEngine.UI.Button));
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_text(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                TimerTestBehaviour gen_to_be_invoked = (TimerTestBehaviour)translator.FastGetCSObj(L, 1);
                gen_to_be_invoked.text = (TMPro.TextMeshProUGUI)translator.GetObject(L, 2, typeof(TMPro.TextMeshProUGUI));
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_currentText(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                TimerTestBehaviour gen_to_be_invoked = (TimerTestBehaviour)translator.FastGetCSObj(L, 1);
                gen_to_be_invoked.currentText = (TMPro.TextMeshProUGUI)translator.GetObject(L, 2, typeof(TMPro.TextMeshProUGUI));
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_scaleText(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                TimerTestBehaviour gen_to_be_invoked = (TimerTestBehaviour)translator.FastGetCSObj(L, 1);
                gen_to_be_invoked.scaleText = (TMPro.TextMeshProUGUI)translator.GetObject(L, 2, typeof(TMPro.TextMeshProUGUI));
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_slider(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                TimerTestBehaviour gen_to_be_invoked = (TimerTestBehaviour)translator.FastGetCSObj(L, 1);
                gen_to_be_invoked.slider = (UnityEngine.UI.Slider)translator.GetObject(L, 2, typeof(UnityEngine.UI.Slider));
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
		
		
		
		
    }
}
