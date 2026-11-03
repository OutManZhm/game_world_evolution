using GWE.Domain.Entities;
using GWE.Repository.Models;

namespace GWE.Repository;

/// <summary>持久化模型与领域实体互转。Domain 不依赖任何 ORM。</summary>
public static class Mappers
{
    public static WorldState ToDomain(this WorldStateRow r) => new()
    {
        Id = r.Id, Tick = r.Tick, Year = r.Year, Month = r.Month, Seed = r.Seed, Pressure = r.Pressure
    };

    public static WorldStateRow ToRow(this WorldState e) => new()
    {
        Id = e.Id, Tick = e.Tick, Year = e.Year, Month = e.Month, Seed = e.Seed, Pressure = e.Pressure
    };

    public static Faction ToDomain(this FactionRow r) => new()
    {
        Id = r.Id, Name = r.Name, Kind = r.Kind, Prestige = r.Prestige, HeadquartersId = r.HeadquartersId,
        Treasury = r.Treasury, ReagentStock = r.ReagentStock, ReagentCap = r.ReagentCap,
        ReagentIssued = r.ReagentIssued
    };

    public static FactionRow ToRow(this Faction e) => new()
    {
        Id = e.Id, Name = e.Name, Kind = e.Kind, Prestige = e.Prestige, HeadquartersId = e.HeadquartersId,
        Treasury = e.Treasury, ReagentStock = e.ReagentStock, ReagentCap = e.ReagentCap,
        ReagentIssued = e.ReagentIssued
    };

    public static Locale ToDomain(this LocaleRow r) => new()
    {
        Id = r.Id, RegionId = r.RegionId, Name = r.Name, Kind = r.Kind, Danger = r.Danger,
        OwnerFactionId = r.OwnerFactionId, PriceIndex = r.PriceIndex
    };

    public static LocaleRow ToRow(this Locale e) => new()
    {
        Id = e.Id, RegionId = e.RegionId, Name = e.Name, Kind = e.Kind, Danger = e.Danger,
        OwnerFactionId = e.OwnerFactionId, PriceIndex = e.PriceIndex
    };

    public static Region ToDomain(this RegionRow r) => new()
    {
        Id = r.Id, Name = r.Name, Kind = r.Kind, Danger = r.Danger
    };

    public static RegionRow ToRow(this Region e) => new()
    {
        Id = e.Id, Name = e.Name, Kind = e.Kind, Danger = e.Danger
    };

    public static Site ToDomain(this SiteRow r) => new()
    {
        Id = r.Id, LocaleId = r.LocaleId, Name = r.Name, Kind = r.Kind,
        Facilities = r.Facilities, Specialty = r.Specialty ?? string.Empty
    };

    public static SiteRow ToRow(this Site e) => new()
    {
        Id = e.Id, LocaleId = e.LocaleId, Name = e.Name, Kind = e.Kind,
        Facilities = e.Facilities, Specialty = e.Specialty
    };

    public static Memory ToDomain(this MemoryRow r) => new()
    {
        Id = r.Id, NpcId = r.NpcId, EventId = r.EventId, Tick = r.Tick, Kind = r.Kind,
        Summary = r.Summary, EmotionalWeight = r.EmotionalWeight, Decay = r.Decay
    };

    public static MemoryRow ToRow(this Memory e) => new()
    {
        Id = e.Id, NpcId = e.NpcId, EventId = e.EventId, Tick = e.Tick, Kind = e.Kind,
        Summary = e.Summary, EmotionalWeight = e.EmotionalWeight, Decay = e.Decay
    };

    public static Belief ToDomain(this BeliefRow r) => new()
    {
        Id = r.Id, NpcId = r.NpcId, FactKey = r.FactKey, Content = r.Content,
        Confidence = r.Confidence, Source = r.Source, Tick = r.Tick
    };

    public static BeliefRow ToRow(this Belief e) => new()
    {
        Id = e.Id, NpcId = e.NpcId, FactKey = e.FactKey, Content = e.Content,
        Confidence = e.Confidence, Source = e.Source, Tick = e.Tick
    };

    public static KnowledgeDelivery ToDomain(this KnowledgeDeliveryRow r) => new()
    {
        Id = r.Id, EventId = r.EventId, FromId = r.FromId, ToId = r.ToId,
        Channel = r.Channel, Tick = r.Tick
    };

    public static KnowledgeDeliveryRow ToRow(this KnowledgeDelivery e) => new()
    {
        Id = e.Id, EventId = e.EventId, FromId = e.FromId, ToId = e.ToId,
        Channel = e.Channel, Tick = e.Tick
    };

    public static EventSchedule ToDomain(this EventScheduleRow r) => new()
    {
        Id = r.Id, Kind = r.Kind, Name = r.Name, DueTick = r.DueTick,
        PayloadJson = r.PayloadJson, State = r.State, EventId = r.EventId
    };

    public static EventScheduleRow ToRow(this EventSchedule e) => new()
    {
        Id = e.Id, Kind = e.Kind, Name = e.Name, DueTick = e.DueTick,
        PayloadJson = e.PayloadJson, State = e.State, EventId = e.EventId
    };

    public static Tension ToDomain(this TensionRow r) => new()
    {
        Id = r.Id, Type = r.Type, SubjectIds = r.SubjectIds, Stakes = r.Stakes,
        Urgency = r.Urgency, DeadlineTick = r.DeadlineTick, State = r.State, CreatedTick = r.CreatedTick
    };

