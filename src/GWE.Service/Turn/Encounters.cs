using GWE.Domain.Entities;

namespace GWE.Service.Turn;

public enum EncounterKind
{
    Treasure,
    LostLaboratory,
    MoonHerbValley,
    ArcaneScroll,
    StatueGuardian,
    WolfPack,
    NightmareWhisper
}

/// <summary>
/// 遭遇定义。难度 = BaseDifficulty + DifficultyPerRank * 当前等阶；
/// 能力值 = 各属性 × 权重之和，不同遭遇吃不同属性。
/// </summary>
public sealed record EncounterDef(
    EncounterKind Kind,
    string Name,
    int BaseDifficulty,
    int DifficultyPerRank,
    double WPower,
    double WComprehension,
    double WWillpower,
    double WPhysique,
    double WCharm,
    bool Hostile);

public sealed record EncounterResult(
    EncounterDef Def,
    double Ability,
    int Difficulty,
    int Roll,
    double Score,
    string Band,
    string Summary,
    int Salience,
    object Facts);

/// <summary>
/// 奇遇/突发事件结算（L4 环境遭遇，西部奇幻题材）。
/// 抽取：按 NPC 实力与遭遇基础难度的距离加权，低阶不会一头撞进高阶秘境。
/// 判定：score = 能力值 + 等阶历练 + 随机(-15..15) − 难度
/// 分档：score ≥ 25 大成功；≥ 0 成功；≥ −20 失败；否则 惨败。
/// 奖励倍率：大成功 1.6 / 成功 1.0 / 失败 0.2 / 惨败 0；敌对遭遇失败追加健康与金钱损失。
/// </summary>
public static class Encounters
{
    private static readonly EncounterDef[] Catalog =
    [
        new(EncounterKind.Treasure, "走私者的赃物窖藏", 30, 5, 0.60, 0.40, 0, 0, 0, false),
        new(EncounterKind.LostLaboratory, "失落的奥术实验室", 36, 6, 0, 0.70, 0.30, 0, 0, false),
        new(EncounterKind.MoonHerbValley, "月光草幽谷", 20, 4, 0, 0.30, 0, 0.70, 0, false),
        new(EncounterKind.ArcaneScroll, "古代法术残页", 26, 5, 0, 1.00, 0, 0, 0, false),
        new(EncounterKind.StatueGuardian, "遗迹石像守卫", 32, 8, 0.80, 0, 0, 0.20, 0, true),
        new(EncounterKind.WolfPack, "荒野狼群", 24, 6, 1.00, 0, 0, 0, 0, true),
        new(EncounterKind.NightmareWhisper, "梦魇低语", 34, 6, 0, 0, 0.80, 0, 0.20, true)
    ];

    /// <summary>探索时是否触发遭遇：基础 45% + 好奇/300（好奇 0..100 → 最多 +33%）。</summary>
    public static bool ShouldTrigger(Npc npc, Random random)
    {
        var curiosity = Personality.Get(npc, "好奇");
        return random.NextDouble() < 0.45 + curiosity / 300.0;
    }

    public static EncounterResult Resolve(Npc npc, Random random)
    {
        var rank = npc.CurrentRank;
        var def = PickDef(Capability(npc), random);
        var difficulty = (int)Math.Round(def.BaseDifficulty + def.DifficultyPerRank * rank * 0.8);
        var ability =
            def.WPower * npc.Power
            + def.WComprehension * npc.Comprehension
            + def.WWillpower * npc.Willpower
            + def.WPhysique * npc.Physique
            + def.WCharm * npc.Charm
            + rank * 6; // 等阶历练：临场经验，避免高阶被难度曲线单方面碾压

        var roll = random.Next(-15, 16);
        var score = ability + roll - difficulty;
        var (band, multiplier) = score switch
        {
            >= 25 => ("大成功", 1.6),
            >= 0 => ("成功", 1.0),
            >= -20 => ("失败", 0.2),
            _ => ("惨败", 0.0)
        };

        var rewardText = ApplyOutcome(npc, def, multiplier, band, random);
        var salience = band switch { "大成功" => 90, "成功" => 65, "失败" => 55, _ => 75 };
        var summary =
            $"{npc.Name} 探索遗迹遭遇【{def.Name}】：能力 {ability:F0} + 随机 {roll:+#;-#;0} − 难度 {difficulty} = {score:F0}，{band}。{rewardText}";

        var facts = new
        {
            action = "explore",
            encounter = def.Kind.ToString(),
            ability = Math.Round(ability, 1),
            difficulty,
            roll,
            score = Math.Round(score, 1),
            outcome = band,
            reward = rewardText
        };
        return new EncounterResult(def, ability, difficulty, roll, score, band, summary, salience, facts);
    }

