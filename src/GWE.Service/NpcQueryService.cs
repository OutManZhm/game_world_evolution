using GWE.Domain.Entities;
using GWE.Repository.Repositories;
using GWE.Service.Contracts;
using GWE.Service.Turn;

namespace GWE.Service;

public interface INpcQueryService
{
    WorldClockDto GetClock();
    IReadOnlyList<NpcStatusDto> GetMainNpcs();
    IReadOnlyList<EventDto> GetRecentEvents(int count);
    IReadOnlyList<TensionDto> GetTensions();
    IReadOnlyList<FactionDto> GetFactions();
    NpcProfileDto GetProfile(long npcId);
}

/// <summary>界面只读投影：人物状态、世界时钟、事件流、张力局势、人物档案（记忆/信念）。</summary>
public sealed class NpcQueryService(
    IWorldRepository worldRepository,
    INpcRepository npcRepository,
    IFactionRepository factionRepository,
    ILocaleRepository localeRepository,
    ISiteRepository siteRepository,
    IEventRepository eventRepository,
    ITensionRepository tensionRepository,
    IMemoryRepository memoryRepository,
    IBeliefRepository beliefRepository) : INpcQueryService
{
    public WorldClockDto GetClock()
    {
        var world = worldRepository.Get();
        return new WorldClockDto { Tick = world.Tick, Year = world.Year, Month = world.Month };
    }

    public IReadOnlyList<NpcStatusDto> GetMainNpcs()
    {
        var world = worldRepository.Get();
        var factions = factionRepository.GetAll().ToDictionary(f => f.Id, f => f.Name);
        var localeNames = localeRepository.GetAll().ToDictionary(l => l.Id, l => l.Name);
        var sites = siteRepository.GetAll().ToDictionary(s => s.Id);

        var lastActions = eventRepository.GetRecent(900)
            .OrderByDescending(e => e.Id)
            .GroupBy(e => e.ActorIds)
            .ToDictionary(g => g.Key, g => g.First().Summary);

        return npcRepository.GetMain()
            .OrderByDescending(n => n.Power)
            .Select(n =>
            {
                var site = sites.GetValueOrDefault(n.SiteId);
                return new NpcStatusDto
                {
                    Id = n.Id,
                    Name = n.Name,
                    Faction = factions.GetValueOrDefault(n.FactionId, "无"),
                    Role = n.Role,
                    Location = localeNames.GetValueOrDefault(n.LocationId, "未知"),
                    Site = site?.Name ?? "街头",
                    Facilities = FacilityText(site?.Facilities ?? string.Empty, site?.Specialty ?? string.Empty),
                    Path = n.Path,
                    RankTitle = n.RankTitle,
                    Age = n.Age(world.Tick),
                    Power = n.Power,
                    ArcanaRank = n.ArcanaRank,
                    Cultivation = n.Cultivation,
                    AdvanceThreshold = n.AdvanceThreshold,
                    HasReagent = Turn.Inventory.Has(n, "进阶秘材"),
                    Constitution = n.Constitution,
                    Comprehension = n.Comprehension,
                    Charm = n.Charm,
                    Physique = n.Physique,
                    Willpower = n.Willpower,
                    Health = n.Health,
                    Mood = n.Mood,
                    Money = n.Money,
                    ApBudget = n.ApBudget,
                    LastAction = lastActions.GetValueOrDefault(n.Id.ToString(), "—")
                };
            })
            .ToList();
    }

    public IReadOnlyList<EventDto> GetRecentEvents(int count)
    {
        var world = worldRepository.Get();
        var npcs = npcRepository.GetAll();
        return eventRepository.GetRecent(count)
            .OrderBy(e => e.Id)
            .Select(e => TurnService.ToDto(e, world, npcs))
            .ToList();
    }

    public IReadOnlyList<TensionDto> GetTensions() =>
        tensionRepository.GetAll()
            .OrderByDescending(t => t.Urgency)
            .Select(t => new TensionDto
            {
                Id = t.Id,
                Type = t.Type,
                Stakes = t.Stakes,
                Urgency = t.Urgency,
                DeadlineTick = t.DeadlineTick,
                State = t.State
            })
            .ToList();

    /// <summary>组织台账：库存/上限/累计发放、成员数、还在排队等发放的人数。</summary>
    public IReadOnlyList<FactionDto> GetFactions()
    {
        var npcs = npcRepository.GetAll();
        return factionRepository.GetAll()
            .OrderByDescending(f => f.ReagentCap)
            .Select(f => new FactionDto
            {
                Id = f.Id,
                Name = f.Name,
                Kind = f.Kind,
                Prestige = f.Prestige,
                Treasury = f.Treasury,
                ReagentStock = f.ReagentStock,
                ReagentCap = f.ReagentCap,
                ReagentIssued = f.ReagentIssued,
                SoftCap = FactionEconomy.SoftCap(f),
                Members = npcs.Count(n => n.FactionId == f.Id),
                Awaiting = npcs.Count(n => FactionEconomy.Eligible(n, f))
            })
            .ToList();
    }

    /// <summary>
    /// 人物档案：记忆权重与信念置信度按“距今年月”在读取时衰减，
    /// 既满足“会淡忘”的设计，也不用每回合全表更新。
    /// </summary>
    public NpcProfileDto GetProfile(long npcId)
    {
        var world = worldRepository.Get();
        var npc = npcRepository.GetAll().FirstOrDefault(n => n.Id == npcId);
        if (npc is null)
        {
            return new NpcProfileDto { Id = npcId };
        }

        var locale = localeRepository.GetAll().FirstOrDefault(l => l.Id == npc.LocationId);
        var memories = memoryRepository.GetByNpc(npcId)
            .OrderByDescending(m => m.EmotionalWeight * Math.Pow(0.93, world.Tick - m.Tick))
            .Take(12)
            .Select(m => new MemoryDto
            {
                Tick = $"第 {m.Tick} 回合",
                Kind = m.Kind,
                Summary = m.Summary,
                Weight = (int)Math.Round(m.EmotionalWeight * Math.Pow(0.93, world.Tick - m.Tick))
            })
            .ToList();

        var beliefs = beliefRepository.GetByNpc(npcId)
            .OrderByDescending(b => b.Confidence - (world.Tick - b.Tick) * 2)
            .Take(12)
            .Select(b => new BeliefDto
            {
                Tick = $"第 {b.Tick} 回合",
                Content = b.Content,
                Confidence = Math.Max(10, b.Confidence - (world.Tick - b.Tick) * 2),
                Source = b.Source
            })
            .ToList();

        return new NpcProfileDto
        {
            Id = npc.Id,
            Name = npc.Name,
            Title = npc.RankTitle,
            Location = locale?.Name ?? "未知",
            Memories = memories,
            Beliefs = beliefs
        };
    }

    private static string FacilityText(string facilities, string specialty)
    {
        if (string.IsNullOrWhiteSpace(facilities))
        {
            return "无";
        }
        var names = new Dictionary<string, string>
        {
            [Facility.Ritual] = "仪式",
            [Facility.Workshop] = "工坊",
            [Facility.Market] = "市场",
            [Facility.Library] = "书库",
            [Facility.Temple] = "神殿",
            [Facility.Tavern] = "酒馆"
        };
        var text = string.Join("/", facilities.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(f => names.GetValueOrDefault(f, f)));
        return string.IsNullOrWhiteSpace(specialty) ? text : $"{text}（{specialty}）";
    }
}
