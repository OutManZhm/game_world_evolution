namespace GWE.Service.Contracts;

/// <summary>主界面 NPC 状态行（只读投影，不是实体本身）。</summary>
public sealed record NpcStatusDto
{
    public long Id { get; init; }
    public string Name { get; init; } = "";
    public string Faction { get; init; } = "";
    public string Role { get; init; } = "";
    public string Location { get; init; } = "";
    public string Path { get; init; } = "";
    public string RankTitle { get; init; } = "";
    public string Site { get; init; } = "";
    public string Facilities { get; init; } = "";
    public int Age { get; init; }
    public int Power { get; init; }
    public int ArcanaRank { get; init; }
    public int Cultivation { get; init; }
    public int AdvanceThreshold { get; init; }
    public bool HasReagent { get; init; }
    public int Constitution { get; init; }
    public int Comprehension { get; init; }
    public int Charm { get; init; }
    public int Physique { get; init; }
    public int Willpower { get; init; }
    public int Health { get; init; }
    public int Mood { get; init; }
    public int Money { get; init; }
    public int ApBudget { get; init; }
    public string LastAction { get; init; } = "";
}

public sealed record EventDto
{
    public int Tick { get; init; }
    public string Time { get; init; } = "";
    public string Type { get; init; } = "";
    public string Actor { get; init; } = "";
    public string Target { get; init; } = "";
    public string Summary { get; init; } = "";
    public int Salience { get; init; }
    public string Status { get; init; } = "";
}

/// <summary>人物档案：记忆、信念与知识来源。界面按需投影，不参与模拟。</summary>
public sealed record NpcProfileDto
{
    public long Id { get; init; }
    public string Name { get; init; } = "";
    public string Title { get; init; } = "";
    public string Location { get; init; } = "";
    public IReadOnlyList<MemoryDto> Memories { get; init; } = [];
    public IReadOnlyList<BeliefDto> Beliefs { get; init; } = [];
}

public sealed record MemoryDto
{
    public string Tick { get; init; } = "";
    public string Kind { get; init; } = "";
    public string Summary { get; init; } = "";
    public int Weight { get; init; }
}

public sealed record BeliefDto
{
    public string Tick { get; init; } = "";
    public string Content { get; init; } = "";
    public int Confidence { get; init; }
    public string Source { get; init; } = "";
}

/// <summary>组织与秘材库存（有限物资，靠委托取材补货）。</summary>
public sealed record FactionDto
{
    public long Id { get; init; }
    public string Name { get; init; } = "";
    public string Kind { get; init; } = "";
    public int Prestige { get; init; }
    public int Treasury { get; init; }
    public int ReagentStock { get; init; }
    public int ReagentCap { get; init; }
    public int ReagentIssued { get; init; }
    /// <summary>金库软上限：越过后多出的部分当月分红给成员。</summary>
    public int SoftCap { get; init; }
    public int Members { get; init; }
    public int Awaiting { get; init; }
    public string Status => ReagentStock <= 0 ? "断料" : ReagentStock * 10 < ReagentCap * 4 ? "告急" : "充足";
}

public sealed record TensionDto{
    public long Id { get; init; }
    public string Type { get; init; } = "";
    public string Stakes { get; init; } = "";
    public int Urgency { get; init; }
    public int DeadlineTick { get; init; }
    public string State { get; init; } = "";
}

public sealed record TurnResultDto
{
    public int Tick { get; init; }
    public int Year { get; init; }
    public int Month { get; init; }
    public IReadOnlyList<EventDto> Events { get; init; } = [];
}

public sealed record WorldClockDto
{
    public int Tick { get; init; }
    public int Year { get; init; }
    public int Month { get; init; }
    public string Text => $"天纪 {Year} 年 {Month} 月 · 第 {Tick} 回合";
}
