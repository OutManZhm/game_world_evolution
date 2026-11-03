namespace GWE.Domain.Entities;

/// <summary>
/// 具名 NPC。玩家同样是一条 npc 记录（本版本未启用玩家角色）。
/// 题材按需求表 Q01：西方奇幻。三条成长线（Q10/N05）：
/// 法术环级 Realm、学术等级 ArcanaRank、骑士位阶 KnightRank。
/// 资质语义（N08）：根骨=精神力天赋（冥想效率与进阶门槛）、悟性=研究速度与成果质量、
/// 体质=骑士训练上限、魅力=社交与庇护、意志=反噬抵抗与审讯承受。
/// </summary>
public sealed class Npc
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Sex { get; set; }
    public int BirthTick { get; set; }
    public long FactionId { get; set; }
    /// <summary>公开身份：店员 / 学者 / 神职者 / 贵族……</summary>
    public string Role { get; set; } = "平民";
    public bool IsMain { get; set; }
    public long LocationId { get; set; }
    /// <summary>当前所在现场（Locale 内部的仪式厅/工坊/市场等），0 表示露宿街头。</summary>
    public long SiteId { get; set; }

    /// <summary>超凡路径：mage 施法者 / knight 血脉骑士 / none 普通人。体系互不兼容（Q01）。</summary>
    public string Path { get; set; } = "none";
    /// <summary>血脉是否觉醒：进入骑士位阶的硬前提（资源清单·血脉与神恩）。</summary>
    public bool BloodlineAwakened { get; set; }

    public int Constitution { get; set; }
    public int Comprehension { get; set; }
    public int Charm { get; set; }
    public int Physique { get; set; }
    public int Willpower { get; set; }

    /// <summary>法术环级（仅 mage）。</summary>
    public int Realm { get; set; }
    /// <summary>骑士位阶（仅 knight）。</summary>
    public int KnightRank { get; set; }
    public int Power { get; set; }
    public int ArcanaRank { get; set; }
    public int ArcanaCredit { get; set; }
    /// <summary>进阶进度（冥想/训练积累），达到门槛后可尝试进阶。</summary>
    public int Cultivation { get; set; }

    public int Authority { get; set; }
    public int Reach { get; set; }
    public int ApBudget { get; set; } = 2;
    public int Health { get; set; } = 100;
    public int Mood { get; set; } = 60;
    public int Money { get; set; } = 20;

    public string PersonalityJson { get; set; } = "{}";
    public string NeedsJson { get; set; } = "{}";
    public string InventoryJson { get; set; } = "[]";
    public string StateJson { get; set; } = "{}";

    public int Age(int tick) => Math.Max(0, (tick - BirthTick) / 12);

    /// <summary>当前成长线的展示称号。</summary>
    public string RankTitle => Path switch
    {
        "mage" => Realm == 0 ? "魔法学徒" : $"{Realm} 环法师",
        "knight" => KnightRank switch
        {
            0 => "骑士侍从",
            1 => "准骑士",
            2 => "正式骑士",
            3 => "大骑士",
            _ => "辉光骑士"
        },
        _ => ArcanaRank >= 3 ? "大学者" : "平民"
    };

    /// <summary>进阶所需进度门槛：根骨（精神力天赋）决定门槛高低（N08）。</summary>
    public int AdvanceThreshold => 80 + Math.Max(0, 60 - Constitution) / 3;

    /// <summary>当前成长线等阶（用于难度与费用计算）。</summary>
    public int CurrentRank => Path == "knight" ? KnightRank : Realm;
}
