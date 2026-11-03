using System.Text.Json;
using GWE.Domain;
using GWE.Domain.Entities;

namespace GWE.Service.Turn;

/// <summary>
/// 组织经济与秘材流通：
/// 1. 收支三条腿——会费（按身份抽成）、声望拨款（外部世界注资）、秘材贸易差价；
/// 2. 秘材两级市场——行会/帮派以批发价进货，组织以零售价拿货，散人以黑市价买；
/// 3. 资金 sink——发放、赏金、代付仪式费、采购，以及金库越过软上限后的成员分红。
///
/// 这条链保证“不消耗资源就能进阶”不可能发生，也保证钱不会在组织账上烂掉：
/// 组织有钱 → 从商会买料 → 发给够格成员 → 成员进阶消耗 → 商会赚差价 → 分红流回个人。
/// </summary>
public static class FactionEconomy
{
    public const string Reagent = "进阶秘材";

    /// <summary>商会从外地商队的批发价：渠道成本，比零售低一截。</summary>
    public const int WholesaleCost = 30;
    /// <summary>商会卖给组织的零售价。</summary>
    public const int WholesalePrice = 90;
    /// <summary>商会也断货时，组织只能等外地商队，价格更贵（含运费与风险）。</summary>
    public const int RemotePrice = 110;

    /// <summary>散人在黑市柜台买料的价钱（随当地物价指数浮动）。</summary>
    public static int RetailPrice(double priceIndex) => (int)Math.Round(40 * priceIndex);

    /// <summary>每回合每个组织最多发放几件：上限的四分之一（至少 1），避免一轮被扫空。</summary>
    public static int IssueQuota(Faction faction) => Math.Max(1, faction.ReagentCap / 4);

    /// <summary>排队顺序：权位 40% + 学术 30% + 位阶 30%（按贡献分近似）。</summary>
    public static int Contribution(Npc npc) =>
        npc.Authority * 2 + npc.ArcanaRank * 14 + npc.CurrentRank * 10 + npc.Willpower / 10;

    /// <summary>
    /// 会费：平民 4 文；修习者按身份加价，位阶与学术等级越高交得越多。
    /// 只抽手头现金的三分之一，交不起不至于被逼去露宿。
    /// </summary>
    public static int Dues(Npc npc) =>
        4 + (npc.Path != "none" ? 6 : 0) + npc.CurrentRank * 2 + npc.ArcanaRank * 2;

    /// <summary>声望拨款：教会的什一税、学派的赞助、贵族的地租、行会的摊位费、帮派的保护费。</summary>
    public static int Patronage(Faction faction) => faction.Prestige * 3;

    /// <summary>金库软上限：越过这条线，多出来的钱一半按人头分红，避免资金在账上烂掉。</summary>
    public static int SoftCap(Faction faction) => faction.Prestige * 40;

    /// <summary>够不够格领组织秘材：登记在册、有成长路径、进度达标、手上还没有。</summary>
    public static bool Eligible(Npc npc) =>
        npc.Path != "none"
        && NpcActionPlanner.CanAdvance(npc)
        && npc.Cultivation >= npc.AdvanceThreshold
        && !Inventory.Has(npc, Reagent);

    /// <summary>拿得到：要么自己掏得起仪式费，要么组织金库能代付。</summary>
    public static bool Affordable(Npc npc, Faction faction)
    {
        var cost = NpcActionPlanner.AdvanceCost(npc);
        return npc.Money >= cost || faction.Treasury >= cost * 2;
    }

    public static bool Eligible(Npc npc, Faction faction) =>
        npc.FactionId == faction.Id && Eligible(npc) && Affordable(npc, faction);

    // ── 收入 ────────────────────────────────────────────────────────────

    public sealed record IncomeLine(Faction Faction, int Dues, int Patronage, int Members);

    /// <summary>月初收会费 + 声望拨款。会费从成员现金里真扣，不是印钱。</summary>
    public static IncomeLine CollectIncome(Faction faction, IReadOnlyList<Npc> npcs)
    {
        var members = npcs.Where(n => n.FactionId == faction.Id).ToList();
        var collected = 0;
        foreach (var npc in members)
        {
            var due = Math.Min(Dues(npc), npc.Money / 3);
            if (due <= 0)
            {
                continue;
            }
            npc.Money -= due;
            collected += due;
        }

        var patronage = Patronage(faction);
        faction.Treasury += collected + patronage;
        return new IncomeLine(faction, collected, patronage, members.Count);
    }

    // ── 秘材两级市场 ────────────────────────────────────────────────────

