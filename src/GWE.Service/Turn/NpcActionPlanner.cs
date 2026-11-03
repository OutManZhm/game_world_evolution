using GWE.Domain;
using GWE.Domain.Entities;

namespace GWE.Service.Turn;

/// <summary>
/// 行为规划：按身份、路径、需求、性格、**所在现场设施**给候选行为打分。
/// 硬约束在此过滤：无血脉不能进阶、无仪式厅不能进阶、无工坊不能制作、无市场不能交易（红线 Q12）。
/// 对应设计文档 6.5 效用 AI 的精简实现。
/// </summary>
public static class NpcActionPlanner
{
    public static NpcActionType Choose(Npc npc, Random random, WorldContext context)
    {
        var scores = new Dictionary<NpcActionType, double>();

        var library = context.HasFacility(npc, Facility.Library);
        var workshop = context.HasFacility(npc, Facility.Workshop);
        var market = context.HasFacility(npc, Facility.Market);
        var ritual = context.HasFacility(npc, Facility.Ritual);
        var site = context.SiteOf(npc);

        // 冥想/训练：施法者吃根骨效率；骑士侍从吃体质；平民不选
        if (npc.Path == "mage")
        {
            scores[NpcActionType.Meditate] = 32 + npc.Constitution * 0.25
                + Math.Max(0, 80 - npc.CurrentRank * 9) * 0.30;
        }
        else if (npc.Path == "knight")
        {
            scores[NpcActionType.Meditate] = 30 + npc.Physique * 0.28;
        }

        // 研究：悟性主导；有文书库加成，没有也能自修但慢
        var scholarly = npc.Role is "研究者" or "评审成员" or "魔法学徒" or "高阶法师" or "药剂师" or "书记员";
        scores[NpcActionType.Research] = scholarly
            ? 20 + npc.Comprehension * 0.30 + (npc.ArcanaRank < 3 ? 8 : 0) + (library ? 8 : -6)
            : 2;

        // 工作：缺钱驱动
        scores[NpcActionType.Work] = 12 + Math.Max(0, 45 - npc.Money) * 0.8;

        // 交易：必须有市场。缺药剂/秘材/材料时才会去（秘材黑市价见 FactionEconomy.RetailPrice）
        var wantsPotion = npc.Health < 70 && npc.Money >= 12;
        var wantsReagent = npc.Path is "mage" or "knight"
            && !Inventory.Has(npc, "进阶秘材") && npc.Money >= 40;
        var wantsMaterial = CanCraft(npc, site) && !Inventory.Has(npc, "基础材料") && npc.Money >= 12;
        if (market && (wantsPotion || wantsReagent || wantsMaterial))
        {
            scores[NpcActionType.Trade] = 24 + npc.Money * 0.12;
        }

        // 制作：材料 + 现场专精 + 配方门槛，缺一不可（E02）
        if (CanCraft(npc, site) && Inventory.Has(npc, "基础材料"))
        {
            scores[NpcActionType.Craft] = 30 + npc.Comprehension * 0.18;
        }

        scores[NpcActionType.Heal] = npc.Health < 70 ? 90 - npc.Health : 0;

        scores[NpcActionType.Socialize] = 12 + npc.Charm * 0.22 + Math.Max(0, 60 - npc.Mood) * 0.4;

        // 迁移：缺仪式厅/工坊/市场、身处险地、物价压身、心情低落
        var ready = npc.Cultivation >= npc.AdvanceThreshold && CanAdvance(npc);
        var ritualGap = ready && Inventory.Has(npc, "进阶秘材") && !ritual;
        var craftGap = npc.Role is "药剂师" or "铁匠" && !workshop;
        var marketGap = npc.Role is "行商" or "掮客" && !market;
        var danger = context.LocaleOf(npc)?.Danger ?? 0;
        // 遗迹讨生活的人不怕危险，危险本身就是他们的饭碗
        var rugged = npc.Role is "拾荒者" or "守遗迹人" or "亡命徒" or "古物贩子" or "野法师";
        scores[NpcActionType.Migrate] = 4
            + (ritualGap ? 120 : 0)
            + (craftGap ? 22 : 0)
            + (marketGap ? 16 : 0)
            + (danger >= 40 && !rugged ? (danger - 30) * 0.8 : 0)
            + Math.Max(0, 30 - npc.Mood) * 0.6
            + (npc.Money < 10 ? 8 : 0);

        if (rugged)
        {
            // 遗迹讨生活的人本来就扎根在这儿：搬家意愿打对折
            scores[NpcActionType.Migrate] *= 0.5;
        }

        // 探索：受训者更常去，平民偶尔采药跑腿；遭遇难度会按实力匹配（见 Encounters.PickDef）
        var explorer = npc.Path != "none" || npc.Power >= 25;
        scores[NpcActionType.Explore] = 9 + Personality.Get(npc, "好奇") * 0.28
            + Math.Min(npc.Power, 120) * 0.05 + (npc.Role is "研究者" or "行商" ? 6 : 0)
            + (explorer ? 7 : 0)
            + (context.FactionNeedsReagent(npc) && npc.Path != "none" ? 22 : 0); // 组织缺料，派人出去取材

        // 进阶仪式：进度 + 秘材 + 金钱 + 仪式厅（红线 Q12）
        if (ready)
        {
            if (!Inventory.Has(npc, "进阶秘材"))
            {
                var waitingForIssue = context.ReagentStock(npc) > 0;
                if (waitingForIssue)
                {
                    // 组织库里有货：按贡献排队等发放，别自己乱跑（发放由 FactionEconomy 在月初执行）
                    scores[NpcActionType.Meditate] = Math.Max(scores.GetValueOrDefault(NpcActionType.Meditate), 90);
                }
                else if (market && npc.Money >= 40)
                {
                    // 库里空了：去市场买（正式成员买不到就只能自己找）
                    scores[NpcActionType.Trade] = 130;
                }
                else if (npc.Money >= 40)
                {
                    scores[NpcActionType.Migrate] = 125;
                }
                else
                {
                    scores[NpcActionType.Work] = 120;
                }

                if (!waitingForIssue && scores.TryGetValue(NpcActionType.Meditate, out var meditate))
                {
                    scores[NpcActionType.Meditate] = meditate * 0.3;
                }
            }
            else if (npc.Money < AdvanceCost(npc))
            {
                scores[NpcActionType.Work] = 115;
            }
            else if (!ritual)
            {
                scores[NpcActionType.Migrate] = 135;
            }
            else
            {
                scores[NpcActionType.Advance] = 150;
            }
        }

        scores[NpcActionType.Rest] = 8 + Math.Max(0, 100 - npc.Health) * 0.08;

        var best = NpcActionType.Rest;
        var bestScore = double.MinValue;
        foreach (var (action, score) in scores.OrderBy(s => (int)s.Key))
        {
            var noisy = score + random.NextDouble() * 10;
            if (noisy > bestScore)
            {
                bestScore = noisy;
                best = action;
            }
        }
        return best;
    }