    /// <summary>综合实力估计：用于把遭遇的难度档位和探索者匹配起来。</summary>
    private static double Capability(Npc npc) =>
        npc.Power * 0.55
        + (npc.Comprehension + npc.Willpower + npc.Physique + npc.Charm) * 0.25;

    /// <summary>按“难度档位离实力越近权重越高”抽取遭遇，避免平民撞上高阶秘境。</summary>
    private static EncounterDef PickDef(double capability, Random random)
    {
        var weights = Catalog
            .Select(d => 1.0 / (1.0 + Math.Abs(d.BaseDifficulty - capability) / 18.0))
            .ToArray();
        var total = weights.Sum();
        var pick = random.NextDouble() * total;
        for (var i = 0; i < Catalog.Length; i++)
        {
            pick -= weights[i];
            if (pick <= 0)
            {
                return Catalog[i];
            }
        }
        return Catalog[^1];
    }

    private static string ApplyOutcome(Npc npc, EncounterDef def, double m, string band, Random random)
    {
        if (m <= 0 && !def.Hostile)
        {
            npc.Mood -= 3;
            return "空手而归，白费一月脚力。";
        }

        if (def.Hostile && m < 1.0)
        {
            var damage = (int)((8 + def.BaseDifficulty / 6.0) * (band == "惨败" ? 1.8 : 1.0));
            npc.Health -= damage;
            npc.Mood -= 6;
            var extra = "";
            if (def.Kind == EncounterKind.WolfPack)
            {
                var loss = Math.Min(npc.Money, 8 + random.Next(0, 10));
                npc.Money -= loss;
                extra = $"，行囊被撕破散落 {loss} 文";
            }
            if (band == "失败" && m > 0)
            {
                var partial = (int)(6 * m);
                npc.Cultivation += partial;
                return $"且战且退，健康 -{damage}{extra}，搏斗中略有长进 +{partial}。";
            }
            return $"不敌受创，健康 -{damage}{extra}。";
        }

        switch (def.Kind)
        {
            case EncounterKind.Treasure:
            {
                var money = (int)(25 * m) + random.Next(0, 8);
                npc.Money += money;
                Inventory.Add(npc, "走私赃物");
                return $"撬开窖藏，得钱 {money} 文与来路不明的货物。";
            }
            case EncounterKind.LostLaboratory:
            {
                var cultivation = (int)(25 * m);
                npc.Cultivation += cultivation;
                var credit = (int)(10 * m);
                npc.ArcanaCredit += credit;
                if (m >= 1.0 && random.Next(100) < 60)
                {
                    Inventory.Add(npc, "进阶秘材");
                    return $"研读遗存实验记录，修为 +{cultivation}，学术积分 +{credit}，并搜得【进阶秘材】。";
                }
                return $"研读遗存实验记录，修为 +{cultivation}，学术积分 +{credit}。";
            }
            case EncounterKind.MoonHerbValley:
            {
                var heal = (int)(12 * m);
                var money = (int)(8 * m);
                npc.Health += heal;
                npc.Money += money;
                Inventory.Add(npc, "月光草");
                return $"采得月光草，健康 +{heal}，余草售得 {money} 文。";
            }
            case EncounterKind.ArcaneScroll:
            {
                var credit = (int)(20 * m);
                npc.ArcanaCredit += credit;
                if (band == "大成功")
                {
                    Inventory.Add(npc, "进阶秘材");
                    return $"通读残页并抄录关键仪轨，学术积分 +{credit}，另有【进阶秘材】线索入手。";
                }
                return $"通读残页，学术积分 +{credit}。";
            }
            case EncounterKind.StatueGuardian:
            {
                var cultivation = (int)(16 * m);
                npc.Cultivation += cultivation;
                if (band == "大成功")
                {
                    Inventory.Add(npc, "进阶秘材");
                    return $"击碎石像，从核心取出【进阶秘材】，修为 +{cultivation}。";
                }
                return $"逼退石像，夺得符文碎石，修为 +{cultivation}。";
            }
            case EncounterKind.WolfPack:
            {
                var money = (int)(12 * m) + random.Next(0, 8);
                npc.Money += money;
                return $"驱散狼群，剥售皮毛得 {money} 文。";
            }
            case EncounterKind.NightmareWhisper:
            {
                npc.Mood += (int)(10 * m);
                if (band == "大成功")
                {
                    npc.Willpower += 1;
                    return "驱散梦魇，意志 +1，心境大进。";
                }
                return $"稳住心神，心境 +{(int)(10 * m)}。";
            }
            default:
                return "一无所获。";
        }
    }
}
