using SqlSugar;

namespace GWE.Repository.Models;

[SugarTable("world_state")]
public sealed class WorldStateRow
{
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true)]
    public int Id { get; set; }

    [SugarColumn(ColumnName = "tick")]
    public int Tick { get; set; }

    [SugarColumn(ColumnName = "year")]
    public int Year { get; set; }

    [SugarColumn(ColumnName = "month")]
    public int Month { get; set; }

    [SugarColumn(ColumnName = "seed")]
    public int Seed { get; set; }

    [SugarColumn(ColumnName = "pressure")]
    public int Pressure { get; set; }
}

[SugarTable("faction")]
public sealed class FactionRow
{
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true)]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "name")]
    public string Name { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "kind")]
    public string Kind { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "prestige")]
    public int Prestige { get; set; }

    [SugarColumn(ColumnName = "headquarters_id")]
    public long HeadquartersId { get; set; }

    [SugarColumn(ColumnName = "treasury")]
    public int Treasury { get; set; }

    [SugarColumn(ColumnName = "reagent_stock")]
    public int ReagentStock { get; set; }

    [SugarColumn(ColumnName = "reagent_cap")]
    public int ReagentCap { get; set; }

    [SugarColumn(ColumnName = "reagent_issued")]
    public int ReagentIssued { get; set; }
}

[SugarTable("locale")]
public sealed class LocaleRow
{
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true)]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "region_id")]
    public long RegionId { get; set; }

    [SugarColumn(ColumnName = "name")]
    public string Name { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "kind")]
    public string Kind { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "danger")]
    public int Danger { get; set; }

    [SugarColumn(ColumnName = "owner_faction_id")]
    public long OwnerFactionId { get; set; }

    [SugarColumn(ColumnName = "price_index")]
    public int PriceIndex { get; set; }
}

[SugarTable("region")]
public sealed class RegionRow
{
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true)]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "name")]
    public string Name { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "kind")]
    public string Kind { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "danger")]
    public int Danger { get; set; }
}

[SugarTable("site")]
public sealed class SiteRow
{
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true)]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "locale_id")]
    public long LocaleId { get; set; }

    [SugarColumn(ColumnName = "name")]
    public string Name { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "kind")]
    public string Kind { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "facilities")]
    public string Facilities { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "specialty", IsNullable = true)]
    public string? Specialty { get; set; }
}

[SugarTable("memory")]
public sealed class MemoryRow
{
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true, IsIdentity = true, ColumnDataType = "INTEGER")]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "npc_id")]
    public long NpcId { get; set; }

    [SugarColumn(ColumnName = "event_id")]
    public long EventId { get; set; }

    [SugarColumn(ColumnName = "tick")]
    public int Tick { get; set; }

    [SugarColumn(ColumnName = "kind")]
    public string Kind { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "summary")]
    public string Summary { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "emotional_weight")]
    public int EmotionalWeight { get; set; }

    [SugarColumn(ColumnName = "decay")]
    public double Decay { get; set; }
}

[SugarTable("npc_belief")]
public sealed class BeliefRow
{
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true, IsIdentity = true, ColumnDataType = "INTEGER")]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "npc_id")]
    public long NpcId { get; set; }

    [SugarColumn(ColumnName = "fact_key")]
    public string FactKey { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "content")]
    public string Content { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "confidence")]
    public int Confidence { get; set; }

    [SugarColumn(ColumnName = "source")]
    public string Source { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "tick")]
    public int Tick { get; set; }
}

[SugarTable("knowledge_delivery")]
public sealed class KnowledgeDeliveryRow
{
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true, IsIdentity = true, ColumnDataType = "INTEGER")]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "event_id")]
    public long EventId { get; set; }

    [SugarColumn(ColumnName = "from_id")]
    public long FromId { get; set; }

    [SugarColumn(ColumnName = "to_id")]
    public long ToId { get; set; }

    [SugarColumn(ColumnName = "channel")]
    public string Channel { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "tick")]
    public int Tick { get; set; }
}

[SugarTable("event_schedule")]
public sealed class EventScheduleRow
{
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true)]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "kind")]
    public string Kind { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "name")]
    public string Name { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "due_tick")]
    public int DueTick { get; set; }

    [SugarColumn(ColumnName = "payload_json")]
    public string PayloadJson { get; set; } = "{}";

    [SugarColumn(ColumnName = "state")]
    public string State { get; set; } = "pending";

    [SugarColumn(ColumnName = "event_id")]
    public long EventId { get; set; }
}

[SugarTable("tension")]
public sealed class TensionRow
{
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true, IsIdentity = true, ColumnDataType = "INTEGER")]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "type")]
    public string Type { get; set; } = "";

    [SugarColumn(ColumnName = "subject_ids")]
    public string SubjectIds { get; set; } = "";

    [SugarColumn(ColumnName = "stakes")]
    public string Stakes { get; set; } = "";

    [SugarColumn(ColumnName = "urgency")]
    public int Urgency { get; set; }

    [SugarColumn(ColumnName = "deadline_tick")]
    public int DeadlineTick { get; set; }

    [SugarColumn(ColumnName = "state")]
    public string State { get; set; } = "open";

    [SugarColumn(ColumnName = "created_tick")]
    public int CreatedTick { get; set; }
}

[SugarTable("npc_turn")]
public sealed class NpcTurnRow
{
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true, IsIdentity = true, ColumnDataType = "INTEGER")]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "npc_id")]
    public long NpcId { get; set; }

    [SugarColumn(ColumnName = "tick")]
    public int Tick { get; set; }

    [SugarColumn(ColumnName = "status")]
    public string Status { get; set; } = "settled";

    [SugarColumn(ColumnName = "ap_budget")]
    public int ApBudget { get; set; }

    [SugarColumn(ColumnName = "ap_spent")]
    public int ApSpent { get; set; }

    [SugarColumn(ColumnName = "actions_json")]
    public string ActionsJson { get; set; } = "[]";

    [SugarColumn(ColumnName = "trace_json")]
    public string TraceJson { get; set; } = "{}";
}