    public static bool CanAdvance(Npc npc) => npc.Path switch
    {
        "mage" => npc.Realm < 9,
        "knight" => npc.BloodlineAwakened && npc.KnightRank < 4,
        _ => false
    };

    /// <summary>能否制作：站在带工坊的现场，且具备该专精的手艺（要么是本行，要么够博学）。</summary>
    public static bool CanCraft(Npc npc, Site? site)
    {
        if (site is null || !site.Has(Facility.Workshop))
        {
            return false;
        }
        var specialty = string.IsNullOrEmpty(site.Specialty) ? "杂货" : site.Specialty;
        if (RoleSpecialty(npc) == specialty)
        {
            return true;
        }
        return specialty switch
        {
            "药剂" => npc.Path == "mage" ? npc.ArcanaRank >= 1 : npc.Comprehension >= 50,
            "裁缝" => npc.Path == "mage" && npc.ArcanaRank >= 1, // 附魔衬里，得懂点符纹
            "杂货" => npc.Willpower >= 45,
            _ => npc.Comprehension >= 55
        };
    }

    /// <summary>制作配方：产品、基准售价、所属专精、手艺门槛。产出由“工坊专精 + 匠人手艺”共同决定。</summary>
    public sealed record CraftRecipe(string Product, int Price, string Specialty, int MinSkill);

    public static readonly CraftRecipe[] Recipes =
    [
        new("疗伤药剂", 18, "药剂", 0),
        new("宁神精油", 30, "药剂", 32),
        new("强效复苏剂", 54, "药剂", 62),
        new("干燥草药包", 11, "草药", 0),
        new("止血膏", 24, "草药", 26),
        new("精铁器具", 22, "锻造", 0),
        new("骑士护腕", 46, "锻造", 46),
        new("木工器具", 16, "木作", 0),
        new("符文木匣", 38, "木作", 50),
        new("御寒衣物", 15, "裁缝", 0),
        new("附魔衬里", 36, "裁缝", 42),
        new("粗糙器具", 12, "杂货", 0)
    ];

    /// <summary>职业对应的工坊专精：站对本行工坊才算熟手。</summary>
    public static string RoleSpecialty(Npc npc) => npc.Role switch
    {
        "药剂师" => "药剂",
        "研究者" => "药剂",
        "铁匠" => "锻造",
        "木匠" => "木作",
        "裁缝" => "裁缝",
        "拾荒者" => "杂货",
        _ => string.Empty
    };

    /// <summary>手艺值：悟性打底，学术等级加成，本行匠人再 +18。</summary>
    public static int CraftSkill(Npc npc, string specialty) =>
        npc.Comprehension / 2 + npc.ArcanaRank * 12
        + (RoleSpecialty(npc) == specialty ? 18 : 0);

    /// <summary>按现场专精挑配方：取手艺做得动的最贵那一档。</summary>
    public static CraftRecipe ChooseRecipe(Npc npc, string specialty)
    {
        var pool = Recipes.Where(r => r.Specialty == specialty).ToList();
        if (pool.Count == 0)
        {
            pool = Recipes.Where(r => r.Specialty == "杂货").ToList();
        }
        var skill = CraftSkill(npc, specialty);
        return pool.Where(r => r.MinSkill <= skill).OrderByDescending(r => r.Price).FirstOrDefault()
            ?? pool.OrderBy(r => r.Price).First();
    }

    /// <summary>进阶仪式费用：随当前等阶上涨。</summary>
    public static int AdvanceCost(Npc npc) => 20 + npc.CurrentRank * 15;
}
