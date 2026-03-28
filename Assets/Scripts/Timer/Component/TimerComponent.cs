using Unity.Entities;

namespace TT.Timer
{
    public enum Flag : byte
    {
        None = 0,
        Paused = 1,
        Expired = 2,
    }

    //字节对齐，28 byte
    public struct Timer : IComponentData
    {
        //4 byte 起始时间戳
        public float BeginStamp;
        //4 byte 暂停时间戳
        public float PauseStamp;
        //4 byte 间隔
        public float Interval;
        //4 byte 补偿
        public float Restitution;
        //4 byte 总执行次数
        public int RepeatCount;
        //4 byte 已执行次数
        public uint CompleteCount;
        //1 byte
        public Flag Flag;
    }

    public struct CallbackTag : IComponentData, IEnableableComponent { }

    public struct DestroyTag : IComponentData, IEnableableComponent { }
}