    public static TensionRow ToRow(this Tension e) => new()
    {
        Id = e.Id, Type = e.Type, SubjectIds = e.SubjectIds, Stakes = e.Stakes,
        Urgency = e.Urgency, DeadlineTick = e.DeadlineTick, State = e.State, CreatedTick = e.CreatedTick
    };

    public static NpcTurn ToDomain(this NpcTurnRow r) => new()
    {
        Id = r.Id, NpcId = r.NpcId, Tick = r.Tick, Status = r.Status,
        ApBudget = r.ApBudget, ApSpent = r.ApSpent, ActionsJson = r.ActionsJson, TraceJson = r.TraceJson
    };

    public static NpcTurnRow ToRow(this NpcTurn e) => new()
    {
        Id = e.Id, NpcId = e.NpcId, Tick = e.Tick, Status = e.Status,
        ApBudget = e.ApBudget, ApSpent = e.ApSpent, ActionsJson = e.ActionsJson, TraceJson = e.TraceJson
    };

    public static Relation ToDomain(this RelationRow r) => new()
    {
        Id = r.Id, FromId = r.FromId, ToId = r.ToId, Kind = r.Kind, Affinity = r.Affinity,
        Trust = r.Trust, Fear = r.Fear, Debt = r.Debt, Intimacy = r.Intimacy,
        LastContactTick = r.LastContactTick, Flags = r.Flags
    };

    public static RelationRow ToRow(this Relation e) => new()
    {
        Id = e.Id, FromId = e.FromId, ToId = e.ToId, Kind = e.Kind, Affinity = e.Affinity,
        Trust = e.Trust, Fear = e.Fear, Debt = e.Debt, Intimacy = e.Intimacy,
        LastContactTick = e.LastContactTick, Flags = e.Flags
    };

    public static Npc ToDomain(this NpcRow r) => new()
    {
        Id = r.Id, Name = r.Name, Sex = r.Sex, BirthTick = r.BirthTick, FactionId = r.FactionId,
        Role = r.Role, IsMain = r.IsMain, LocationId = r.LocationId, SiteId = r.SiteId,
        Path = r.Path, BloodlineAwakened = r.BloodlineAwakened, KnightRank = r.KnightRank,
        Constitution = r.Constitution, Comprehension = r.Comprehension, Charm = r.Charm,
        Physique = r.Physique, Willpower = r.Willpower,
        Realm = r.Realm, Power = r.Power, ArcanaRank = r.ArcanaRank, ArcanaCredit = r.ArcanaCredit,
        Cultivation = r.Cultivation, Authority = r.Authority, Reach = r.Reach, ApBudget = r.ApBudget,
        Health = r.Health, Mood = r.Mood, Money = r.Money,
        PersonalityJson = r.PersonalityJson, NeedsJson = r.NeedsJson,
        InventoryJson = r.InventoryJson, StateJson = r.StateJson
    };

    public static NpcRow ToRow(this Npc e) => new()
    {
        Id = e.Id, Name = e.Name, Sex = e.Sex, BirthTick = e.BirthTick, FactionId = e.FactionId,
        Role = e.Role, IsMain = e.IsMain, LocationId = e.LocationId, SiteId = e.SiteId,
        Path = e.Path, BloodlineAwakened = e.BloodlineAwakened, KnightRank = e.KnightRank,
        Constitution = e.Constitution, Comprehension = e.Comprehension, Charm = e.Charm,
        Physique = e.Physique, Willpower = e.Willpower,
        Realm = e.Realm, Power = e.Power, ArcanaRank = e.ArcanaRank, ArcanaCredit = e.ArcanaCredit,
        Cultivation = e.Cultivation, Authority = e.Authority, Reach = e.Reach, ApBudget = e.ApBudget,
        Health = e.Health, Mood = e.Mood, Money = e.Money,
        PersonalityJson = e.PersonalityJson, NeedsJson = e.NeedsJson,
        InventoryJson = e.InventoryJson, StateJson = e.StateJson
    };

    public static GameEvent ToDomain(this EventRow r) => new()
    {
        Id = r.Id, Tick = r.Tick, Type = r.Type, Layer = (Domain.EventLayer)r.Layer,
        Status = (Domain.EventStatus)r.Status, ActorIds = r.ActorIds, TargetIds = r.TargetIds,
        LocationId = r.LocationId, RootId = r.RootId, ParentId = r.ParentId, Depth = r.Depth,
        Decay = r.Decay, FactsJson = r.FactsJson, Visibility = (Domain.EventVisibility)r.Visibility,
        Salience = r.Salience, Tags = r.Tags, Summary = r.Summary
    };

    public static EventRow ToRow(this GameEvent e) => new()
    {
        Id = e.Id, Tick = e.Tick, Type = e.Type, Layer = (int)e.Layer, Status = (int)e.Status,
        ActorIds = e.ActorIds, TargetIds = e.TargetIds, LocationId = e.LocationId,
        RootId = e.RootId, ParentId = e.ParentId, Depth = e.Depth, Decay = e.Decay,
        FactsJson = e.FactsJson, Visibility = (int)e.Visibility, Salience = e.Salience,
        Tags = e.Tags, Summary = e.Summary
    };
}
