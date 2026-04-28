using Unity.Entities;

namespace TT.Timer
{
    public enum Flag : byte
    {
        None = 0,
        Paused = 0b1,               //0正常，1暂停
        Expired = 0b10,             //0正常，1过期
        IgnoreScale = 0b100,        //0缩放，1不缩放
        IgnoreGap = 0b1000,         //0补偿暂停耗时，1不补偿
    }

    //字节对齐，32 byte
    public struct Timer : IComponentData
    {
        //4 byte 起始时间戳
        public float BeginStamp;
        //4 byte 间隔
        public float Interval;
        //4 byte 未消费补偿
        public float Restitution;
        //4 byte 缩放系数
        public float Scale;
        //4 byte 总执行次数
        public int RepeatCount;
        //4 byte 补充(缩放和暂停前的总次数)
        public uint Supplement;
        //4 byte 已执行总次数
        public uint CompleteCount;
        //1 byte 上一次状态
        public byte Current;
        //1 byte 新状态
        public byte Flag;
    }

    public struct CallbackTag : IComponentData, IEnableableComponent { }

    public struct DestroyTag : IComponentData, IEnableableComponent { }
}

