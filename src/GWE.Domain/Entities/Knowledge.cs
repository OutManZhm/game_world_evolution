namespace GWE.Domain.Entities;

/// <summary>人物记忆：重要事件留下的情绪痕迹，权重会随时间衰减（设计文档 6.1 记忆层）。</summary>
public sealed class Memory
{
    public long Id { get; set; }
    public long NpcId { get; set; }
    public long EventId { get; set; }
    public int Tick { get; set; }
    public string Kind { get; set; } = "经历";
    public string Summary { get; set; } = string.Empty;
    /// <summary>情绪权重：正为快意，负为怨恨，绝对值随回合衰减。</summary>
    public int EmotionalWeight { get; set; }
    public double Decay { get; set; } = 1.0;
}

/// <summary>人物信念：他“以为”的事实 + 置信度。事实可以错，信念不能空（设计文档 6.1 信念层）。</summary>
public sealed class Belief
{
    public long Id { get; set; }
    public long NpcId { get; set; }
    /// <summary>事实键，event:{id} 或 rumor:{主题}，用于去重与传播。</summary>
    public string FactKey { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    /// <summary>置信度 0..100，传播一次衰减。</summary>
    public int Confidence { get; set; } = 85;
    public string Source { get; set; } = "亲历";
    public int Tick { get; set; }
}

/// <summary>知识传递：一条信息从谁传到谁、走什么渠道（设计文档 8.x knowledge_delivery）。</summary>
public sealed class KnowledgeDelivery
{
    public long Id { get; set; }
    public long EventId { get; set; }
    public long FromId { get; set; }
    public long ToId { get; set; }
    /// <summary>亲历 / 目击 / 传闻 / 告发。</summary>
    public string Channel { get; set; } = "目击";
    public int Tick { get; set; }
}

/// <summary>
/// 定时事件排程：L0 脚本主线与 L2 日历活动。
/// 到期前一回合以 Planned 状态入 event_log，到期当回合转为 Resolved 并施加效果。
/// </summary>
public sealed class EventSchedule
{
    public long Id { get; set; }
    /// <summary>L0 = 脚本主线，L2 = 日历活动。</summary>
    public string Kind { get; set; } = "Calendar";
    public string Name { get; set; } = string.Empty;
    public int DueTick { get; set; }
    /// <summary>效果：{"mood":5,"money":3,"tension":"药剂短缺","urgency":10,"localeId":2}</summary>
    public string PayloadJson { get; set; } = "{}";
    /// <summary>pending / planned / resolved。</summary>
    public string State { get; set; } = "pending";
    public long EventId { get; set; }
}
