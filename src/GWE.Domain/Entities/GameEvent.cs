namespace GWE.Domain.Entities;

/// <summary>
/// 事件 / 事件计划节点。facts 为结构化事实 JSON，界面文案只由它投影生成。
/// </summary>
public sealed class GameEvent
{
    public long Id { get; set; }
    public int Tick { get; set; }
    public string Type { get; set; } = string.Empty;
    public EventLayer Layer { get; set; } = EventLayer.NpcEmergent;
    public EventStatus Status { get; set; } = EventStatus.Resolved;
    public string ActorIds { get; set; } = "";
    public string TargetIds { get; set; } = "";
    public long LocationId { get; set; }
    public long? RootId { get; set; }
    public long? ParentId { get; set; }
    public int Depth { get; set; }
    public double Decay { get; set; } = 1;
    public string FactsJson { get; set; } = "{}";
    public EventVisibility Visibility { get; set; } = EventVisibility.Local;
    public int Salience { get; set; }
    public string Tags { get; set; } = "";
    /// <summary>叙事层投影：供 UI 直接展示的一行文本，由 facts 生成，不是事实本身。</summary>
    public string Summary { get; set; } = "";
}