    public sealed record RestockLine(Faction Merchant, int Bought, int Cost, int StockLeft);

    /// <summary>
    /// 商会补货：从外地商队按批发价吃货，补到上限的四分之三，每回合最多 4 件。
    /// 这是“钱 → 料”的入口，也是商会金库的主要出口。
    /// </summary>
    public static List<RestockLine> Restock(IReadOnlyList<Faction> factions)
    {
        var lines = new List<RestockLine>();
        foreach (var merchant in factions.Where(f => f.IsMerchant))
        {
            var target = merchant.ReagentCap * 3 / 4;
            var need = target - merchant.ReagentStock;
            if (need <= 0)
            {
                continue;
            }
            var affordable = merchant.Treasury / WholesaleCost;
            var buy = Math.Min(Math.Min(need, affordable), 4);
            if (buy <= 0)
            {
                continue;
            }
            merchant.Treasury -= buy * WholesaleCost;
            merchant.ReagentStock += buy;
            lines.Add(new RestockLine(merchant, buy, buy * WholesaleCost, merchant.ReagentStock));
        }
        return lines;
    }

    /// <summary>有货的商会：按库存从多到少排，优先吃大户。</summary>
    public static Faction? MerchantWithStock(IEnumerable<Faction> factions) =>
        factions.Where(f => f.IsMerchant && f.ReagentStock > 0)
            .OrderByDescending(f => f.ReagentStock)
            .FirstOrDefault();

    public sealed record Procurement(Faction Buyer, int FromMerchant, int FromRemote, Faction? Merchant, int Paid, int StockLeft);

    /// <summary>
    /// 缺料组织采购：先从商会拿货（90/件，钱进商会金库），商会也空了才走外地商队（110/件）。
    /// 补到接近满仓、每回合最多 4 件——留出缓冲，才不会一边发一边见底。
    /// </summary>
    public static List<Procurement> Procure(IReadOnlyList<Faction> factions)
    {
        var lines = new List<Procurement>();
        var merchants = factions.Where(f => f.IsMerchant).ToList();
        foreach (var buyer in factions.Where(f => !f.IsMerchant))
        {
            var target = Math.Max(2, buyer.ReagentCap - 1);
            if (buyer.ReagentStock >= target)
            {
                continue;
            }

            var want = Math.Min(Math.Min(4, target - buyer.ReagentStock), buyer.ReagentCap - buyer.ReagentStock);
            var budget = buyer.Treasury;
            var fromMerchant = 0;
            var fromRemote = 0;
            Faction? supplier = null;

            foreach (var merchant in merchants.Where(m => m.ReagentStock > 0)
                         .OrderByDescending(m => m.ReagentStock))
            {
                if (fromMerchant >= want)
                {
                    break;
                }
                var take = Math.Min(Math.Min(want - fromMerchant, merchant.ReagentStock), budget / WholesalePrice);
                if (take <= 0)
                {
                    continue;
                }
                merchant.ReagentStock -= take;
                merchant.Treasury += take * WholesalePrice;
                fromMerchant += take;
                budget -= take * WholesalePrice;
                supplier ??= merchant;
            }

            if (fromMerchant < want)
            {
                var take = Math.Min(want - fromMerchant, budget / RemotePrice);
                if (take > 0)
                {
                    fromRemote = take;
                }
            }

            var total = fromMerchant + fromRemote;
            if (total <= 0)
            {
                continue;
            }

            var paid = fromMerchant * WholesalePrice + fromRemote * RemotePrice;
            buyer.Treasury -= paid;
            buyer.ReagentStock += total;
            lines.Add(new Procurement(buyer, fromMerchant, fromRemote, supplier, paid, buyer.ReagentStock));
        }
        return lines;
    }

    // ── 发放 ────────────────────────────────────────────────────────────

