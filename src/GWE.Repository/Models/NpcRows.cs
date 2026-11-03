using SqlSugar;

namespace GWE.Repository.Models;

[SugarTable("npc")]
public sealed class NpcRow
{
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true)]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "name_key")]
    public string Name { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "sex")]
    public int Sex { get; set; }

    [SugarColumn(ColumnName = "birth_tick")]
    public int BirthTick { get; set; }

    [SugarColumn(ColumnName = "faction_id")]
    public long FactionId { get; set; }

    [SugarColumn(ColumnName = "role_id")]
    public string Role { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "is_main")]
    public bool IsMain { get; set; }

    [SugarColumn(ColumnName = "location_id")]
    public long LocationId { get; set; }

    [SugarColumn(ColumnName = "site_id")]
    public long SiteId { get; set; }

    [SugarColumn(ColumnName = "path")]
    public string Path { get; set; } = "none";

    [SugarColumn(ColumnName = "bloodline_awakened")]
    public bool BloodlineAwakened { get; set; }

    [SugarColumn(ColumnName = "knight_rank")]
    public int KnightRank { get; set; }

    [SugarColumn(ColumnName = "constitution")]
    public int Constitution { get; set; }

    [SugarColumn(ColumnName = "comprehension")]
    public int Comprehension { get; set; }

    [SugarColumn(ColumnName = "charm")]
    public int Charm { get; set; }

    [SugarColumn(ColumnName = "physique")]
    public int Physique { get; set; }

    [SugarColumn(ColumnName = "willpower")]
    public int Willpower { get; set; }

    [SugarColumn(ColumnName = "realm")]
    public int Realm { get; set; }

    [SugarColumn(ColumnName = "power")]
    public int Power { get; set; }

    [SugarColumn(ColumnName = "arcana_rank")]
    public int ArcanaRank { get; set; }

    [SugarColumn(ColumnName = "arcana_credit")]
    public int ArcanaCredit { get; set; }

    [SugarColumn(ColumnName = "cultivation")]
    public int Cultivation { get; set; }

    [SugarColumn(ColumnName = "authority")]
    public int Authority { get; set; }

    [SugarColumn(ColumnName = "reach")]
    public int Reach { get; set; }

    [SugarColumn(ColumnName = "ap_budget")]
    public int ApBudget { get; set; }

    [SugarColumn(ColumnName = "health")]
    public int Health { get; set; }

    [SugarColumn(ColumnName = "mood")]
    public int Mood { get; set; }

    [SugarColumn(ColumnName = "money")]
    public int Money { get; set; }

    [SugarColumn(ColumnName = "personality_json")]
    public string PersonalityJson { get; set; } = "{}";

    [SugarColumn(ColumnName = "needs_json")]
    public string NeedsJson { get; set; } = "{}";

    [SugarColumn(ColumnName = "inventory_json")]
    public string InventoryJson { get; set; } = "[]";

    [SugarColumn(ColumnName = "state_json")]
    public string StateJson { get; set; } = "{}";
}

[SugarTable("relation")]
public sealed class RelationRow
{
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true, IsIdentity = true, ColumnDataType = "INTEGER")]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "from_id")]
    public long FromId { get; set; }

    [SugarColumn(ColumnName = "to_id")]
    public long ToId { get; set; }

    [SugarColumn(ColumnName = "kind")]
    public string Kind { get; set; } = "friend";

    [SugarColumn(ColumnName = "affinity")]
    public int Affinity { get; set; }

    [SugarColumn(ColumnName = "trust")]
    public int Trust { get; set; }

    [SugarColumn(ColumnName = "fear")]
    public int Fear { get; set; }

    [SugarColumn(ColumnName = "debt")]
    public int Debt { get; set; }

    [SugarColumn(ColumnName = "intimacy")]
    public int Intimacy { get; set; }

    [SugarColumn(ColumnName = "last_contact")]
    public int LastContactTick { get; set; }

    [SugarColumn(ColumnName = "flags")]
    public string Flags { get; set; } = "";
}

[SugarTable("event_log")]
public sealed class EventRow
{
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true, IsIdentity = true, ColumnDataType = "INTEGER")]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "tick")]
    public int Tick { get; set; }

    [SugarColumn(ColumnName = "type")]
    public string Type { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "layer")]
    public int Layer { get; set; }

    [SugarColumn(ColumnName = "status")]
    public int Status { get; set; }

    [SugarColumn(ColumnName = "actor_ids")]
    public string ActorIds { get; set; } = "";

    [SugarColumn(ColumnName = "target_ids")]
    public string TargetIds { get; set; } = "";

    [SugarColumn(ColumnName = "location_id")]
    public long LocationId { get; set; }

    [SugarColumn(ColumnName = "root_id", IsNullable = true)]
    public long? RootId { get; set; }

    [SugarColumn(ColumnName = "parent_id", IsNullable = true)]
    public long? ParentId { get; set; }

    [SugarColumn(ColumnName = "depth")]
    public int Depth { get; set; }

    [SugarColumn(ColumnName = "decay")]
    public double Decay { get; set; }

    [SugarColumn(ColumnName = "facts_json")]
    public string FactsJson { get; set; } = "{}";

    [SugarColumn(ColumnName = "visibility")]
    public int Visibility { get; set; }

    [SugarColumn(ColumnName = "salience")]
    public int Salience { get; set; }

    [SugarColumn(ColumnName = "tags")]
    public string Tags { get; set; } = "";

    [SugarColumn(ColumnName = "summary")]
    public string Summary { get; set; } = "";
}
