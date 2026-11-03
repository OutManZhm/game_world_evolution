using GWE.Domain.Entities;
using GWE.Repository.Models;
using SqlSugar;

namespace GWE.Repository;

/// <summary>
/// 建表 + 首次启动种子数据。严格按《需求确认与填写表》第 11 节"最小世界初始数据"：
/// I01 地图、I02 300 独立 NPC 结构分布、I03 五类组织、I05 社会关系、I06 四个初始矛盾。
/// 只在 npc 表为空时播种。
/// </summary>
public sealed class DatabaseInitializer(IDbSession session)
{
    public void Initialize()
    {
        var db = session.Client;
        db.CodeFirst.InitTables<WorldStateRow>();
        db.CodeFirst.InitTables<FactionRow>();
        db.CodeFirst.InitTables<RegionRow>();
        db.CodeFirst.InitTables<LocaleRow>();
        db.CodeFirst.InitTables<SiteRow>();
        db.CodeFirst.InitTables<NpcRow>();
        db.CodeFirst.InitTables<RelationRow>();
        db.CodeFirst.InitTables<EventRow>();
        db.CodeFirst.InitTables<TensionRow>();
        db.CodeFirst.InitTables<NpcTurnRow>();
        db.CodeFirst.InitTables<MemoryRow>();
        db.CodeFirst.InitTables<BeliefRow>();
        db.CodeFirst.InitTables<KnowledgeDeliveryRow>();
        db.CodeFirst.InitTables<EventScheduleRow>();

        if (db.Queryable<NpcRow>().Any())
        {
            return;
        }

        db.Insertable(new WorldState { Id = 1, Tick = 0, Year = 1287, Month = 1 }.ToRow()).ExecuteCommand();

        // 空间三层：Region → Locale → Site
        var regions = new List<Region>
        {
            new() { Id = 1, Name = "灰岩城", Kind = "主城", Danger = 8 },
            new() { Id = 2, Name = "灰岩近郊", Kind = "谷地", Danger = 14 },
            new() { Id = 3, Name = "灰烬林地", Kind = "林地", Danger = 30 },
            new() { Id = 4, Name = "潮汐海岸", Kind = "海岸", Danger = 20 }
        };
        db.Insertable(regions.Select(r => r.ToRow()).ToList()).ExecuteCommand();

        // I01：1 主城（三区）+ 1 飞地 + 2 村镇 + 1 港口 + 1 遗迹
        var locales = new List<Locale>
        {
            new() { Id = 1, RegionId = 1, Name = "灰岩城·平民区", Kind = "平民区", Danger = 10, PriceIndex = 110 },
            new() { Id = 2, RegionId = 1, Name = "灰岩城·贵族区", Kind = "贵族区", Danger = 5, PriceIndex = 150 },
            new() { Id = 3, RegionId = 1, Name = "灰岩城·机构区", Kind = "机构区", Danger = 8, PriceIndex = 120 },
            new() { Id = 4, RegionId = 3, Name = "灰烬林地·施法者飞地", Kind = "飞地", Danger = 25, PriceIndex = 130 },
            new() { Id = 5, RegionId = 2, Name = "河湾村", Kind = "村镇", Danger = 15, PriceIndex = 90 },
            new() { Id = 6, RegionId = 2, Name = "白垩镇", Kind = "村镇", Danger = 12, PriceIndex = 95 },
            new() { Id = 7, RegionId = 4, Name = "潮汐港", Kind = "港口", Danger = 20, PriceIndex = 105 },
            new() { Id = 8, RegionId = 3, Name = "巨环遗迹", Kind = "遗迹", Danger = 55, PriceIndex = 100 }
        };
        db.Insertable(locales.Select(l => l.ToRow()).ToList()).ExecuteCommand();

        // Site：设施挂在现场上，进阶要仪式厅、制作要工坊、交易要市场、研究要文书库
        // Specialty：工坊专精，决定这里能出什么货（药剂/锻造/木作/裁缝/草药/杂货）
        var sites = new List<Site>
        {
            new() { Id = 1, LocaleId = 1, Name = "市集广场", Kind = "广场", Facilities = "market" },
            new() { Id = 2, LocaleId = 1, Name = "鼠巷酒馆", Kind = "酒馆", Facilities = "tavern" },
            new() { Id = 3, LocaleId = 1, Name = "守夜人哨所", Kind = "哨所", Facilities = "" },
            new() { Id = 4, LocaleId = 2, Name = "领主厅", Kind = "厅堂", Facilities = "" },
            new() { Id = 5, LocaleId = 2, Name = "家族礼拜堂", Kind = "神殿", Facilities = "ritual,temple" },
            new() { Id = 6, LocaleId = 2, Name = "庄园工坊", Kind = "工坊", Facilities = "workshop", Specialty = "锻造" },
            new() { Id = 7, LocaleId = 3, Name = "裁判所大厅", Kind = "审讯厅", Facilities = "" },
            new() { Id = 8, LocaleId = 3, Name = "灰塔文书库", Kind = "文书库", Facilities = "library" },
            new() { Id = 9, LocaleId = 3, Name = "公会交易所", Kind = "交易所", Facilities = "market" },
            new() { Id = 10, LocaleId = 4, Name = "灰塔仪式厅", Kind = "仪式厅", Facilities = "ritual,library" },
            new() { Id = 11, LocaleId = 4, Name = "药剂工坊", Kind = "工坊", Facilities = "workshop,market", Specialty = "药剂" },
            new() { Id = 12, LocaleId = 5, Name = "村口集市", Kind = "集市", Facilities = "market" },
            new() { Id = 13, LocaleId = 5, Name = "草药圃", Kind = "药圃", Facilities = "workshop", Specialty = "草药" },
            new() { Id = 14, LocaleId = 6, Name = "白垩矿场", Kind = "矿场", Facilities = "" },
            new() { Id = 15, LocaleId = 6, Name = "镇公所", Kind = "厅堂", Facilities = "market" },
            new() { Id = 16, LocaleId = 7, Name = "码头市场", Kind = "市场", Facilities = "market" },
            new() { Id = 17, LocaleId = 7, Name = "船坞工坊", Kind = "工坊", Facilities = "workshop", Specialty = "木作" },
            new() { Id = 18, LocaleId = 8, Name = "外环营地", Kind = "营地", Facilities = "tavern" },
            // 遗迹不是无人区：拾荒者、古物贩子与守遗迹人在这里讨生活
            new() { Id = 19, LocaleId = 8, Name = "拾荒者棚屋", Kind = "工坊", Facilities = "workshop", Specialty = "杂货" },
            new() { Id = 20, LocaleId = 8, Name = "黑市摊位", Kind = "市场", Facilities = "market" },
            new() { Id = 21, LocaleId = 8, Name = "内环古祭坛", Kind = "祭坛", Facilities = "ritual" },
            new() { Id = 22, LocaleId = 1, Name = "成衣铺", Kind = "工坊", Facilities = "workshop", Specialty = "裁缝" }
        };
        db.Insertable(sites.Select(s => s.ToRow()).ToList()).ExecuteCommand();

        // I03：五类组织。秘材是组织内的有限物资：cap 是固定配额，stock 是当前库存。
        // 行会/帮派是商会，cap 大、库存厚，负责全城的批发与零售；学派/教会/贵族只能从它们手里拿货。
        var factions = new List<Faction>
        {
            new() { Id = 1, Name = "圣焰裁判所", Kind = "信仰与执法机构", Prestige = 80, HeadquartersId = 3, Treasury = 900, ReagentCap = 4, ReagentStock = 3 },
            new() { Id = 2, Name = "秘法议会·灰塔分会", Kind = "施法者学派", Prestige = 75, HeadquartersId = 4, Treasury = 1800, ReagentCap = 10, ReagentStock = 7 },
            new() { Id = 3, Name = "奥尔登家族", Kind = "贵族领主", Prestige = 85, HeadquartersId = 2, Treasury = 1200, ReagentCap = 5, ReagentStock = 4 },
            new() { Id = 4, Name = "自由市场行会", Kind = "行会商会", Prestige = 50, HeadquartersId = 1, Treasury = 600, ReagentCap = 12, ReagentStock = 8 },
            new() { Id = 5, Name = "鼠巷帮", Kind = "地下帮派", Prestige = 20, HeadquartersId = 1, Treasury = 220, ReagentCap = 6, ReagentStock = 4 }
        };
        db.Insertable(factions.Select(f => f.ToRow()).ToList()).ExecuteCommand();

        var random = new Random(20261103);
        var npcs = SeedNpcs(random, 300);
        AssignSites(npcs, sites, random);
        db.Insertable(npcs.Select(n => n.ToRow()).ToList()).ExecuteCommand();

        var relations = SeedRelations(npcs, random);
        db.Insertable(relations.Select(r => r.ToRow()).ToList()).ExecuteCommand();

        // 定时事件：L0 脚本主线 + L2 日历活动（到期前一回合以 Planned 入日志）
        var schedules = new List<EventSchedule>
        {
            new() { Id = 1, Kind = "Calendar", Name = "春分集市", DueTick = 2, PayloadJson = "{\"mood\":4,\"money\":3,\"tension\":\"药剂短缺\",\"urgency\":-6,\"localeId\":1}" },
            new() { Id = 2, Kind = "Calendar", Name = "灰塔年度评审", DueTick = 5, PayloadJson = "{\"mood\":2,\"tension\":\"资料分配不均\",\"urgency\":-8,\"localeId\":4}" },
            new() { Id = 3, Kind = "Calendar", Name = "丰收节", DueTick = 8, PayloadJson = "{\"mood\":8,\"money\":2,\"localeId\":5}" },
            new() { Id = 4, Kind = "Calendar", Name = "冬至祭", DueTick = 11, PayloadJson = "{\"mood\":6,\"health\":3,\"localeId\":2}" },
            new() { Id = 5, Kind = "Script", Name = "裁判所大清查", DueTick = 10, PayloadJson = "{\"tension\":\"隐匿暴露风险\",\"urgency\":25,\"mood\":-6,\"localeId\":1}" },
            new() { Id = 6, Kind = "Script", Name = "巨环遗迹异动", DueTick = 14, PayloadJson = "{\"tension\":\"药剂短缺\",\"urgency\":10,\"mood\":-3,\"localeId\":8}" }
        };
        db.Insertable(schedules.Select(s => s.ToRow()).ToList()).ExecuteCommand();

        // I06：四个初始矛盾
        var tensions = new List<Tension>
        {
            new() { Type = "隐匿暴露风险", Stakes = "平民区疑似藏有未登记的施法者，裁判所线人开始排查", Urgency = 45, DeadlineTick = 6 },
            new() { Type = "资料分配不均", Stakes = "灰塔分会低阶会员不满高阶文献只对评审成员开放", Urgency = 35, DeadlineTick = 12 },
            new() { Type = "爵位继承争议", Stakes = "奥尔登家两位继承人中仅一人血脉觉醒，家臣开始站队", Urgency = 55, DeadlineTick = 9 },
            new() { Type = "药剂短缺", Stakes = "河湾村热病蔓延，药剂师坐地起价，平民无力负担", Urgency = 65, DeadlineTick = 4 }
        };
        db.Insertable(tensions.Select(t => t.ToRow()).ToList()).ExecuteCommand();
    }

