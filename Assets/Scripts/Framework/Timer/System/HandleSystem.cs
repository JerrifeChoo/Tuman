using System;
using System.Collections.Generic;
using Unity.Entities;
using XLua;

namespace TT.Timer
{
    [UpdateBefore(typeof(TimerSystem))]
    public partial class HandleSystem : SystemBase
    {
        public static Dictionary<int, Action<Entity>> DestroyHandlers = new Dictionary<int, Action<Entity>>(512);
        public static Dictionary<int, Action<Entity>> CallbackHandlers = new Dictionary<int, Action<Entity>>(512);
        private EntityCommandBuffer Default = default(EntityCommandBuffer);

        protected override void OnCreate()
        {
            RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var query1 = GetEntityQuery(typeof(CallbackTag));
            var query2 = GetEntityQuery(typeof(DestroyTag));
            RequireAnyForUpdate(query1, query2);
        }

        protected override void OnUpdate()
        {
            var ecbSystem = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer command = Default;
            foreach (var (handle, destroy, entity) in SystemAPI.Query<RefRO<CallbackTag>, RefRO<DestroyTag>>().WithAny<CallbackTag, DestroyTag>().WithEntityAccess())
            {
                Action<Entity> callback;
                if (command.Equals(Default))
                    command = ecbSystem.CreateCommandBuffer(base.World.Unmanaged);

                var destroyed = SystemAPI.IsComponentEnabled<DestroyTag>(entity);
                if (destroyed)
                {
                    CallbackHandlers.Remove(entity.Index);
                    DestroyHandlers.TryGetValue(entity.Index, out callback);
                    callback?.Invoke(entity);
                    DestroyHandlers.Remove(entity.Index);
                    command.DestroyEntity(entity);
                }
                else
                {
                    var isCallback = SystemAPI.IsComponentEnabled<CallbackTag>(entity);
                    if (isCallback)
                    {
                        CallbackHandlers.TryGetValue(entity.Index, out callback);
                        callback?.Invoke(entity);
                        command.SetComponentEnabled<CallbackTag>(entity, false);
                    }
                }
            }
        }

        protected override void OnDestroy()
        {
        }
    }
}
