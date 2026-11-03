namespace GWE.Domain.Entities;

/// <summary>组织。需求表 I03：信仰与执法机构、施法者议会/学派、贵族领主、行会商会、地下帮派。</summary>
public sealed class Faction
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = "行会";
    public int Prestige { get; set; }
    public long HeadquartersId { get; set; }

    /// <summary>组织金库：发放秘材的悬赏与委托报酬从这里出。</summary>
    public int Treasury { get; set; }
    /// <summary>进阶秘材库存。组织内是有限物资，不是无限商店。</summary>
    public int ReagentStock { get; set; }
    /// <summary>库存上限（固定配额，靠委托取材补货也不会超）。</summary>
    public int ReagentCap { get; set; }
    /// <summary>累计发放件数（用于观察物资流到谁手上）。</summary>
    public int ReagentIssued { get; set; }

    /// <summary>库存告急：低于上限四成时组织开始派“取材委托”。</summary>
    public bool ReagentLow => ReagentStock * 10 < ReagentCap * 4;

    /// <summary>
    /// 商会类组织（行会商会 / 地下帮派）：不发放秘材，靠低买高卖赚差价，
    /// 是全城秘材的批发与零售渠道。学派、教会、贵族只能从它们手里拿货。
    /// </summary>
    public bool IsMerchant => Kind is "行会商会" or "地下帮派";
}

/// <summary>地点（需求表 I01：主城三区、飞地、村镇、港口、遗迹）。隶属 Region，内部含若干 Site。</summary>
public sealed class Locale
{
    public long Id { get; set; }
    public long RegionId { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>平民区 / 贵族区 / 机构区 / 施法者飞地 / 村镇 / 港口 / 遗迹 / 野外。</summary>
    public string Kind { get; set; } = "村镇";
    public int Danger { get; set; }
    public long OwnerFactionId { get; set; }
    /// <summary>基础物资价格系数（>100 贵，<100 便宜）。</summary>
    public int PriceIndex { get; set; } = 100;
}

/// <summary>有向关系边：同一对人物可有多条不同 Kind 的记录。</summary>
public sealed class Relation
{
    public long Id { get; set; }
    public long FromId { get; set; }
    public long ToId { get; set; }
    public string Kind { get; set; } = "friend";
    public int Affinity { get; set; }
    public int Trust { get; set; }
    public int Fear { get; set; }
    public int Debt { get; set; }
    public int Intimacy { get; set; }
    public int LastContactTick { get; set; }
    public string Flags { get; set; } = "";
}

/// <summary>张力：尚未解决的矛盾（需求表 I06：秘密修习者、资料分配、继承争议、材料短缺）。</summary>
public sealed class Tension
{
    public long Id { get; set; }
    public string Type { get; set; } = "";
    public string SubjectIds { get; set; } = "";
    public string Stakes { get; set; } = "";
    public int Urgency { get; set; }
    public int DeadlineTick { get; set; }
    public string State { get; set; } = "open";
    public int CreatedTick { get; set; }
}

/// <summary>回合台账：证明该人物本回合有完整结算（数据模型清单 §4 npc_turn 的最小实现）。</summary>
public sealed class NpcTurn
{
    public long Id { get; set; }
    public long NpcId { get; set; }
    public int Tick { get; set; }
    public string Status { get; set; } = "settled";
    public int ApBudget { get; set; }
    public int ApSpent { get; set; }
    public string ActionsJson { get; set; } = "[]";
    public string TraceJson { get; set; } = "{}";
}
