using TT.Timer;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class TimerBridge : MonoBehaviour
{
    private static TimerBridge instance;
    public BeginSimulationEntityCommandBufferSystem.Singleton ecbSystem;
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

    private Entity Prefab;

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
    public Entity Add(float interval, HandleSystem.CallbackHandler onCallback, HandleSystem.CallbackHandler onDestroy, int repeatCount = 1)
    {

        var entity = entityManager.Instantiate(Prefab);
        entityManager.SetComponentData(entity, new Timer
        {
            BeginStamp = Time.realtimeSinceStartup,
            Interval = interval,
            RepeatCount = repeatCount,
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
        var ecb = ecbSystem.CreateCommandBuffer(world.Unmanaged);
        var timer = entityManager.GetComponentData<Timer>(entity);
        if (timer.Flag == Flag.Expired)
            return;
        timer.Flag = Flag.Expired;
        ecb.SetComponent(entity, timer);
        ecb.SetComponentEnabled<DestroyTag>(entity, true);
    }

    //mono update在BeginSimulationEntityCommandBufferSystem之前，必须通过命令行队列更新，防止被被上一帧系统Update数据覆盖
    public void Pause(Entity entity)
    {
        if (!IsValid(entity)) return;
        var timer = entityManager.GetComponentData<Timer>(entity);
        if (timer.Flag != Flag.Expired)
        {
            timer.PauseStamp = Time.realtimeSinceStartup;
            timer.Flag = Flag.Paused;
        }
        var ecb = ecbSystem.CreateCommandBuffer(world.Unmanaged);
        ecb.SetComponent(entity, timer);
        ecb.SetComponentEnabled<CallbackTag>(entity, false);
    }

    //mono update在BeginSimulationEntityCommandBufferSystem之前，必须通过命令行队列更新，防止被被上一帧系统Update数据覆盖
    public void Resume(Entity entity)
    {
        if (!IsValid(entity)) return;
        var timer = entityManager.GetComponentData<Timer>(entity);
        var ecb = ecbSystem.CreateCommandBuffer(world.Unmanaged);
        if (timer.Flag == Flag.Paused)
        {
            var stamp = Time.realtimeSinceStartup;
            //补偿
            timer.Restitution += stamp - timer.PauseStamp;
            timer.Flag = Flag.None;
        }
        ecb.SetComponent(entity, timer);
    }

    public bool IsPaused(Entity entity)
    {
        if (!IsValid(entity))
            return true;
        var timer = entityManager.GetComponentData<Timer>(entity);
        return timer.Flag == Flag.Paused;
    }

    public bool IsValid(Entity entity)
    {
        if (entity == Entity.Null)
            return false;
        return entityManager.Exists(entity);
    }
}
