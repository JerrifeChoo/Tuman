using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.UIElements;

namespace TT.Timer
{
    [BurstCompile]
    internal struct TimerProcessor
    {
        [BurstCompile]
        public static void Process(ref NativeArray<Entity> entities, ref NativeArray<Timer> timers, float timeStamp, float timeScale,
            in EntityCommandBuffer ecb = default(EntityCommandBuffer), int unfilteredChunkIndex = default, bool paralleled = default,
            in EntityCommandBuffer.ParallelWriter pw = default(EntityCommandBuffer.ParallelWriter))
        {
            for (int i = 0; i < timers.Length; i++)
            {
                var timer = timers[i];

                var (current, flag) = (timer.Current, timer.Flag);
                bool expired = (current & (byte)Flag.Expired) != 0;
                bool paused = (current & (byte)Flag.Paused) != 0;
                bool ignoreScale = (current & (byte)Flag.IgnoreScale) != 0;
                bool ignoreGap = (current & (byte)Flag.IgnoreGap) != 0;

                var (newExpired, newPaused, newIgnoreScale, newIgnoreGap) = (expired, paused, ignoreScale, ignoreGap);
                bool shouldUpdate = current != flag;
                //状态变更
                if (shouldUpdate)
                {
                    newExpired = (flag & (byte)Flag.Expired) != 0;
                    newPaused = (flag & (byte)Flag.Paused) != 0;
                    newIgnoreScale = (flag & (byte)Flag.IgnoreScale) != 0;
                    newIgnoreGap = (flag & (byte)Flag.IgnoreGap) != 0;
                    timer.Current = flag;
                }

                //过期
                if (expired || newExpired)
                {
                    if (shouldUpdate)
                    {
                        if (paralleled)
                            pw.SetComponent(unfilteredChunkIndex, entities[i], timer);
                        else
                            ecb.SetComponent(entities[i], timer);
                    }
                    //只存在未过期->过期
                    if (expired != newExpired)
                    {
                        if (paralleled)
                            pw.SetComponentEnabled<DestroyTag>(unfilteredChunkIndex, entities[i], true);
                        else
                            ecb.SetComponentEnabled<DestroyTag>(entities[i], true);
                    }
                    continue;
                }

                var beginStamp = timer.BeginStamp;
                float restitution = timer.Restitution;
                float scale = 1;
                if (!ignoreScale)
                {
                    scale = timer.Scale;
                    if (scale != timeScale)
                    {
                        timer.Scale = timeScale;
                        shouldUpdate = true;
                    }
                }

                //暂停
                if (paused || newPaused)
                {
                    if (paused != newPaused)
                    {
                        if (!ignoreGap)
                        {
                            //被恢复
                            if (paused)
                            {
                                timer.Supplement = timer.CompleteCount;
                                timer.BeginStamp = timeStamp;
                                shouldUpdate = true;
                            }
                            //被暂停
                            else if (timer.Interval != 0 && scale != 0)
                            {
                                restitution += ((timeStamp - beginStamp) * scale) % timer.Interval;
                                timer.Restitution = restitution;
                                shouldUpdate = true;
                            }
                        }
                    }

                    if (shouldUpdate)
                    {
                        if (paralleled)
                            pw.SetComponent(unfilteredChunkIndex, entities[i], timer);
                        else
                            ecb.SetComponent(entities[i], timer);
                    }
                    continue;
                }

                //缩放系数
                if (!ignoreScale)
                {
                    //系数改变
                    if (scale != timeScale)
                    {
                        if (timer.Interval != 0 && scale != 0)
                        {
                            restitution += ((timeStamp - beginStamp) * scale) % timer.Interval;
                            timer.Restitution = restitution;
                        }
                        timer.Supplement = timer.CompleteCount;
                        timer.BeginStamp = timeStamp;
                        scale = timeScale;
                        shouldUpdate = true;
                    }

                    if (timeScale == 0)
                    {
                        if (shouldUpdate)
                        {
                            if (paralleled)
                                pw.SetComponent(unfilteredChunkIndex, entities[i], timer);
                            else
                                ecb.SetComponent(entities[i], timer);
                        }
                        continue;
                    }
                }

                uint times;
                float scalePass = 0;
                if (timer.Interval == 0)
                    times = timer.Supplement + 1;
                else
                {
                    scalePass = (timeStamp - timer.BeginStamp) * scale + restitution;
                    times = timer.Supplement + (uint)math.floor(scalePass / timer.Interval);
                    //修复缩放可能导致的数值溢出bug
                    if (times < timer.CompleteCount)
                    {
                        timer.BeginStamp = timeStamp - times * timer.Interval / scale - (scalePass % timer.Interval);
                        timer.Supplement = 0;
                        timer.CompleteCount = 0;
                        shouldUpdate = true;
                    }
                }
                if (times > 0 && times > timer.CompleteCount)
                {
                    //消费补偿
                    if (restitution > 0)
                    {
                        timer.BeginStamp = timeStamp - (times - timer.Supplement) * timer.Interval / scale - (scalePass % timer.Interval);
                        timer.Restitution = 0;
                    }
                    shouldUpdate = true;
                    if (timer.RepeatCount == -1 || timer.RepeatCount > timer.CompleteCount)
                    {
                        timer.CompleteCount = times;
                        if (paralleled)
                        {
                            pw.SetComponentEnabled<CallbackTag>(unfilteredChunkIndex, entities[i], true);
                        }
                        else
                        {
                            ecb.SetComponentEnabled<CallbackTag>(entities[i], true);
                        }
                        if (timer.RepeatCount != -1 && timer.CompleteCount >= timer.RepeatCount)
                            timer.Flag |= (byte)Flag.Expired;
                    }
                    else
                    {
                        timer.Flag |= (byte)Flag.Expired;
                    }
                }
                if (shouldUpdate)
                {
                    if (paralleled)
                        pw.SetComponent(unfilteredChunkIndex, entities[i], timer);
                    else
                        ecb.SetComponent(entities[i], timer);
                }
            }
        }
    }
}
