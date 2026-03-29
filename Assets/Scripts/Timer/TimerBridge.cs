using System.Linq;
using TT.Timer;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class TimerBridge : MonoBehaviour
{
    private static TimerBridge instance;
    public BeginSimulationEntityCommandBufferSystem.Singleton ecbSystem;
    private Entity Prefab;
    private World world;
    private EntityManager entityManager;

    public static TimerBridge Instance
    {
        get
        {
            if (instance == null)
            {
                var gameObject = new GameObject("TimerBridge");
                instance = gameObject.AddComponent<TimerBridge>();
                DontDestroyOnLoad(gameObject);
            }
            return instance;
        }
    }

    private void Awake()
    {
        world = World.DefaultGameObjectInjectionWorld;
        entityManager = world.EntityManager;

        EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
        entityQueryBuilder = entityQueryBuilder.WithAllRW<BeginSimulationEntityCommandBufferSystem.Singleton>();
        entityQueryBuilder = entityQueryBuilder.WithOptions(EntityQueryOptions.IncludeSystems);
        ecbSystem = entityQueryBuilder.Build(entityManager).GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        entityQueryBuilder.Dispose();
        //创建模板，直接分配内存并复制，减少动态创建内存移动和内部ArcheType变更
        Prefab = entityManager.CreateEntity();
        //批量添加，减少内部开销
        var typeSet = new ComponentTypeSet(typeof(Timer), typeof(CallbackTag), typeof(DestroyTag), typeof(Prefab));
        entityManager.AddComponent(Prefab, typeSet);
        entityManager.SetComponentEnabled<CallbackTag>(Prefab, false);
        entityManager.SetComponentEnabled<DestroyTag>(Prefab, false);
        DontDestroyOnLoad(gameObject);
    }

    // entity是个值，不是引用，必须立马创建，不可以通过命令行，否则无法返回正确的entity
    public Entity Add(float interval, HandleSystem.CallbackHandler onCallback, HandleSystem.CallbackHandler onDestroy, int repeatCount = 1, bool ignoreScale = false, bool ignoreGap = false)
    {

        var entity = entityManager.Instantiate(Prefab);
        byte flag = 0;
        float scale = 1;
        if (ignoreScale)
            flag += (byte)Flag.IgnoreScale;
        else
            scale = Time.timeScale;
        if (ignoreGap)
            flag += (byte)Flag.IgnoreGap;
        entityManager.SetComponentData(entity, new Timer
        {
            BeginStamp = Time.realtimeSinceStartup,
            Interval = interval,
            RepeatCount = repeatCount,
            Scale = scale,
            Flag = flag,
        });
        if (onCallback != null)
            HandleSystem.CallbackHandlers[entity.Index] = onCallback;
        if (onDestroy != null)
            HandleSystem.DestroyHandlers[entity.Index] = onDestroy;
        return entity;
    }

    //mono update在BeginSimulationEntityCommandBufferSystem之前，必须通过命令行队列更新，防止被被上一帧系统Update数据覆盖
    public void Remove(Entity entity)
    {
        if (!IsValid(entity)) return;
        var timer = entityManager.GetComponentData<Timer>(entity);
        if ((timer.Flag & (byte)Flag.Expired) != 0)
            return;
        var ecb = ecbSystem.CreateCommandBuffer(world.Unmanaged);
        timer.Flag |= (byte)Flag.Expired;
        ecb.SetComponent(entity, timer);
    }

    public void RemoveAll()
    {
        EntityQuery query = entityManager.CreateEntityQuery(typeof(Timer));
        NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        var count = entities.Count();
        if (count > 0)
        {
            var ecb = ecbSystem.CreateCommandBuffer(world.Unmanaged);
            for (int i = 0; i < count; i++)
            {
                var timer = entityManager.GetComponentData<Timer>(entities[i]);
                if ((timer.Flag & (byte)Flag.Expired) != 0)
                    continue;
                timer.Flag |= (byte)Flag.Expired;
                ecb.SetComponent(entities[i], timer);
            }
        }
        query.Dispose();
        entities.Dispose();
    }

    //mono update在BeginSimulationEntityCommandBufferSystem之前，必须通过命令行队列更新，防止被被上一帧系统Update数据覆盖
    public void Pause(Entity entity)
    {
        if (!IsValid(entity)) return;
        var timer = entityManager.GetComponentData<Timer>(entity);
        if ((timer.Flag & (byte)Flag.Paused) != 0)
            return;
        timer.Flag |= (byte)Flag.Paused;
        var ecb = ecbSystem.CreateCommandBuffer(world.Unmanaged);
        ecb.SetComponent(entity, timer);
        ecb.SetComponentEnabled<CallbackTag>(entity, false);
    }

    public void PauseAll()
    {
        EntityQuery query = entityManager.CreateEntityQuery(typeof(Timer));
        NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        var count = entities.Count();
        if (count > 0)
        {
            var ecb = ecbSystem.CreateCommandBuffer(world.Unmanaged);
            for (int i = 0; i < count; i++)
            {
                var timer = entityManager.GetComponentData<Timer>(entities[i]);
                if ((timer.Flag & (byte)Flag.Paused) != 0)
                    continue;
                timer.Flag |= (byte)Flag.Paused;
                ecb.SetComponent(entities[i], timer);
                ecb.SetComponentEnabled<CallbackTag>(entities[i], false);
            }
        }
        query.Dispose();
        entities.Dispose();
    }

    //mono update在BeginSimulationEntityCommandBufferSystem之前，必须通过命令行队列更新，防止被被上一帧系统Update数据覆盖
    public void Resume(Entity entity)
    {
        if (!IsValid(entity)) return;
        var timer = entityManager.GetComponentData<Timer>(entity);
        if ((timer.Flag & (byte)Flag.Paused) == 0)
            return;
        var ecb = ecbSystem.CreateCommandBuffer(world.Unmanaged);
        timer.Flag -= (byte)Flag.Paused;
        ecb.SetComponent(entity, timer);
    }

    public void ResumeAll(bool ignoreRestituion = false)
    {
        EntityQuery query = entityManager.CreateEntityQuery(typeof(Timer));
        NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        var count = entities.Count();
        if (count > 0)
        {
            var ecb = ecbSystem.CreateCommandBuffer(world.Unmanaged);
            for (int i = 0; i < count; i++)
            {
                var timer = entityManager.GetComponentData<Timer>(entities[i]);
                if ((timer.Flag & (byte)Flag.Paused) == 0)
                    continue;
                timer.Flag -= (byte)Flag.Paused;
                ecb.SetComponent(entities[i], timer);
            }
        }
        query.Dispose();
        entities.Dispose();
    }

    //直接禁用会停止计时，重新启用不会计算暂停补偿
    public void SetSystemEnabled(bool enabled)
    {
        var sys = world.GetExistingSystem<TimerSystem>();
        ref var state = ref world.Unmanaged.ResolveSystemStateRef(sys);
        state.Enabled = enabled;
        sys = world.GetExistingSystem<HandleSystem>();
        state = ref world.Unmanaged.ResolveSystemStateRef(sys);
        state.Enabled = enabled;
    }

    public bool IsSystemEnabled()
    {
        var sys = world.GetExistingSystem<TimerSystem>();
        var state = world.Unmanaged.ResolveSystemStateRef(sys);
        if (!state.Enabled)
            return false;
        sys = world.GetExistingSystem<HandleSystem>();
        state = world.Unmanaged.ResolveSystemStateRef(sys);
        return state.Enabled;
    }

    public bool IsPaused(Entity entity)
    {
        if (!IsValid(entity))
            return true;
        var timer = entityManager.GetComponentData<Timer>(entity);
        return ((timer.Flag & (byte)Flag.Paused) != 0);
    }

    public bool IsValid(Entity entity)
    {
        if (entity == Entity.Null)
            return false;
        return entityManager.Exists(entity);
    }
}
