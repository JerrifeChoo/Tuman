using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace TT.Timer
{
    [BurstCompile]
    internal struct TimerProcessor
    {
        [BurstCompile]
        public static void Process(ref NativeArray<Entity> entities, ref NativeArray<Timer> timers, float timeStamp,
            in EntityCommandBuffer ecb = default(EntityCommandBuffer), int unfilteredChunkIndex = default, bool paralleled = default,
            in EntityCommandBuffer.ParallelWriter pw = default(EntityCommandBuffer.ParallelWriter))
        {
            for (int i = 0; i < timers.Length; i++)
            {
                var timer = timers[i];
                if (timer.Flag == Flag.Paused || timer.Flag == Flag.Expired)
                {
                    continue;
                }
                else
                {
                    uint times;
                    if (timer.Interval == 0)
                        times = timer.CompleteCount + 1;
                    else
                        times = (uint)math.floor((timeStamp - timer.BeginStamp - timer.Restitution) / timer.Interval);
                    if (times > 0 && times > timer.CompleteCount)
                    {
                        bool unlimit = timer.RepeatCount == -1;
                        if (unlimit || timer.RepeatCount > timer.CompleteCount)
                        {
                            timer.CompleteCount = times;
                            if (paralleled)
                            {
                                pw.SetComponent(unfilteredChunkIndex, entities[i], timer);
                                pw.SetComponentEnabled<CallbackTag>(unfilteredChunkIndex, entities[i], true);
                            }
                            else
                            {
                                ecb.SetComponent(entities[i], timer);
                                ecb.SetComponentEnabled<CallbackTag>(entities[i], true);
                            }
                        }
                        if (!unlimit && timer.CompleteCount >= timer.RepeatCount)
                        {
                            timer.Flag = Flag.Expired;
                            if (paralleled)
                            {
                                pw.SetComponent(unfilteredChunkIndex, entities[i], timer);
                                pw.SetComponentEnabled<DestroyTag>(unfilteredChunkIndex, entities[i], true);
                            }
                            else
                            {
                                ecb.SetComponent(entities[i], timer);
                                ecb.SetComponentEnabled<DestroyTag>(entities[i], true);
                            }
                        }
                    }
                }
            }

        }
    }
}