    /// <summary>把每个人分配到其地点内的具体现场：施法者去仪式厅、工匠去工坊、行商去市场、学者去文书库。</summary>
    private static void AssignSites(List<Npc> npcs, List<Site> sites, Random random)
    {
        var byLocale = sites.GroupBy(s => s.LocaleId).ToDictionary(g => g.Key, g => g.ToList());

        Site? Prefer(long localeId, string facility)
        {
            if (!byLocale.TryGetValue(localeId, out var list))
            {
                return null;
            }
            return list.FirstOrDefault(s => s.Has(facility)) ?? list[random.Next(list.Count)];
        }

        foreach (var npc in npcs)
        {
            var facility = npc.Path == "mage" ? Facility.Ritual
                : npc.Path == "knight" ? Facility.Ritual
                : npc.Role is "药剂师" or "铁匠" or "木匠" or "裁缝" or "学徒" or "拾荒者" ? Facility.Workshop
                : npc.Role is "行商" or "掮客" or "店员" or "古物贩子" ? Facility.Market
                : npc.Role is "研究者" or "评审成员" or "书记员" or "魔法学徒" or "高阶法师" ? Facility.Library
                : npc.Role is "神职者" or "审判官" ? Facility.Temple
                : npc.Role is "亡命徒" ? Facility.Tavern
                : npc.Role is "守遗迹人" ? Facility.Ritual // 守着内环古祭坛过夜
                : string.Empty;

            npc.SiteId = Prefer(npc.LocationId, facility)?.Id ?? 0;
        }
    }

