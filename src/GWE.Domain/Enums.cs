namespace GWE.Domain;

/// <summary>事件源分层：L0 脚本主线 / L1 世界压力 / L2 日历活动 / L3 NPC 涌现 / L4 环境遭遇。</summary>
public enum EventLayer
{
    Script = 0,
    WorldPressure = 1,
    Calendar = 2,
    NpcEmergent = 3,
    Environment = 4
}

public enum EventStatus
{
    Planned = 0,
    Resolved = 1,
    Aborted = 2,
    Expired = 3
}

public enum EventVisibility
{
    Private = 0,
    Witness = 1,
    Local = 2,
    Public = 3
}

/// <summary>NPC 行为目录（需求表 Q08：工作、交易、冥想修炼、研究、制作、社交、迁移、探索遗迹 + 疗伤、进阶、休整）。</summary>
public enum NpcActionType
{
    Rest = 0,
    /// <summary>冥想修炼：积累法术环级进度，效率吃根骨（N08）。</summary>
    Meditate = 1,
    /// <summary>研究：积累学术积分，效率吃悟性（N08）。</summary>
    Research = 2,
    Work = 3,
    /// <summary>交易：买卖基础物资与药剂（E05：高阶材料走学术点，此处只做金钱市场）。</summary>
    Trade = 4,
    /// <summary>制作：药剂/卷轴 = 材料 + 配方 + 设施，缺一不可（E02）。</summary>
    Craft = 5,
    Heal = 6,
    Socialize = 7,
    /// <summary>迁移：换地点，受身份与路费约束（T05/P05）。</summary>
    Migrate = 8,
    /// <summary>探索遗迹：可能触发 L4 环境遭遇。</summary>
    Explore = 9,
    /// <summary>进阶仪式：强制消耗金钱与进阶秘材，缺料无法进阶（红线 Q12）。</summary>
    Advance = 10
}

public static class ActionText
{
    public static string Of(NpcActionType action) => action switch
    {
        NpcActionType.Rest => "休整",
        NpcActionType.Meditate => "冥想",
        NpcActionType.Research => "研究",
        NpcActionType.Work => "工作",
        NpcActionType.Trade => "交易",
        NpcActionType.Craft => "制作",
        NpcActionType.Heal => "疗伤",
        NpcActionType.Socialize => "社交",
        NpcActionType.Migrate => "迁移",
        NpcActionType.Explore => "探索",
        NpcActionType.Advance => "进阶",
        _ => "未知"
    };
}
