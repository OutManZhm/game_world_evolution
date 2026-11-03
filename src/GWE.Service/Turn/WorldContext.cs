using GWE.Domain.Entities;

namespace GWE.Service.Turn;

/// <summary>
/// 一回合内的世界索引：地点、现场与设施查询。
/// 规划器和结算器都从这里取“他站在哪儿、这里能干什么”，避免把空间逻辑散落在各处。
/// </summary>
public sealed class WorldContext
{
    private readonly Dictionary<long, Site> _sites;
    private readonly Dictionary<long, Locale> _locales;
    private readonly Dictionary<long, List<Site>> _sitesByLocale;
    private readonly Dictionary<long, Faction> _factions;

    public WorldContext(IEnumerable<Locale> locales, IEnumerable<Site> sites, IEnumerable<Faction>? factions = null)
    {
        _sites = sites.ToDictionary(s => s.Id);
        _locales = locales.ToDictionary(l => l.Id);
        _sitesByLocale = _sites.Values.GroupBy(s => s.LocaleId)
            .ToDictionary(g => g.Key, g => g.ToList());
        _factions = (factions ?? []).ToDictionary(f => f.Id);
    }

    /// <summary>所属组织（无组织返回 null）。</summary>
    public Faction? FactionOf(Npc npc) =>
        npc.FactionId > 0 ? _factions.GetValueOrDefault(npc.FactionId) : null;

    /// <summary>全城商会：散人买料的唯一正规渠道（都没货就只能等外地商队）。</summary>
    public Faction? ReagentSupplier() => FactionEconomy.MerchantWithStock(_factions.Values);

    /// <summary>所属组织当前可用的进阶秘材库存（无组织 = 0，只能去黑市或自己找）。</summary>
    public int ReagentStock(Npc npc) => FactionOf(npc)?.ReagentStock ?? 0;

    /// <summary>组织是否在派人取材（库存告急或空了）。</summary>
    public bool FactionNeedsReagent(Npc npc) => FactionOf(npc)?.ReagentLow ?? false;

    public Site? SiteOf(Npc npc) =>
        npc.SiteId > 0 && _sites.TryGetValue(npc.SiteId, out var site) ? site : null;

    public Locale? LocaleOf(Npc npc) =>
        _locales.GetValueOrDefault(npc.LocationId);

    public Site? SiteById(long siteId) => _sites.GetValueOrDefault(siteId);

    public bool HasFacility(Npc npc, string facility) => SiteOf(npc)?.Has(facility) ?? false;

    public IReadOnlyList<Site> SitesIn(long localeId) =>
        _sitesByLocale.GetValueOrDefault(localeId) ?? [];

    /// <summary>该地点里第一个带指定设施的现场。</summary>
    public Site? FindFacility(long localeId, string facility) =>
        SitesIn(localeId).FirstOrDefault(s => s.Has(facility));

    /// <summary>某地点是否具备指定设施（用于迁移目标筛选）。</summary>
    public bool LocaleHasFacility(long localeId, string facility) => FindFacility(localeId, facility) is not null;

    public IReadOnlyList<Locale> Locales => _locales.Values.ToList();
}
