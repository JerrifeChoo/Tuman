using Unity.Burst;
using Unity.Entities;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;

namespace TT.Timer
{
    [BurstCompile]
    public partial struct TimerSystem : ISystem
    {
        private int CurrentWorkerCount;
        //线程阈值，超过启用JobChunk
        private int Threshold;
        //是否启用并行
        private bool Paralleled;
        private EntityQuery TimerQuery;
        //[BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<Timer>();
            TimerQuery = state.GetEntityQuery(typeof(Timer));
            Threshold = int.MaxValue;
        }

        [BurstCompile]
        private void CalculateThreshold()
        {
            int workerCount = JobsUtility.JobWorkerCount;
            if (CurrentWorkerCount != workerCount)
            {
                //Unity 默认为JobsUtility.JobWorkerMaximumCount(逻辑处理器-1)
                if (workerCount <= 0)
                {
                    Threshold = int.MaxValue;
                }
                else
                {
                    //一个chunk最多128个
                    Threshold = 256;
                }
                CurrentWorkerCount = workerCount;
            }
            //只有2个以上工作线程才会开启批次
            if (workerCount > 1)
            {
                int chunkCount = TimerQuery.CalculateChunkCount();
                //计算Chunk块数量，超出两倍工作线程数启用Parallel，否则单Job执行
                Paralleled = chunkCount > 2 * workerCount;
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float time = Time.realtimeSinceStartup;
            CalculateThreshold();
            var ecbSystem = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            int entityCount = TimerQuery.CalculateEntityCount();
            var command = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged);
            if (entityCount > Threshold)
            {
                var componentHandle = SystemAPI.GetComponentTypeHandle<Timer>();
                var entityTypeHandle = SystemAPI.GetEntityTypeHandle();
                
                if (Paralleled)
                {
                    var job = new TimerJobChunkParallel
                    {
                        TimeStamp = time,
                        Paralleled = Paralleled,
                        ComponentHandle = componentHandle,
                        EntityHandle = entityTypeHandle,
                        ECB = command.AsParallelWriter()
                    };
                    state.Dependency = job.ScheduleParallel(TimerQuery, state.Dependency);
                }
                else
                {
                    var job = new TimerJobChunk
                    {
                        TimeStamp = time,
                        Paralleled = Paralleled,
                        ComponentHandle = componentHandle,
                        EntityHandle = entityTypeHandle,
                        ECB = command
                    };
                    state.Dependency = job.Schedule(TimerQuery, state.Dependency);
                }
            }
            else
            {
                var entities = TimerQuery.ToEntityArray(state.WorldUpdateAllocator);
                var timers = TimerQuery.ToComponentDataArray<Timer>(state.WorldUpdateAllocator);
                TimerProcessor.Process(ref entities, ref timers, time, in command);
            }
            state.Dependency.Complete();
        }
    }
}