    /// <summary>I02：300 独立 NPC。约七成平民与职员，一成行商与工匠，其余为施法者/骑士/神职/贵族/学者；高阶 ≤5%。</summary>
    private static List<Npc> SeedNpcs(Random random, int total)
    {
        var npcs = new List<Npc>(total);
        var id = 1L;
        var usedNames = new HashSet<string>();

        string NextName()
        {
            while (true)
            {
                var name = NameGen.Random(random);
                if (usedNames.Add(name))
                {
                    return name;
                }
            }
        }

        Npc Base(int locId, int factionId, int minAge, int maxAge, string role)
        {
            var con = random.Next(20, 81);
            var com = random.Next(20, 81);
            var phy = random.Next(20, 81);
            return new Npc
            {
                Id = id++,
                Name = NextName(),
                Sex = random.Next(2),
                BirthTick = -12 * random.Next(minAge, maxAge),
                FactionId = factionId,
                Role = role,
                LocationId = locId,
                Constitution = con,
                Comprehension = com,
                Charm = random.Next(20, 81),
                Physique = phy,
                Willpower = random.Next(20, 81),
                Health = random.Next(75, 101),
                Mood = random.Next(40, 85),
                Money = random.Next(8, 40),
                PersonalityJson = $"{{\"道义\":{random.Next(-20, 61)},\"野心\":{random.Next(0, 61)},\"好奇\":{random.Next(0, 81)},\"谨慎\":{random.Next(0, 81)}}}",
                NeedsJson = "{\"安全\":60,\"成长\":50,\"财富\":60,\"归属\":40,\"声望\":30,\"隐匿\":20}"
            };
        }

        // 200 平民与职员（约七成）
        var commonRoles = new[] { "店员", "书记员", "乐师", "码头工人", "农夫", "仆从", "学徒", "守夜人" };
        for (var i = 0; i < 200; i++)
        {
            var loc = i % 3 == 0 ? random.Next(5, 8) : 1;
            npcs.Add(Base(loc, loc == 1 ? 4 : 0, 16, 60, commonRoles[random.Next(commonRoles.Length)]));
        }

        // 30 行商与工匠（约一成）
        var craftRoles = new[] { "行商", "掮客", "药剂师", "铁匠", "木匠", "裁缝" };
        for (var i = 0; i < 30; i++)
        {
            var loc = random.Next(1, 8);
            var n = Base(loc, 4, 20, 55, craftRoles[random.Next(craftRoles.Length)]);
            n.Money = random.Next(30, 120);
            n.InventoryJson = "[\"基础材料\"]";
            npcs.Add(n);
        }

        // 20 施法者（学徒 → 高环，高阶占少数）
        for (var i = 0; i < 20; i++)
        {
            var high = i < 4; // 高阶 ≤ 300×5% 总量内
            var n = Base(4, 2, 20, 70, high ? "高阶法师" : "魔法学徒");
            n.Path = "mage";
            n.Realm = high ? random.Next(6, 9) : random.Next(0, 4);
            n.ArcanaRank = high ? random.Next(3, 5) : random.Next(0, 2);
            n.Constitution = random.Next(50, 91);
            n.Comprehension = random.Next(60, 96);
            n.Willpower = random.Next(50, 91);
            n.Power = n.Realm * 18 + random.Next(0, 25);
            n.ArcanaCredit = n.Realm * 40 + random.Next(0, 60);
            n.Money = high ? random.Next(80, 200) : random.Next(15, 70);
            n.Cultivation = random.Next(0, 70);
            n.ApBudget = high ? 4 : 2;
            n.Authority = high ? 40 : 5;
            npcs.Add(n);
        }

        // 12 骑士与侍从（血脉觉醒是位阶硬前提）
        for (var i = 0; i < 12; i++)
        {
            var high = i < 2;
            var n = Base(high ? 2 : 3, 3, 18, 50, high ? "辉光骑士" : "骑士侍从");
            n.Path = "knight";
            n.BloodlineAwakened = i < 10; // 最后两名侍从尚未觉醒，进不了骑士位阶
            n.KnightRank = n.BloodlineAwakened ? (high ? 4 : random.Next(1, 3)) : 0;
            n.Physique = random.Next(60, 96);
            n.Power = n.KnightRank * 22 + random.Next(0, 30);
            n.Money = random.Next(20, 90);
            n.ApBudget = 2;
            n.InventoryJson = n.BloodlineAwakened ? "[\"进阶秘材\"]" : "[]";
            npcs.Add(n);
        }

        // 8 神职与执法者
        for (var i = 0; i < 8; i++)
        {
            var high = i < 2;
            var n = Base(3, 1, 22, 60, high ? "审判官" : "神职者");
            n.Authority = high ? 60 : 20;
            n.Willpower = random.Next(55, 95);
            n.Money = random.Next(20, 80);
            n.ApBudget = high ? 4 : 2;
            npcs.Add(n);
        }

        // 10 贵族（含两位继承人）
        for (var i = 0; i < 10; i++)
        {
            var n = Base(2, 3, 16, 70, i < 2 ? (i == 0 ? "长位继承人" : "次位继承人") : "贵族");
            n.BloodlineAwakened = i == 0;
            n.Path = i < 2 ? "knight" : "none";
            n.KnightRank = i == 0 ? 2 : 0;
            n.Money = random.Next(150, 500);
            n.Authority = random.Next(30, 70);
            n.Charm = random.Next(45, 90);
            n.Physique = random.Next(35, 80);
            n.Power = i == 0 ? 60 : random.Next(5, 40);
            npcs.Add(n);
        }

        // 10 学者与研究者
        for (var i = 0; i < 10; i++)
        {
            var n = Base(i % 2 == 0 ? 4 : 1, 2, 24, 65, i < 3 ? "评审成员" : "研究者");
            n.Comprehension = random.Next(55, 95);
            n.ArcanaRank = i < 3 ? random.Next(3, 5) : random.Next(0, 3);
            n.ArcanaCredit = n.ArcanaRank * 50 + random.Next(0, 80);
            n.Money = random.Next(20, 100);
            n.Authority = i < 3 ? 35 : 8;
            npcs.Add(n);
        }

        // 10 遗迹居民：拾荒者、古物贩子、守遗迹人，以及两个没登记的野法师。
        // 遗迹不是无人区，而是有自己的一套活法；野法师进不了组织的发放名单，只能自己刨。
        var ruinRoles = new[] { "拾荒者", "拾荒者", "古物贩子", "守遗迹人", "野法师" };
        for (var i = 0; i < 10; i++)
        {
            var role = ruinRoles[i % ruinRoles.Length];
            var wildMage = role == "野法师";
            var n = Base(8, wildMage ? 0 : i % 2 == 0 ? 5 : 0, 18, 55, role);
            n.Willpower = random.Next(55, 96);
            n.Physique = random.Next(45, 85);
            n.Money = random.Next(2, 25);
            n.Mood = random.Next(30, 70);
            n.PersonalityJson = $"{{\"道义\":{random.Next(-40, 21)},\"野心\":{random.Next(10, 71)},\"好奇\":{random.Next(40, 96)},\"谨慎\":{random.Next(10, 71)}}}";

            if (wildMage)
            {
                // 野路子施法者：在内环古祭坛摸到门径，熬了多年，只差一炉仪式
                n.Path = "mage";
                n.Realm = random.Next(2, 4);
                n.Constitution = random.Next(55, 90);
                n.Power = n.Realm * 16 + random.Next(0, 20);
                n.Cultivation = random.Next(84, 96);
                n.Money = random.Next(45, 80);
                n.InventoryJson = "[\"进阶秘材\"]"; // 自己从遗迹里刨出来的，舍不得上交也没处上交
            }
            else if (i % 3 == 0)
            {
                n.InventoryJson = "[\"进阶秘材\"]";
            }
            npcs.Add(n);
        }

        // 标记主要人物（界面展示）：贵族继承人、审判官、评审成员、高阶法师、辉光骑士、药剂师等
        var mainRoles = npcs.Where(n =>
            n.Role is "长位继承人" or "次位继承人" or "审判官" or "评审成员" or "高阶法师" or "辉光骑士"
            or "药剂师" or "行商").Take(24).ToList();
        foreach (var n in mainRoles)
        {
            n.IsMain = true;
        }

        return npcs;
    }

