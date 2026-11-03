using GWE.Domain.Entities;
using GWE.Repository.Models;
using SqlSugar;

namespace GWE.Repository.Repositories;

public interface IWorldRepository
{
    WorldState Get();
    void Save(WorldState state);
}

public interface INpcRepository
{
    IReadOnlyList<Npc> GetAll();
    IReadOnlyList<Npc> GetMain();
    void UpdateRange(IEnumerable<Npc> npcs);
}

public interface IFactionRepository
{
    IReadOnlyList<Faction> GetAll();

    /// <summary>回写组织库存与金库（发放秘材、委托报酬后调用）。</summary>
    void UpdateRange(IEnumerable<Faction> factions);
}

public interface ILocaleRepository
{
    IReadOnlyList<Locale> GetAll();
}

public interface ISiteRepository
{
    IReadOnlyList<Site> GetAll();
}

public interface IRegionRepository
{
    IReadOnlyList<Region> GetAll();
}

public interface IMemoryRepository
{
    IReadOnlyList<Memory> GetAll();
    IReadOnlyList<Memory> GetByNpc(long npcId);
    void InsertRange(IEnumerable<Memory> memories);
    void UpdateRange(IEnumerable<Memory> memories);
}

public interface IBeliefRepository
{
    IReadOnlyList<Belief> GetAll();
    IReadOnlyList<Belief> GetByNpc(long npcId);
    void InsertRange(IEnumerable<Belief> beliefs);
    void UpdateRange(IEnumerable<Belief> beliefs);
}

public interface IKnowledgeDeliveryRepository
{
    void InsertRange(IEnumerable<KnowledgeDelivery> deliveries);
    IReadOnlyList<KnowledgeDelivery> GetRecent(int count);
}

public interface IEventScheduleRepository
{
    IReadOnlyList<EventSchedule> GetAll();
    void UpsertRange(IEnumerable<EventSchedule> schedules);
}

public interface IRelationRepository
{
    IReadOnlyList<Relation> GetAll();
    void UpsertRange(IEnumerable<Relation> relations);
}

public interface IEventRepository
{
    /// <summary>单条插入并返回数据库 id（用于父事件引用）。</summary>
    long Insert(GameEvent e);
    /// <summary>插入后回填因果字段（root/parent/depth）。</summary>
    void Update(GameEvent e);
    void InsertRange(IEnumerable<GameEvent> events);
    GameEvent? Get(long id);
    IReadOnlyList<GameEvent> GetByTick(int tick);
    IReadOnlyList<GameEvent> GetRecent(int count);
}

public interface ITensionRepository
{
    IReadOnlyList<Tension> GetAll();
    void UpsertRange(IEnumerable<Tension> tensions);
}

public interface INpcTurnRepository
{
    void InsertRange(IEnumerable<NpcTurn> turns);
}

public sealed class WorldRepository(IDbSession session) : IWorldRepository
{
    private ISqlSugarClient Db => session.Client;

    public WorldState Get() =>
        Db.Queryable<WorldStateRow>().First(r => r.Id == 1)?.ToDomain()
        ?? throw new InvalidOperationException("world_state 未初始化。");

    public void Save(WorldState state) =>
        Db.Updateable(state.ToRow()).ExecuteCommand();
}

public sealed class NpcRepository(IDbSession session) : INpcRepository
{
    private ISqlSugarClient Db => session.Client;

    public IReadOnlyList<Npc> GetAll() =>
        Db.Queryable<NpcRow>().OrderBy(r => r.Id).ToList().Select(r => r.ToDomain()).ToList();

    public IReadOnlyList<Npc> GetMain() =>
        Db.Queryable<NpcRow>().Where(r => r.IsMain).OrderBy(r => r.Id).ToList()
            .Select(r => r.ToDomain()).ToList();

    public void UpdateRange(IEnumerable<Npc> npcs) =>
        Db.Updateable(npcs.Select(n => n.ToRow()).ToList()).ExecuteCommand();
}

public sealed class FactionRepository(IDbSession session) : IFactionRepository
{
    public IReadOnlyList<Faction> GetAll() =>
        session.Client.Queryable<FactionRow>().OrderBy(r => r.Id).ToList().Select(r => r.ToDomain()).ToList();

    public void UpdateRange(IEnumerable<Faction> factions) =>
        session.Client.Updateable(factions.Select(f => f.ToRow()).ToList()).ExecuteCommand();
}

public sealed class LocaleRepository(IDbSession session) : ILocaleRepository
{
    public IReadOnlyList<Locale> GetAll() =>
        session.Client.Queryable<LocaleRow>().ToList().Select(r => r.ToDomain()).ToList();
}

public sealed class SiteRepository(IDbSession session) : ISiteRepository
{
    public IReadOnlyList<Site> GetAll() =>
        session.Client.Queryable<SiteRow>().OrderBy(r => r.Id).ToList().Select(r => r.ToDomain()).ToList();
}

public sealed class RegionRepository(IDbSession session) : IRegionRepository
{
    public IReadOnlyList<Region> GetAll() =>
        session.Client.Queryable<RegionRow>().ToList().Select(r => r.ToDomain()).ToList();
}

public sealed class MemoryRepository(IDbSession session) : IMemoryRepository
{
    private ISqlSugarClient Db => session.Client;

    public IReadOnlyList<Memory> GetAll() =>
        Db.Queryable<MemoryRow>().ToList().Select(r => r.ToDomain()).ToList();

    public IReadOnlyList<Memory> GetByNpc(long npcId) =>
        Db.Queryable<MemoryRow>().Where(r => r.NpcId == npcId).OrderBy(r => r.Tick, OrderByType.Desc)
            .ToList().Select(r => r.ToDomain()).ToList();

