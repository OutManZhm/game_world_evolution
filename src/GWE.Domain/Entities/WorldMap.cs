namespace GWE.Domain.Entities;

/// <summary>区域：空间三层的最上层（设计文档 5.x：Region → Locale → Site）。</summary>
public sealed class Region
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>主城 / 谷地 / 林地 / 海岸。</summary>
    public string Kind { get; set; } = "领地";
    /// <summary>区域基准危险度，地点在其基础上浮动。</summary>
    public int Danger { get; set; }
}

/// <summary>
/// 现场：Locale 内部的具体场所。设施能力挂在现场上（仪式厅、工坊、市场、文书库……），
/// 进阶/制作/交易/研究都要先站到对的地方，这也是“迁移”存在的理由。
/// </summary>
public sealed class Site
{
    public long Id { get; set; }
    public long LocaleId { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>广场 / 酒馆 / 工坊 / 仪式厅 / 文书库 / 神殿 / 营地。</summary>
    public string Kind { get; set; } = "场所";
    /// <summary>设施清单，逗号分隔：ritual,workshop,market,library,temple,tavern。</summary>
    public string Facilities { get; set; } = string.Empty;
    /// <summary>
    /// 生产专精：工坊/药圃具体产出什么（药剂、锻造、木作、裁缝、杂货）。
    /// 制作配方由“专精 + 匠人手艺”共同决定，所以药剂坊不会再产木工器具。
    /// </summary>
    public string Specialty { get; set; } = string.Empty;

    public bool Has(string facility) =>
        Facilities.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains(facility);
}

/// <summary>设施代号，避免各处写字符串魔法值。</summary>
public static class Facility
{
    public const string Ritual = "ritual";
    public const string Workshop = "workshop";
    public const string Market = "market";
    public const string Library = "library";
    public const string Temple = "temple";
    public const string Tavern = "tavern";
}