    /// <summary>I05：家庭/师徒/雇佣/同僚为主，少量隐秘同盟与告发关系。</summary>
    private static List<Relation> SeedRelations(List<Npc> npcs, Random random)
    {
        var relations = new List<Relation>();

        bool HasPair(long a, long b) => relations.Any(r => r.FromId == a && r.ToId == b);

        void Add(long from, long to, string kind, int affinity, int trust, int debt = 0, string flags = "")
        {
            if (from != to && !HasPair(from, to))
            {
                relations.Add(new Relation
                {
                    FromId = from, ToId = to, Kind = kind, Affinity = affinity, Trust = trust,
                    Debt = debt, LastContactTick = 0, Flags = flags
                });
            }
        }

        var byRole = npcs.GroupBy(n => n.Role).ToDictionary(g => g.Key, g => g.ToList());

        // 同业者互识（同僚）
        foreach (var group in byRole.Values)
        {
            for (var i = 0; i < Math.Min(group.Count - 1, 6); i++)
            {
                var a = group[i];
                var b = group[i + 1];
                Add(a.Id, b.Id, "colleague", random.Next(10, 50), random.Next(5, 40));
            }
        }

        var mages = byRole.GetValueOrDefault("魔法学徒", []);
        var scholars = byRole.GetValueOrDefault("研究者", []);
        for (var i = 0; i < 8 && i < mages.Count - 1; i++)
        {
            Add(mages[i + 1].Id, mages[i].Id, "mentor", random.Next(20, 60), random.Next(20, 50));
        }
        for (var i = 0; i < 6 && i < scholars.Count - 1; i++)
        {
            Add(scholars[i + 1].Id, scholars[i].Id, "mentor", random.Next(10, 50), random.Next(10, 40));
        }

        // 贵族家臣与骑士效忠奥尔登家继承人
        var heir = npcs.FirstOrDefault(n => n.Role == "长位继承人");
        var heir2 = npcs.FirstOrDefault(n => n.Role == "次位继承人");
        var knights = byRole.GetValueOrDefault("骑士侍从", []);
        foreach (var knight in knights)
        {
            Add(knight.Id, heir?.Id ?? 0, "sworn", random.Next(20, 70), random.Next(20, 60));
            if (heir2 is not null && random.Next(2) == 0)
            {
                Add(knight.Id, heir2.Id, "colleague", random.Next(-20, 30), random.Next(0, 20));
            }
        }

        // 债务关系（欠债是底层张力来源）
        var commoners = byRole.GetValueOrDefault("店员", []);
        for (var i = 0; i < 10 && commoners.Count >= 2; i++)
        {
            var a = commoners[random.Next(commoners.Count)];
            var b = commoners[random.Next(commoners.Count)];
            Add(a.Id, b.Id, "debtor", random.Next(-30, 10), random.Next(-20, 10), random.Next(5, 25));
        }

        // 隐秘同盟与告发关系（少量，I05）
        var rat = byRole.GetValueOrDefault("守夜人", []);
        var rats = rat.Take(2).ToList();
        foreach (var informer in rats)
        {
            var target = mages.Skip(1).FirstOrDefault(m => m.Path == "mage") ?? mages.FirstOrDefault();
            if (target is not null)
            {
                Add(informer.Id, target.Id, "informer", random.Next(-40, -10), 0, 0, "secret");
            }
        }

        return relations;
    }
}

/// <summary>西幻风人名生成：音节拼装，去重由调用方保证。</summary>
internal static class NameGen
{
    private static readonly string[] Heads =
    [
        "艾", "阿", "贝", "布", "塞", "达", "德", "艾德", "菲", "弗", "格", "海", "伊", "约", "卡", "莱",
        "洛", "马", "莫", "尼", "奥", "佩", "罗", "萨", "塞", "托", "乌", "维", "沃", "泽"
    ];

    private static readonly string[] Tails =
    [
        "德里克", "伦特", "温莎", "米尔", "斯坦", "格罗夫", "伍德", "菲尔", "莱恩", "蒙特", "罗斯", "凡",
        "格斯", "莉娅", "安娜", "德拉", "贝丝", "西亚", "坦雅", "维娅", "诺克", "达姆", "格斯", "蒙特"
    ];

    private static readonly string[] Surnames =
    [
        "", "·灰岩", "·白杨", "·渡鸦", "·晨露", "·铁匠", "·河口", "·荆棘", "·冬风", "·灯塔"
    ];

    public static string Random(Random random) =>
        Heads[random.Next(Heads.Length)] + Tails[random.Next(Tails.Length)] + Surnames[random.Next(Surnames.Length)];
}
