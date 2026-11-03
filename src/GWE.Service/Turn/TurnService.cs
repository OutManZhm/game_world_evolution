using System.Text.Json;
using GWE.Domain;
using GWE.Domain.Entities;
using GWE.Repository;
using GWE.Repository.Repositories;
using GWE.Service.Contracts;

namespace GWE.Service.Turn;

public interface ITurnService
{
    TurnResultDto Advance();
}

/// <summary>
/// 回合引擎：tick+1（月）→ 定时事件（L0/L2）→ 全体 NPC 月度生计 → 按 AP 选择并结算行为
/// → 张力推进 → 因果/记忆/信念落库 → 单事务提交。
/// 进阶仪式强制消耗金钱与进阶秘材，且必须站在仪式厅（红线 Q12 / N05）；生计开销构成最小生存闭环（Q07/E01）。
/// </summary>
public sealed class TurnService(
    IDbSession session,
    IWorldRepository worldRepository,
    INpcRepository npcRepository,
    IRelationRepository relationRepository,
    IEventRepository eventRepository,
    ILocaleRepository localeRepository,
    ISiteRepository siteRepository,
    INpcTurnRepository npcTurnRepository,
    ITensionRepository tensionRepository,
    IEventScheduleRepository scheduleRepository,
    IMemoryRepository memoryRepository,
    IBeliefRepository beliefRepository,
    IKnowledgeDeliveryRepository deliveryRepository,
    IFactionRepository factionRepository) : ITurnService
{
    /// <summary>事件 + 对同批次内父事件的索引（-1 = 无父），用于插入后回填因果字段。</summary>
    private sealed record PendingEvent(GameEvent Event, int ParentIndex);

    /// <summary>定时事件效果载荷。</summary>
    private sealed record SchedulePayload(
        int? Mood, int? Money, int? Health, string? Tension, int? Urgency, long? LocaleId);

    private static readonly JsonSerializerOptions PayloadOptions = new() { PropertyNameCaseInsensitive = true };

    public TurnResultDto Advance()
    {
        var world = worldRepository.Get();
        var npcs = npcRepository.GetAll().ToList();
        var relations = relationRepository.GetAll().ToList();
        var locales = localeRepository.GetAll();
        var sites = siteRepository.GetAll();
        var tensions = tensionRepository.GetAll().ToList();
        var schedules = scheduleRepository.GetAll().ToList();
        var factions = factionRepository.GetAll().ToList();
        var context = new WorldContext(locales, sites, factions);
        var beliefs = beliefRepository.GetAll().ToList();
        var memories = new List<Memory>();
        var newBeliefs = new List<Belief>();
        var deliveries = new List<KnowledgeDelivery>();
        var pending = new List<PendingEvent>();
        var scheduleUpdates = new List<GameEvent>();
        var plannedIndex = new Dictionary<long, int>();
        var turns = new List<NpcTurn>(npcs.Count);
        var random = new Random(unchecked(world.Seed * 31 + world.Tick + 1));

        world.Tick += 1;
        world.Month += 1;
        if (world.Month > 12)
        {
            world.Month = 1;
            world.Year += 1;
        }

        // L0/L2：到期的转为已发生，下一回合到期的先以 Planned 入日志
        ResolveSchedules(world, schedules, npcs, tensions, pending, scheduleUpdates);
        PlanSchedules(world, schedules, pending, plannedIndex);

        // 组织收支四条腿：收会费与拨款 → 商会补货 → 缺料组织采购 → 发放 → 金库溢出分红
        foreach (var faction in factions.OrderBy(f => f.Id))
        {
            FactionEconomy.CollectIncome(faction, npcs);
        }

        // 商会按批发价从外地商队补货：全城秘材的总闸门在这里，不是凭空刷出来的
        foreach (var restock in FactionEconomy.Restock(factions))
        {
            var owner = TopMember(npcs, restock.Merchant);
            Add(pending, OrgEvent(world.Tick, "ReagentRestock", "商会进货", 30,
                $"{restock.Merchant.Name} 从外地商队批发 {restock.Bought} 件【进阶秘材】（耗 {restock.Cost} 文，库存 {restock.StockLeft}/{restock.Merchant.ReagentCap}）。",
                new { action = "reagent_restock", factionId = restock.Merchant.Id, buy = restock.Bought, cost = restock.Cost, stock = restock.StockLeft },
                owner));
        }

        // 缺料组织向商会拿货（商会赚差价），商会也空了才加价等外地商队
        foreach (var procurement in FactionEconomy.Procure(factions))
        {
            var via = procurement.FromMerchant > 0
                ? $"从{procurement.Merchant!.Name}购入 {procurement.FromMerchant} 件"
                : "等外地商队送来";
            if (procurement.FromRemote > 0)
            {
                via += procurement.FromMerchant > 0 ? $"、另从外地商队补 {procurement.FromRemote} 件" : $" {procurement.FromRemote} 件";
            }
            Add(pending, OrgEvent(world.Tick, "ReagentPurchase", "组织采购", 35,
                $"{procurement.Buyer.Name} 库存偏低，{via}【进阶秘材】，共耗 {procurement.Paid} 文（库存 {procurement.StockLeft}/{procurement.Buyer.ReagentCap}）。",
                new
                {
                    action = "reagent_purchase",
                    factionId = procurement.Buyer.Id,
                    fromMerchant = procurement.FromMerchant,
                    fromRemote = procurement.FromRemote,
                    supplierId = procurement.Merchant?.Id ?? 0,
                    paid = procurement.Paid,
                    stock = procurement.StockLeft
                },
                TopMember(npcs, procurement.Buyer)));
        }

        // 组织发放：库存有限，按贡献发给够格的成员（拿不到的人只能自己找或去黑市）
        foreach (var (grantee, faction, stockLeft, sponsored) in FactionEconomy.Distribute(npcs, factions))
        {
            Add(pending, Event(grantee, world.Tick, "ReagentGrant", "组织发放", EventVisibility.Witness, 55,
                sponsored
                    ? $"{faction.Name} 向 {grantee.Name} 发放【进阶秘材】并代付仪式费（库存余 {stockLeft}/{faction.ReagentCap}）。"
                    : $"{faction.Name} 向 {grantee.Name} 发放【进阶秘材】（库存余 {stockLeft}/{faction.ReagentCap}）。",
                FactionEconomy.GrantFacts(grantee, faction, stockLeft, sponsored)));
        }

        // 金库越过软上限：多出来的一半按人头分红，钱不在账上烂掉
        foreach (var faction in factions.OrderBy(f => f.Id))
        {
            var payout = FactionEconomy.Dividend(faction, npcs);
            if (payout is null)
            {
                continue;
            }
            Add(pending, OrgEvent(world.Tick, "FactionDividend", "组织分红", 25,
                $"{payout.Faction.Name} 金库越过 {FactionEconomy.SoftCap(payout.Faction)} 文软上限，向 {payout.Members} 名成员每人分红 {payout.Share} 文（共 {payout.Pool} 文）。",
                new { action = "faction_dividend", factionId = payout.Faction.Id, pool = payout.Pool, members = payout.Members, share = payout.Share },
                TopMember(npcs, payout.Faction)));
        }

        foreach (var npc in npcs)
        {
            // 月度生计：食物与住所开销（E01/Q07）。付不起 → 健康与心情下降。
            var upkeep = npc.Authority >= 20 || npc.Path != "none" ? 10 : 4;
            if (npc.Money >= upkeep)
            {
                npc.Money -= upkeep;
            }
            else
            {
                npc.Health -= 6;
                npc.Mood -= 10;
                Add(pending, Event(npc, world.Tick, "UpkeepFailed", "生计", EventVisibility.Private, 40,
                    $"{npc.Name} 付不起本月食宿，忍饥露宿，健康 -6。",
                    new { action = "upkeep_failed", upkeep }));
            }

            var actions = new List<string>();
            var ap = npc.ApBudget;
            while (ap > 0)
            {
                var action = NpcActionPlanner.Choose(npc, random, context);
                Resolve(npc, action, npcs, relations, context, pending, random, world.Tick);
                actions.Add(ActionText.Of(action));
                ap -= action == NpcActionType.Advance ? 2 : 1;
            }
            Clamp(npc);

            turns.Add(new NpcTurn
            {
                NpcId = npc.Id,
                Tick = world.Tick,
                ApBudget = npc.ApBudget,
                ApSpent = npc.ApBudget - Math.Max(0, ap),
                ActionsJson = JsonSerializer.Serialize(actions)
            });
        }

        // 世界压力：未解决的张力每回合累积紧迫度，到期或爆表时升级为 L1 事件（设计文档 8.3）
        AdvanceTensions(world, tensions, pending, random);

        var events = Commit(
            pending, npcs, relations, tensions, factions, schedules, plannedIndex, turns, world,
            beliefs, memories, newBeliefs, deliveries, scheduleUpdates, random);

        return new TurnResultDto
        {
            Tick = world.Tick,
            Year = world.Year,
            Month = world.Month,
            Events = events.Select(e => ToDto(e, world, npcs)).ToList()
        };
    }

    /// <summary>单事务落库：插事件 → 回填因果 → 记忆/信念/传递 → 状态与台账 → 定时事件状态。</summary>
    private List<GameEvent> Commit(
        List<PendingEvent> pending,
        List<Npc> npcs,
        List<Relation> relations,
        List<Tension> tensions,
        List<Faction> factions,
        List<EventSchedule> schedules,
        Dictionary<long, int> plannedIndex,
        List<NpcTurn> turns,
        WorldState world,
        List<Belief> beliefs,
        List<Memory> memories,
        List<Belief> newBeliefs,
        List<KnowledgeDelivery> deliveries,
        List<GameEvent> scheduleUpdates,
        Random random)
    {
        var committed = new List<GameEvent>();
        session.ExecuteInTransaction(() =>
        {
            // 先插父事件（探索），再插子事件（遭遇），用返回的 id 回填 root/parent/depth。
            for (var i = 0; i < pending.Count; i++)
            {
                pending[i].Event.Id = eventRepository.Insert(pending[i].Event);
            }
            for (var i = 0; i < pending.Count; i++)
            {
                var (e, parentIndex) = pending[i];
                if (parentIndex >= 0)
                {
                    var parent = pending[parentIndex].Event;
                    e.ParentId = parent.Id;
                    e.RootId = parent.RootId ?? parent.Id;
                    e.Depth = parent.Depth + 1;
                    eventRepository.Update(e);
                }
            }
            committed.AddRange(pending.Select(p => p.Event));

            // 计划事件的 id 回写到排程，供到期时改状态
            foreach (var schedule in schedules.Where(s => s.State == "planned" && s.EventId == 0))
            {
                if (plannedIndex.TryGetValue(schedule.Id, out var index))
                {
                    schedule.EventId = pending[index].Event.Id;
                }
            }

            CollectKnowledge(committed, npcs, beliefs, memories, newBeliefs, deliveries, random);

            npcRepository.UpdateRange(npcs);
            relationRepository.UpsertRange(relations);
            tensionRepository.UpsertRange(tensions);
            factionRepository.UpdateRange(factions);
            scheduleRepository.UpsertRange(schedules);
            npcTurnRepository.InsertRange(turns);

            memoryRepository.InsertRange(memories);
            beliefRepository.InsertRange(newBeliefs);
            deliveryRepository.InsertRange(deliveries);
            foreach (var e in scheduleUpdates)
            {
                eventRepository.Update(e);
            }

            worldRepository.Save(world);
        });
        return committed;
    }

    /// <summary>
    /// 认知落库：显著性足够的事件给当事人留记忆、给同地目击者立信念，
    /// 社交时把已知信息传给对方（置信度衰减），形成“事实 → 传闻”的传播链。
    /// </summary>
    private static void CollectKnowledge(
        List<GameEvent> events,
        List<Npc> npcs,
        List<Belief> existing,
        List<Memory> memories,
        List<Belief> newBeliefs,
        List<KnowledgeDelivery> deliveries,
        Random random)
    {
        var byId = npcs.ToDictionary(n => n.Id);
        var known = existing.Select(b => (b.NpcId, b.FactKey))
            .Concat(newBeliefs.Select(b => (b.NpcId, b.FactKey)))
            .ToHashSet();

        var byLocale = npcs.GroupBy(n => n.LocationId).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var e in events)
        {
            if (e.Salience < 60 || !long.TryParse(e.ActorIds, out var actorId))
            {
                continue;
            }
            var actor = byId.GetValueOrDefault(actorId);
            if (actor is null)
            {
                continue;
            }

            // 当事人记忆：情绪权重来自显著性
            memories.Add(new Memory
            {
                NpcId = actor.Id,
                EventId = e.Id,
                Tick = e.Tick,
                Kind = e.Type,
                Summary = e.Summary.Length > 60 ? e.Summary[..60] : e.Summary,
                EmotionalWeight = e.Salience / 2 - (e.Type == "UpkeepFailed" ? 40 : 0)
            });

            var pool = e.Visibility == EventVisibility.Public
                ? npcs.Where(n => n.Id != actor.Id).ToList()
                : byLocale.GetValueOrDefault(e.LocationId, []).Where(n => n.Id != actor.Id).ToList();
            if (pool.Count == 0)
            {
                continue;
            }

            var witnessCount = e.Visibility switch
            {
                EventVisibility.Public => 3,
                EventVisibility.Local => 2,
                _ => 1
            };
            var factKey = $"event:{e.Id}";
            var confidence = e.Visibility switch
            {
                EventVisibility.Public => 92,
                EventVisibility.Local => 78,
                _ => 85
            };
            var channel = e.Visibility switch
            {
                EventVisibility.Public => "公告",
                EventVisibility.Local => "传闻",
                _ => "目击"
            };

            foreach (var witness in pool.OrderBy(_ => random.Next()).Take(witnessCount))
            {
                if (!known.Add((witness.Id, factKey)))
                {
                    continue;
                }
                newBeliefs.Add(new Belief
                {
                    NpcId = witness.Id,
                    FactKey = factKey,
                    Content = e.Summary.Length > 70 ? e.Summary[..70] : e.Summary,
                    Confidence = confidence,
                    Source = channel,
                    Tick = e.Tick
                });
                deliveries.Add(new KnowledgeDelivery
                {
                    EventId = e.Id,
                    FromId = actor.Id,
                    ToId = witness.Id,
                    Channel = channel,
                    Tick = e.Tick
                });
            }
        }

        // 社交传播：把当事人知道的、对方不知道的事说出去，置信度 -12
        foreach (var e in events.Where(x => x.Type == nameof(NpcActionType.Socialize)))
        {
            if (!TryGetTarget(e.FactsJson, out var targetId)
                || !long.TryParse(e.ActorIds, out var speakerId))
            {
                continue;
            }
            var speakable = existing
                .Where(b => b.NpcId == speakerId && b.Confidence > 30)
                .OrderByDescending(b => b.Confidence)
                .Take(3)
                .FirstOrDefault(b => known.Add((targetId, b.FactKey)));
            if (speakable is null)
            {
                continue;
            }
            newBeliefs.Add(new Belief
            {
                NpcId = targetId,
                FactKey = speakable.FactKey,
                Content = speakable.Content,
                Confidence = Math.Max(20, speakable.Confidence - 12),
                Source = "听说",
                Tick = e.Tick
            });
            var eventId = speakable.FactKey.StartsWith("event:") &&
                          long.TryParse(speakable.FactKey[6..], out var parsed) ? parsed : 0;
            deliveries.Add(new KnowledgeDelivery
            {
                EventId = eventId,
                FromId = speakerId,
                ToId = targetId,
                Channel = "口头",
                Tick = e.Tick
            });
        }
    }

    private static bool TryGetTarget(string factsJson, out long targetId)
    {
        targetId = 0;
        try
        {
            using var doc = JsonDocument.Parse(factsJson);
            return doc.RootElement.TryGetProperty("targetId", out var value)
                && long.TryParse(value.ToString(), out targetId);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>L0/L2 到期：把 Planned 事件转为 Resolved，并把载荷效果施加到人/张力上。</summary>
    private void ResolveSchedules(
        WorldState world,
        List<EventSchedule> schedules,
        List<Npc> npcs,
        List<Tension> tensions,
        List<PendingEvent> pending,
        List<GameEvent> updates)
    {
        foreach (var schedule in schedules.Where(s => s.State == "planned" && s.DueTick <= world.Tick))
        {
            var payload = Parse(schedule.PayloadJson);
            var affected = payload.LocaleId is { } localeId
                ? npcs.Where(n => n.LocationId == localeId).ToList()
                : npcs;
            foreach (var npc in affected)
            {
                if (payload.Mood is { } mood)
                {
                    npc.Mood += mood;
                }
                if (payload.Money is { } money)
                {
                    npc.Money += money;
                }
                if (payload.Health is { } health)
                {
                    npc.Health += health;
                }
            }

            var tension = payload.Tension is null
                ? null
                : tensions.FirstOrDefault(t => t.Type == payload.Tension);
            if (tension is not null && payload.Urgency is { } urgency)
            {
                tension.Urgency = Math.Clamp(tension.Urgency + urgency, 0, 100);
            }

            schedule.State = "resolved";
            var existing = schedule.EventId > 0 ? eventRepository.Get(schedule.EventId) : null;
            var detail = Describe(payload, tension?.Type);
            if (existing is not null)
            {
                existing.Status = EventStatus.Resolved;
                existing.Summary = $"{schedule.Name}如期发生：{detail}";
                existing.Salience = Math.Max(existing.Salience, 80);
                updates.Add(existing);
            }
            else
            {
                var e = new GameEvent
                {
                    Tick = world.Tick,
                    Type = schedule.Kind == "Script" ? "ScriptBeat" : "CalendarEvent",
                    Layer = schedule.Kind == "Script" ? EventLayer.Script : EventLayer.Calendar,
                    Status = EventStatus.Resolved,
                    LocationId = payload.LocaleId ?? 0,
                    Salience = 80,
                    Visibility = EventVisibility.Public,
                    Tags = schedule.Name,
                    FactsJson = schedule.PayloadJson,
                    Summary = $"{schedule.Name}如期发生：{detail}"
                };
                pending.Add(new PendingEvent(e, -1));
                schedule.EventId = 0;
            }
        }
    }

    /// <summary>L0/L2 预告：下一回合到期的事件先以 Planned 状态进入日志（事件状态机真正走一遍）。</summary>
    private static void PlanSchedules(
        WorldState world,
        List<EventSchedule> schedules,
        List<PendingEvent> pending,
        Dictionary<long, int> plannedIndex)
    {
        foreach (var schedule in schedules.Where(s => s.State == "pending" && s.DueTick == world.Tick + 1))
        {
            var payload = Parse(schedule.PayloadJson);
            var layer = schedule.Kind == "Script" ? EventLayer.Script : EventLayer.Calendar;
            var detail = Describe(payload, payload.Tension);
            var e = new GameEvent
            {
                Tick = schedule.DueTick,
                Type = schedule.Kind == "Script" ? "ScriptBeat" : "CalendarEvent",
                Layer = layer,
                Status = EventStatus.Planned,
                LocationId = payload.LocaleId ?? 0,
                Salience = 70,
                Visibility = EventVisibility.Local,
                Tags = schedule.Name,
                FactsJson = schedule.PayloadJson,
                Summary = $"预定：{schedule.Name} 将于下月举行/发生（{detail}）。"
            };
            plannedIndex[schedule.Id] = pending.Count;
            pending.Add(new PendingEvent(e, -1));
            schedule.State = "planned";
        }
    }

    private static SchedulePayload Parse(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<SchedulePayload>(json, PayloadOptions) ?? new SchedulePayload(null, null, null, null, null, null);
        }
        catch (JsonException)
        {
            return new SchedulePayload(null, null, null, null, null, null);
        }
    }

    private static string Describe(SchedulePayload p, string? tensionType)
    {
        var parts = new List<string>();
        if (p.Mood is { } mood) parts.Add($"当地心情 {(mood >= 0 ? "+" : "")}{mood}");
        if (p.Money is { } money) parts.Add($"金钱 {(money >= 0 ? "+" : "")}{money}");
        if (p.Health is { } health) parts.Add($"健康 {(health >= 0 ? "+" : "")}{health}");
        if (p.Urgency is { } urgency && tensionType is not null)
            parts.Add($"「{tensionType}」紧迫度 {(urgency >= 0 ? "+" : "")}{urgency}");
        return parts.Count == 0 ? "无事发生" : string.Join("，", parts);
    }

    private static void Add(List<PendingEvent> list, GameEvent e, int parentIndex = -1) =>
        list.Add(new PendingEvent(e, parentIndex));

    /// <summary>
    /// 张力推进：未解决的张力每回合累积紧迫度；紧迫度 ≥90 或期限已过，就升级成 L1 世界压力事件，
    /// 再把紧迫度压回 55、延期 6 回合，形成持续的压力循环（设计文档 8.3 的最小实现）。
    /// </summary>
    private static void AdvanceTensions(
        WorldState world, List<Tension> tensions, List<PendingEvent> pending, Random random)
    {
        foreach (var tension in tensions.Where(t => t.State is "open" or "escalated"))
        {
            tension.Urgency = Math.Min(100, tension.Urgency + 2 + random.Next(0, 4));
            if (tension.Urgency < 90 && world.Tick <= tension.DeadlineTick)
            {
                continue;
            }

            var escalatedUrgency = tension.Urgency;
            tension.State = "escalated";
            tension.Urgency = 55;
            tension.DeadlineTick = world.Tick + 6;

            var actorId = tension.SubjectIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .FirstOrDefault(s => long.TryParse(s, out _)) ?? "0";

            pending.Add(new PendingEvent(new GameEvent
            {
                Tick = world.Tick,
                Type = "TensionEscalation",
                Layer = EventLayer.WorldPressure,
                Status = EventStatus.Resolved,
                ActorIds = actorId,
                LocationId = 0,
                FactsJson = JsonSerializer.Serialize(new
                {
                    action = "tension_escalation",
                    tensionId = tension.Id,
                    tensionType = tension.Type,
                    urgency = escalatedUrgency,
                    stakes = tension.Stakes
                }),
                Visibility = EventVisibility.Local,
                Salience = 85,
                Tags = "张力",
                Summary = $"{tension.Type} 升级：{EscalationText(tension.Type, tension.Stakes)}"
            }, -1));
        }
    }

    private static string EscalationText(string type, string stakes) => type switch
    {
        "隐匿暴露风险" => "裁判所巡查队开进平民区逐户查验登记，未登记者连夜转移。",
        "资料分配不均" => "灰塔分会低阶会员联名请愿公开高阶文献，评审会驳回，双方僵持。",
        "爵位继承争议" => "奥尔登家臣在领主厅公开站队，继承之争摆上台面。",
        "药剂短缺" => "热病沿河湾扩散，药价再涨，平民围堵药铺。",
        _ => stakes
    };

    private static GameEvent Event(
        Npc npc, int tick, string type, string tag, EventVisibility visibility, int salience,
        string summary, object facts) => new()
    {
        Tick = tick,
        Type = type,
        Layer = EventLayer.NpcEmergent,
        Status = EventStatus.Resolved,
        ActorIds = npc.Id.ToString(),
        LocationId = npc.LocationId,
        FactsJson = JsonSerializer.Serialize(facts),
        Visibility = visibility,
        Salience = salience,
        Tags = tag,
        Summary = summary
    };

    /// <summary>
    /// 组织级事件（采购、进货、分红）：没有具体发起人，但挂上该组织的话事人当演员，
    /// 界面上才有人物可查，事件链也接得住。
    /// </summary>
    private static GameEvent OrgEvent(
        int tick, string type, string tag, int salience, string summary, object facts, Npc? actor = null) => new()
    {
        Tick = tick,
        Type = type,
        Layer = EventLayer.NpcEmergent,
        Status = EventStatus.Resolved,
        ActorIds = actor is null ? "" : actor.Id.ToString(),
        LocationId = actor?.LocationId ?? 0,
        FactsJson = JsonSerializer.Serialize(facts),
        Visibility = EventVisibility.Local,
        Salience = salience,
        Tags = tag,
        Summary = summary
    };

    /// <summary>组织里贡献最高的在册成员，用作组织事件的代言人。</summary>
    private static Npc? TopMember(IReadOnlyList<Npc> npcs, Faction faction) =>
        npcs.Where(n => n.FactionId == faction.Id)
            .OrderByDescending(FactionEconomy.Contribution)
            .FirstOrDefault();

    private static void Resolve(
        Npc npc,
        NpcActionType action,
        List<Npc> npcs,
        List<Relation> relations,
        WorldContext context,
        List<PendingEvent> pending,
        Random random,
        int tick)
    {
        var locale = context.LocaleOf(npc);
        var price = (locale?.PriceIndex ?? 100) / 100.0;

        switch (action)
        {
            case NpcActionType.Meditate:
            {
                var text = npc.Path == "mage" ? "冥想修习" : "武技训练";
                var stat = npc.Path == "mage" ? npc.Constitution : npc.Physique;
                // 根骨/体质决定效率，等阶越高越能忍受长修（避免高阶卡在进度条上）
                var gain = 5 + stat / 8 + npc.CurrentRank * 2;
                npc.Cultivation += gain;
                npc.Mood -= 1;
                Add(pending, Event(npc, tick, action.ToString(), text, EventVisibility.Private, 25,
                    $"{npc.Name} {text}一月，进度 +{gain}（当前 {npc.Cultivation}/{npc.AdvanceThreshold}）。",
                    new { action = "meditate", path = npc.Path, gain, cultivation = npc.Cultivation }));
                break;
            }
            case NpcActionType.Research:
            {
                var bonus = context.HasFacility(npc, Facility.Library) ? 4 : 0;
                var credit = 3 + npc.Comprehension / 10 + bonus;
                npc.ArcanaCredit += credit;
                var oldRank = npc.ArcanaRank;
                npc.ArcanaRank = npc.ArcanaCredit switch
                {
                    >= 300 => 4,
                    >= 160 => 3,
                    >= 80 => 2,
                    >= 30 => 1,
                    _ => 0
                };
                var promoted = npc.ArcanaRank > oldRank;
                Add(pending, Event(npc, tick, action.ToString(), "研究", EventVisibility.Witness, promoted ? 70 : 30,
                    promoted
                        ? $"{npc.Name} 的研究成果通过归档评审，学术等级升至 {npc.ArcanaRank} 级。"
                        : $"{npc.Name} 整理文献推进研究，学术积分 +{credit}（当前 {npc.ArcanaCredit}）。",
                    new { action = "research", credit, arcanaRank = npc.ArcanaRank }));
                break;
            }
            case NpcActionType.Work:
            {
                var gain = 5 + npc.Physique / 18 + random.Next(0, 4);
                npc.Money += gain;
                npc.Mood -= 2;
                Add(pending, Event(npc, tick, action.ToString(), "工作", EventVisibility.Local, 20,
                    $"{npc.Name} 做工一月，得钱 {gain} 文。",
                    new { action = "work", gain, money = npc.Money }));
                break;
            }
            case NpcActionType.Trade:
            {
                if (!context.HasFacility(npc, Facility.Market))
                {
                    goto case NpcActionType.Rest;
                }
                var site = context.SiteOf(npc)?.Name ?? "市场";
                var potionCost = (int)Math.Round(12 * price);
                if (npc.Health < 70 && npc.Money >= potionCost)
                {
                    npc.Money -= potionCost;
                    npc.Health += 15;
                    Add(pending, Event(npc, tick, action.ToString(), "交易", EventVisibility.Local, 30,
                        $"{npc.Name} 在{site}购得疗伤药剂，花费 {potionCost} 文，健康 +15。",
                        new { action = "trade", bought = "药剂", cost = potionCost }));
                    break;
                }
                var materialCost = (int)Math.Round(12 * price);
                var reagentCost = FactionEconomy.RetailPrice(price);
                if (!Inventory.Has(npc, "进阶秘材") && npc.Money >= reagentCost && npc.Path != "none")
                {
                    npc.Money -= reagentCost;
                    // 秘材不是凭空刷出来的：有商会就动商会的库存，钱进商会金库；商会断货才走外地黑市
                    var supplier = context.ReagentSupplier();
                    if (supplier is not null)
                    {
                        supplier.ReagentStock -= 1;
                        supplier.Treasury += reagentCost;
                    }
                    Add(pending, Event(npc, tick, action.ToString(), "交易", EventVisibility.Local, 35,
                        supplier is not null
                            ? $"{npc.Name} 在{site}的黑市柜台从{supplier.Name}购得【进阶秘材】，花费 {reagentCost} 文（余 {npc.Money} 文）。"
                            : $"{npc.Name} 在{site}等外地商队捎来【进阶秘材】，花费 {reagentCost} 文（余 {npc.Money} 文）。",
                        new { action = "trade", bought = "进阶秘材", cost = reagentCost, supplierId = supplier?.Id ?? 0 }));
                    break;
                }
                if (npc.Money >= materialCost)
                {
                    npc.Money -= materialCost;
                    Inventory.Add(npc, "基础材料");
                    Add(pending, Event(npc, tick, action.ToString(), "交易", EventVisibility.Local, 20,
                        $"{npc.Name} 在{site}购入一批【基础材料】，花费 {materialCost} 文。",
                        new { action = "trade", bought = "基础材料", cost = materialCost }));
                }
                break;
            }
            case NpcActionType.Craft:
            {
                var siteInfo = context.SiteOf(npc);
                if (siteInfo is null
                    || !siteInfo.Has(Facility.Workshop)
                    || !Inventory.Has(npc, "基础材料"))
                {
                    goto case NpcActionType.Rest;
                }
                var specialty = string.IsNullOrEmpty(siteInfo.Specialty) ? "杂货" : siteInfo.Specialty;
                var recipe = NpcActionPlanner.ChooseRecipe(npc, specialty);
                var skill = NpcActionPlanner.CraftSkill(npc, specialty);
                Inventory.Remove(npc, "基础材料");
                // 越难做的配方越容易失手：手艺减去配方门槛
                var chance = 56 + skill / 2 - recipe.MinSkill / 3;
                if (random.Next(100) >= Math.Clamp(chance, 25, 94))
                {
                    var damage = 4 + recipe.MinSkill / 12 + random.Next(0, 6);
                    npc.Health -= damage;
                    Add(pending, Event(npc, tick, action.ToString(), "制作", EventVisibility.Witness, 55,
                        $"{npc.Name} 在{siteInfo.Name}制作【{recipe.Product}】时失手，材料损毁，健康 -{damage}。",
                        new { action = "craft", outcome = "accident", product = recipe.Product, specialty, damage }));
                    break;
                }
                var earn = (int)Math.Round(recipe.Price * price * (0.9 + skill / 200.0)) + random.Next(0, 6);
                npc.Money += earn;
                npc.ArcanaCredit += npc.Path == "mage" ? 2 : 0;
                Add(pending, Event(npc, tick, action.ToString(), "制作", EventVisibility.Local, 35,
                    $"{npc.Name} 在{siteInfo.Name}（{specialty}）制成【{recipe.Product}】并售出，得钱 {earn} 文。",
                    new { action = "craft", outcome = "success", product = recipe.Product, specialty, skill, earn }));
                break;
            }
            case NpcActionType.Heal:
            {
                var gain = 10 + npc.Physique / 10;
                npc.Health += gain;
                npc.Money = Math.Max(0, npc.Money - 3);
                Add(pending, Event(npc, tick, action.ToString(), "疗伤", EventVisibility.Private, 25,
                    $"{npc.Name} 疗养一月，健康 +{gain}。",
                    new { action = "heal", gain, health = npc.Health }));
                break;
            }
            case NpcActionType.Socialize:
            {
                var relation = relations.Where(r => r.FromId == npc.Id)
                    .OrderBy(_ => random.Next()).FirstOrDefault();
                Npc? target;
                if (relation is null)
                {
                    target = npcs.Where(n => n.Id != npc.Id && n.LocationId == npc.LocationId)
                        .OrderBy(_ => random.Next()).FirstOrDefault()
                        ?? npcs.Where(n => n.Id != npc.Id).OrderBy(_ => random.Next()).FirstOrDefault();
                    if (target is null)
                    {
                        goto case NpcActionType.Rest;
                    }
                    relation = new Relation { FromId = npc.Id, ToId = target.Id, Kind = "friend" };
                    relations.Add(relation);
                }
                else
                {
                    target = npcs.FirstOrDefault(n => n.Id == relation.ToId);
                }

                var gain = 3 + random.Next(0, 6) + npc.Charm / 30;
                relation.Affinity = Math.Clamp(relation.Affinity + gain, -100, 100);
                relation.Trust = Math.Clamp(relation.Trust + 1, -100, 100);
                relation.LastContactTick = tick;
                npc.Mood += 4;
                if (target is not null)
                {
                    target.Mood += 2;
                }
                Add(pending, Event(npc, tick, action.ToString(), "社交", EventVisibility.Witness, 30,
                    $"{npc.Name} 与 {target?.Name ?? "故人"} 交谈甚欢，好感 +{gain}（当前 {relation.Affinity}）。",
                    new { action = "socialize", targetId = relation.ToId, affinityGain = gain }));
                break;
            }
            case NpcActionType.Migrate:
            {
                // 缺什么设施就往有什么设施的地方迁：仪式厅 > 工坊 > 市场；都没有就挑更安全的便宜地
                var ready = npc.Cultivation >= npc.AdvanceThreshold
                    && (npc.Path == "mage" ? npc.Realm < 9 : npc.Path == "knight" && npc.BloodlineAwakened);
                var hasReagent = Inventory.Has(npc, "进阶秘材");
                var wanted = ready && hasReagent && !context.HasFacility(npc, Facility.Ritual) ? Facility.Ritual
                    : ready && !hasReagent && !context.HasFacility(npc, Facility.Market) ? Facility.Market
                    : npc.Role is "药剂师" or "铁匠" or "木匠" or "裁缝" or "拾荒者" && !context.HasFacility(npc, Facility.Workshop)
                        ? Facility.Workshop
                    : npc.Role is "行商" or "掮客" or "古物贩子" && !context.HasFacility(npc, Facility.Market)
                        ? Facility.Market
                    : null;

                // 胆子跟着实力走：够强的人才敢往危险地区（遗迹）落脚
                var maxDanger = npc.Power >= 55 || npc.CurrentRank >= 5 ? 60 : 35;
                var candidates = context.Locales
                    .Where(l => l.Id != npc.LocationId && l.Danger <= maxDanger)
                    .Where(l => wanted is null || context.LocaleHasFacility(l.Id, wanted))
                    .ToList();
                if (candidates.Count == 0 || npc.Money < 5)
                {
                    goto case NpcActionType.Rest;
                }

                var destination = wanted is not null
                    ? candidates[random.Next(candidates.Count)]
                    : candidates.OrderBy(l => l.Danger + (l.PriceIndex - 100) * 0.2).First();
                var site = (wanted is not null ? context.FindFacility(destination.Id, wanted) : null)
                    ?? context.SitesIn(destination.Id).Where(s => s.Facilities.Length > 0)
                        .OrderBy(_ => random.Next()).FirstOrDefault()
                    ?? context.SitesIn(destination.Id).OrderBy(_ => random.Next()).FirstOrDefault();

                npc.Money -= 5;
                npc.LocationId = destination.Id;
                npc.SiteId = site?.Id ?? 0;
                npc.Mood += 3;
                var reason = wanted switch
                {
                    Facility.Ritual => "为进阶仪式寻一处仪式厅",
                    Facility.Market when ready && !hasReagent => "为求购进阶秘材迁往市集",
                    Facility.Market => "为生意迁往市集所在",
                    Facility.Workshop => "为谋生的工坊另寻去处",
                    _ => "换个活法"
                };
                Add(pending, Event(npc, tick, action.ToString(), "迁移", EventVisibility.Local, 40,
                    $"{npc.Name} {reason}，花费 5 文迁往{destination.Name}·{site?.Name ?? "街头"}。",
                    new { action = "migrate", to = destination.Id, siteId = site?.Id ?? 0, reason }));
                break;
            }
            case NpcActionType.Explore:
            {
                var exploreIndex = pending.Count;
                var target = npc.LocationId == 8 ? "巨环遗迹内环" : "巨环遗迹外围";
                var faction = context.FactionOf(npc);
                // 组织缺料时这趟出门就是公差：目标是找秘材，回来要上交
                var onCommission = faction is not null && faction.ReagentLow && npc.Path != "none";
                var tag = onCommission ? "委托取材" : "探索";
                Add(pending, Event(npc, tick, action.ToString(), tag, EventVisibility.Local, 25,
                    onCommission
                        ? $"{npc.Name} 受{faction!.Name}委托，从{locale?.Name ?? "城外"}动身前往{target}搜寻进阶秘材。"
                        : $"{npc.Name} 从{locale?.Name ?? "城外"}动身，前往{target}探索。",
                    new { action = "explore", phase = "departure", target, commission = onCommission }));
                if (Encounters.ShouldTrigger(npc, random))
                {
                    var result = Encounters.Resolve(npc, random);
                    var encounter = new GameEvent
                    {
                        Tick = tick,
                        Type = "Encounter",
                        Layer = EventLayer.Environment,
                        Status = EventStatus.Resolved,
                        ActorIds = npc.Id.ToString(),
                        LocationId = npc.LocationId,
                        FactsJson = JsonSerializer.Serialize(result.Facts),
                        Visibility = result.Def.Hostile ? EventVisibility.Local : EventVisibility.Witness,
                        Salience = result.Salience,
                        Tags = result.Def.Name,
                        Summary = result.Summary
                    };
                    Add(pending, encounter, exploreIndex);
                }
                else
                {
                    // 没遇到奇缘也会捡些材料回来，给制作链供料；公差出门则有机会刨到秘材
                    var gathered = random.Next(100) < 35;
                    var foundReagent = onCommission && random.Next(100) < 45;
                    npc.Cultivation += 2;
                    npc.Mood += 2;
                    if (gathered)
                    {
                        Inventory.Add(npc, "基础材料");
                    }
                    if (foundReagent)
                    {
                        Inventory.Add(npc, "进阶秘材");
                    }
                    var found = foundReagent
                        ? "从塌方的旧祭坛夹层里刨出一件【进阶秘材】"
                        : gathered ? "采得一批【基础材料】" : "见闻渐长";
                    Add(pending, Event(npc, tick, action.ToString(), tag, EventVisibility.Local, foundReagent ? 45 : 15,
                        $"{npc.Name} 游历一月未遇奇缘，{found}，进度 +2。",
                        new { action = "explore", encounter = "none", gathered, foundReagent }));
                }

                // 取材上交：组织缺料、自己又还没到进阶门槛，就把秘材充公换赏金与功勋
                if (faction is not null)
                {
                    var turnIn = FactionEconomy.TurnIn(npc, faction, tick, price, random);
                    if (turnIn is not null)
                    {
                        Add(pending, turnIn, exploreIndex);
                    }
                }
                break;
            }
            case NpcActionType.Advance:
            {
                if (!context.HasFacility(npc, Facility.Ritual)
                    || !Inventory.Has(npc, "进阶秘材")
                    || npc.Cultivation < npc.AdvanceThreshold)
                {
                    // 前置条件在执行阶段再次校验：缺设施/秘材/进度时不得进阶
                    goto case NpcActionType.Rest;
                }
                var cost = NpcActionPlanner.AdvanceCost(npc);
                var ritualSite = context.SiteOf(npc)?.Name ?? "仪式厅";
                npc.Money -= cost;
                Inventory.Remove(npc, "进阶秘材");
                var stat = npc.Path == "mage" ? npc.Constitution : npc.Physique;
                var chance = 35 + stat / 4 + npc.Willpower / 4 + (npc.Health - 60) / 2;
                if (random.Next(0, 100) < Math.Clamp(chance, 5, 95))
                {
                    if (npc.Path == "knight")
                    {
                        npc.KnightRank += 1;
                    }
                    else
                    {
                        npc.Realm += 1;
                    }
                    npc.Power += 14 + npc.CurrentRank * 5;
                    npc.Cultivation = 0;
                    npc.Mood += 10;
                    Add(pending, Event(npc, tick, action.ToString(), "进阶", EventVisibility.Public, 85,
                        $"{npc.Name} 在{ritualSite}耗费 {cost} 文与全部秘材完成进阶仪式，晋升【{npc.RankTitle}】。",
                        new { action = "advance", outcome = "success", cost, rank = npc.CurrentRank, site = ritualSite }));
                }
                else
                {
                    var damage = 12 + random.Next(0, 9);
                    npc.Health -= damage;
                    npc.Cultivation = npc.AdvanceThreshold * 3 / 5;
                    npc.Mood -= 8;
                    Add(pending, Event(npc, tick, action.ToString(), "进阶", EventVisibility.Witness, 65,
                        $"{npc.Name} 在{ritualSite}的进阶仪式失败：耗费 {cost} 文与秘材，反噬致健康 -{damage}。",
                        new { action = "advance", outcome = "failure", cost, damage, site = ritualSite }));
                }
                break;
            }
            case NpcActionType.Rest:
            default:
            {
                npc.Health += 4;
                npc.Mood += 3;
                Add(pending, Event(npc, tick, "Rest", "休整", EventVisibility.Private, 10,
                    $"{npc.Name} 休整一月，心神稍复。",
                    new { action = "rest" }));
                break;
            }
        }
    }

    private static void Clamp(Npc npc)
    {
        npc.Health = Math.Clamp(npc.Health, 1, 100);
        npc.Mood = Math.Clamp(npc.Mood, 0, 100);
        npc.Money = Math.Max(0, npc.Money);
        npc.Cultivation = Math.Min(npc.Cultivation, npc.AdvanceThreshold * 2);
    }

    internal static EventDto ToDto(GameEvent e, WorldState world, IReadOnlyList<Npc> npcs)
    {
        string NameOf(string ids) =>
            long.TryParse(ids.Split(',', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(), out var id)
                ? npcs.FirstOrDefault(n => n.Id == id)?.Name ?? $"#{id}"
                : "";

        return new EventDto
        {
            Tick = e.Tick,
            Time = $"{world.Year}.{world.Month:D2}",
            Type = e.Tags,
            Actor = NameOf(e.ActorIds),
            Target = NameOf(e.TargetIds),
            Summary = e.Summary,
            Salience = e.Salience,
            Status = e.Status switch
            {
                EventStatus.Planned => "计划中",
                EventStatus.Resolved => "已发生",
                EventStatus.Aborted => "已中止",
                _ => "已过期"
            }
        };
    }
}