    public void InsertRange(IEnumerable<Memory> memories)
    {
        var rows = memories.Select(m => m.ToRow()).ToList();
        if (rows.Count > 0)
        {
            Db.Insertable(rows).ExecuteCommand();
        }
    }

    public void UpdateRange(IEnumerable<Memory> memories)
    {
        var rows = memories.Where(m => m.Id > 0).Select(m => m.ToRow()).ToList();
        if (rows.Count > 0)
        {
            Db.Updateable(rows).ExecuteCommand();
        }
    }
}

public sealed class BeliefRepository(IDbSession session) : IBeliefRepository
{
    private ISqlSugarClient Db => session.Client;

    public IReadOnlyList<Belief> GetAll() =>
        Db.Queryable<BeliefRow>().ToList().Select(r => r.ToDomain()).ToList();

    public IReadOnlyList<Belief> GetByNpc(long npcId) =>
        Db.Queryable<BeliefRow>().Where(r => r.NpcId == npcId).OrderBy(r => r.Tick, OrderByType.Desc)
            .ToList().Select(r => r.ToDomain()).ToList();

    public void InsertRange(IEnumerable<Belief> beliefs)
    {
        var rows = beliefs.Select(b => b.ToRow()).ToList();
        if (rows.Count > 0)
        {
            Db.Insertable(rows).ExecuteCommand();
        }
    }

    public void UpdateRange(IEnumerable<Belief> beliefs)
    {
        var rows = beliefs.Where(b => b.Id > 0).Select(b => b.ToRow()).ToList();
        if (rows.Count > 0)
        {
            Db.Updateable(rows).ExecuteCommand();
        }
    }
}

public sealed class KnowledgeDeliveryRepository(IDbSession session) : IKnowledgeDeliveryRepository
{
    private ISqlSugarClient Db => session.Client;

    public void InsertRange(IEnumerable<KnowledgeDelivery> deliveries)
    {
        var rows = deliveries.Select(d => d.ToRow()).ToList();
        if (rows.Count > 0)
        {
            Db.Insertable(rows).ExecuteCommand();
        }
    }

    public IReadOnlyList<KnowledgeDelivery> GetRecent(int count) =>
        Db.Queryable<KnowledgeDeliveryRow>().OrderBy(r => r.Id, OrderByType.Desc).Take(count).ToList()
            .Select(r => r.ToDomain()).ToList();
}

public sealed class EventScheduleRepository(IDbSession session) : IEventScheduleRepository
{
    private ISqlSugarClient Db => session.Client;

    public IReadOnlyList<EventSchedule> GetAll() =>
        Db.Queryable<EventScheduleRow>().OrderBy(r => r.DueTick).ToList().Select(r => r.ToDomain()).ToList();

    public void UpsertRange(IEnumerable<EventSchedule> schedules)
    {
        foreach (var schedule in schedules)
        {
            if (schedule.Id > 0)
            {
                Db.Updateable(schedule.ToRow()).ExecuteCommand();
            }
            else
            {
                schedule.Id = Db.Insertable(schedule.ToRow()).ExecuteReturnIdentity();
            }
        }
    }
}

public sealed class RelationRepository(IDbSession session) : IRelationRepository
{
    private ISqlSugarClient Db => session.Client;

    public IReadOnlyList<Relation> GetAll() =>
        Db.Queryable<RelationRow>().ToList().Select(r => r.ToDomain()).ToList();

    public void UpsertRange(IEnumerable<Relation> relations)
    {
        foreach (var relation in relations)
        {
            if (relation.Id > 0)
            {
                Db.Updateable(relation.ToRow()).ExecuteCommand();
            }
            else
            {
                relation.Id = Db.Insertable(relation.ToRow()).ExecuteReturnIdentity();
            }
        }
    }
}

public sealed class EventRepository(IDbSession session) : IEventRepository
{
    private ISqlSugarClient Db => session.Client;

    public long Insert(GameEvent e) =>
        Db.Insertable(e.ToRow()).ExecuteReturnIdentity();

    public void Update(GameEvent e) =>
        Db.Updateable(e.ToRow()).ExecuteCommand();

    public void InsertRange(IEnumerable<GameEvent> events) =>
        Db.Insertable(events.Select(e => e.ToRow()).ToList()).ExecuteCommand();

    public GameEvent? Get(long id) =>
        Db.Queryable<EventRow>().First(e => e.Id == id)?.ToDomain();

    public IReadOnlyList<GameEvent> GetByTick(int tick) =>        Db.Queryable<EventRow>().Where(e => e.Tick == tick).OrderBy(e => e.Id).ToList()
            .Select(e => e.ToDomain()).ToList();

    public IReadOnlyList<GameEvent> GetRecent(int count) =>
        Db.Queryable<EventRow>().OrderBy(e => e.Id, OrderByType.Desc).Take(count).ToList()
            .Select(e => e.ToDomain()).ToList();
}

public sealed class TensionRepository(IDbSession session) : ITensionRepository
{
    private ISqlSugarClient Db => session.Client;

    public IReadOnlyList<Tension> GetAll() =>
        Db.Queryable<TensionRow>().ToList().Select(r => r.ToDomain()).ToList();

    public void UpsertRange(IEnumerable<Tension> tensions)
    {
        foreach (var tension in tensions)
        {
            if (tension.Id > 0)
            {
                Db.Updateable(tension.ToRow()).ExecuteCommand();
            }
            else
            {
                tension.Id = Db.Insertable(tension.ToRow()).ExecuteReturnIdentity();
            }
        }
    }
}

public sealed class NpcTurnRepository(IDbSession session) : INpcTurnRepository
{
    public void InsertRange(IEnumerable<NpcTurn> turns) =>
        session.Client.Insertable(turns.Select(t => t.ToRow()).ToList()).ExecuteCommand();
}
