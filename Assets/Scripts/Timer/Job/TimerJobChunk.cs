using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;

namespace TT.Timer
{
    [BurstCompile]
    internal struct TimerJobChunk : IJobChunk
    {
        [ReadOnly]
        public ComponentTypeHandle<Timer> ComponentHandle;
        [ReadOnly]
        public EntityTypeHandle EntityHandle;
        public EntityCommandBuffer ECB;
        public float TimeStamp;
        public float TimeScale;
        public bool Paralleled;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var timers = chunk.GetNativeArray(ref ComponentHandle);
            var entities = chunk.GetNativeArray(EntityHandle);
            TimerProcessor.Process(ref entities, ref timers, TimeStamp, TimeScale, in ECB, unfilteredChunkIndex);
        }
    }

    [BurstCompile]
    internal struct TimerJobChunkParallel : IJobChunk
    {
        [ReadOnly]
        public ComponentTypeHandle<Timer> ComponentHandle;
        [ReadOnly]
        public EntityTypeHandle EntityHandle;
        public EntityCommandBuffer.ParallelWriter ECB;
        public float TimeStamp;
        public float TimeScale;
        public bool Paralleled;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var timers = chunk.GetNativeArray(ref ComponentHandle);
            var entities = chunk.GetNativeArray(EntityHandle);
            TimerProcessor.Process(ref entities, ref timers, TimeStamp, TimeScale, default(EntityCommandBuffer), unfilteredChunkIndex, true, ECB);
        }
    }
}