    /// <summary>
    /// 组织发放：把库存发给贡献最高的若干合格成员；成员自己掏不起仪式费时由金库代付。
    /// 金库是钱从组织走到个人，不是凭空生成。
    /// </summary>
    public static List<(Npc Npc, Faction Faction, int StockLeft, bool Sponsored)> Distribute(
        IReadOnlyList<Npc> npcs,
        IReadOnlyList<Faction> factions)
    {
        var grants = new List<(Npc, Faction, int, bool)>();
        foreach (var faction in factions.Where(f => !f.IsMerchant).OrderBy(f => f.Id))
        {
            var quota = Math.Min(IssueQuota(faction), faction.ReagentStock);
            if (quota <= 0)
            {
                continue;
            }

            var queue = npcs
                .Where(n => Eligible(n, faction))
                .OrderByDescending(Contribution)
                .ThenByDescending(n => n.Cultivation)
                .Take(quota)
                .ToList();

            foreach (var npc in queue)
            {
                faction.ReagentStock -= 1;
                faction.ReagentIssued += 1;
                Inventory.Add(npc, Reagent);

                // 代付仪式费：组织出钱，避免“有秘材却永远凑不出仪式费”的死结
                var cost = NpcActionPlanner.AdvanceCost(npc);
                var sponsored = false;
                if (npc.Money < cost && faction.Treasury >= cost * 2)
                {
                    faction.Treasury -= cost;
                    npc.Money += cost;
                    sponsored = true;
                }

                grants.Add((npc, faction, faction.ReagentStock, sponsored));
            }
        }
        return grants;
    }

    /// <summary>发放事件的载荷（组织、库存、贡献分、是否由组织代付仪式费）。</summary>
    public static object GrantFacts(Npc npc, Faction faction, int stockLeft, bool sponsored) => new
    {
        action = "reagent_grant",
        factionId = faction.Id,
        faction = faction.Name,
        contribution = Contribution(npc),
        stockLeft,
        cap = faction.ReagentCap,
        sponsored
    };

    // ── 分红 ────────────────────────────────────────────────────────────

    public sealed record DividendPayout(Faction Faction, int Pool, int Members, int Share);

    /// <summary>
    /// 金库分红：越过软上限的部分全部按人头分给成员。
    /// 既是防通胀阀门，也让“组织有钱”这件事真的落到人身上（平民也会因此多几文本钱）。
    /// </summary>
    public static DividendPayout? Dividend(Faction faction, IReadOnlyList<Npc> npcs)
    {
        var cap = SoftCap(faction);
        if (faction.Treasury <= cap)
        {
            return null;
        }
        var members = npcs.Where(n => n.FactionId == faction.Id).ToList();
        if (members.Count == 0)
        {
            return null;
        }

        var pool = faction.Treasury - cap;
        var share = pool / members.Count;
        if (share < 3)
        {
            return null;
        }

        var paid = share * members.Count;
        faction.Treasury -= paid;
        foreach (var member in members)
        {
            member.Money += share;
        }
        return new DividendPayout(faction, paid, members.Count, share);
    }

    // ── 取材上交 ────────────────────────────────────────────────────────

    /// <summary>
    /// 是否符合“该上交”的情形：组织缺料 + 自己还没到进阶门槛（到了就留着自己用）。
    /// 有组织的正式成员才上交；野路子自己留着。
    /// </summary>
    public static bool ShouldTurnIn(Npc npc, Faction faction) =>
        faction.ReagentStock < faction.ReagentCap
        && Inventory.Has(npc, Reagent)
        && npc.Cultivation < npc.AdvanceThreshold;

    /// <summary>
    /// 取材上交：秘材充公进库存，成员拿赏金与功勋（金库不够时只给功勋）。
    /// </summary>
    public static GameEvent? TurnIn(Npc npc, Faction faction, int tick, double price, Random random)
    {
        if (!ShouldTurnIn(npc, faction))
        {
            return null;
        }

        Inventory.Remove(npc, Reagent);
        faction.ReagentStock += 1;
        var bounty = Math.Min(faction.Treasury, (int)Math.Round(45 * price) + random.Next(0, 10));
        faction.Treasury -= bounty;
        npc.Money += bounty;
        npc.Authority += 1;
        npc.Mood += 3;

        return new GameEvent
        {
            Tick = tick,
            Type = "ReagentTurnIn",
            Layer = EventLayer.NpcEmergent,
            Status = EventStatus.Resolved,
            ActorIds = npc.Id.ToString(),
            LocationId = npc.LocationId,
            FactsJson = JsonSerializer.Serialize(new
            {
                action = "reagent_turnin",
                factionId = faction.Id,
                bounty,
                stock = faction.ReagentStock,
                cap = faction.ReagentCap
            }),
            Visibility = EventVisibility.Witness,
            Salience = 40,
            Tags = "委托取材",
            Summary = bounty > 0
                ? $"{npc.Name} 把寻得的【进阶秘材】交给{faction.Name}，领赏 {bounty} 文，库存回到 {faction.ReagentStock}/{faction.ReagentCap}。"
                : $"{npc.Name} 把寻得的【进阶秘材】交给{faction.Name}，金库空虚，只记了一笔功勋（库存 {faction.ReagentStock}/{faction.ReagentCap}）。"
        };
    }
}
