namespace GWE.Domain.Entities;

/// <summary>世界状态单行表：tick = 月。</summary>
public sealed class WorldState
{
    public int Id { get; set; } = 1;
    public int Tick { get; set; }
    public int Year { get; set; } = 1024;
    public int Month { get; set; } = 1;
    public int Seed { get; set; } = 20261103;
    /// <summary>世界压力 0..100，后续驱动 L1 事件。</summary>
    public int Pressure { get; set; } = 10;
}
