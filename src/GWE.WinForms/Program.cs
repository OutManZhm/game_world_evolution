using Autofac;
using GWE.Repository;
using GWE.Repository.Repositories;
using GWE.Service;
using GWE.Service.Turn;
using GWE.WinForms.UI;

namespace GWE.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        var databasePath = Path.Combine(AppContext.BaseDirectory, "data", "gwe.db");
        var builder = new ContainerBuilder();

        // 数据层
        builder.RegisterInstance(new SqlSugarDbSession(databasePath)).As<IDbSession>().SingleInstance();
        builder.RegisterType<DatabaseInitializer>().AsSelf().SingleInstance();
        builder.RegisterType<WorldRepository>().As<IWorldRepository>().InstancePerLifetimeScope();
        builder.RegisterType<NpcRepository>().As<INpcRepository>().InstancePerLifetimeScope();
        builder.RegisterType<FactionRepository>().As<IFactionRepository>().InstancePerLifetimeScope();
        builder.RegisterType<LocaleRepository>().As<ILocaleRepository>().InstancePerLifetimeScope();
        builder.RegisterType<SiteRepository>().As<ISiteRepository>().InstancePerLifetimeScope();
        builder.RegisterType<RegionRepository>().As<IRegionRepository>().InstancePerLifetimeScope();
        builder.RegisterType<RelationRepository>().As<IRelationRepository>().InstancePerLifetimeScope();
        builder.RegisterType<EventRepository>().As<IEventRepository>().InstancePerLifetimeScope();
        builder.RegisterType<TensionRepository>().As<ITensionRepository>().InstancePerLifetimeScope();
        builder.RegisterType<NpcTurnRepository>().As<INpcTurnRepository>().InstancePerLifetimeScope();
        builder.RegisterType<EventScheduleRepository>().As<IEventScheduleRepository>().InstancePerLifetimeScope();
        builder.RegisterType<MemoryRepository>().As<IMemoryRepository>().InstancePerLifetimeScope();
        builder.RegisterType<BeliefRepository>().As<IBeliefRepository>().InstancePerLifetimeScope();
        builder.RegisterType<KnowledgeDeliveryRepository>().As<IKnowledgeDeliveryRepository>().InstancePerLifetimeScope();

        // 业务逻辑层
        builder.RegisterType<TurnService>().As<ITurnService>().InstancePerLifetimeScope();
        builder.RegisterType<NpcQueryService>().As<INpcQueryService>().InstancePerLifetimeScope();

        // 显示层
        builder.RegisterType<MainForm>().AsSelf();

        using var container = builder.Build();
        container.Resolve<DatabaseInitializer>().Initialize();

        if (args.Contains("--smoke"))
        {
            var turns = 3;
            var index = Array.IndexOf(args, "--turns");
            if (index >= 0 && index + 1 < args.Length && int.TryParse(args[index + 1], out var parsed))
            {
                turns = parsed;
            }
            RunSmoke(container, turns);
            return;
        }

        Application.Run(container.Resolve<MainForm>());
    }

    /// <summary>无界面自检：连跑 N 回合（默认 3），校验资源消耗、状态推进与主窗体数据绑定。</summary>
    private static void RunSmoke(Autofac.ILifetimeScope container, int turns)
    {
        var query = container.Resolve<INpcQueryService>();
        var turn = container.Resolve<ITurnService>();

        Console.WriteLine($"初始: {query.GetClock().Text}");
        for (var i = 0; i < turns; i++)
        {
            var result = turn.Advance();
            Console.WriteLine($"回合 {result.Tick}: {result.Events.Count} 条事件");
        }

        Console.WriteLine("主要 NPC：");
        foreach (var npc in query.GetMainNpcs().Take(10))
        {
            Console.WriteLine($"  {npc.Name} [{npc.RankTitle}] {npc.Role} @ {npc.Location}·{npc.Site}（{npc.Facilities}） 战力{npc.Power} 进度{npc.Cultivation}/{npc.AdvanceThreshold} 秘材:{(npc.HasReagent ? "有" : "无")} 金钱{npc.Money}");
        }
        Console.WriteLine("近期事件：");
        foreach (var e in query.GetRecentEvents(6))
        {
            Console.WriteLine($"  [{e.Time}] {e.Summary}");
        }
        Console.WriteLine("张力：");
        foreach (var t in query.GetTensions())
        {
            Console.WriteLine($"  [{t.Urgency}] {t.Type}：{t.Stakes}");
        }

        Console.WriteLine("组织秘材：");
        foreach (var f in query.GetFactions())
        {
            Console.WriteLine($"  {f.Name}：库存 {f.ReagentStock}/{f.ReagentCap}，已发放 {f.ReagentIssued}，成员 {f.Members}，排队 {f.Awaiting}，金库 {f.Treasury}（{f.Status}）");
        }

        var sample = query.GetMainNpcs().FirstOrDefault();
        if (sample is not null)
        {
            var profile = query.GetProfile(sample.Id);
            Console.WriteLine($"档案（{profile.Name}）：记忆 {profile.Memories.Count} 条，信念 {profile.Beliefs.Count} 条");
            foreach (var m in profile.Memories.Take(3))
            {
                Console.WriteLine($"  记忆[{m.Kind}/{m.Weight}] {m.Summary}");
            }
            foreach (var b in profile.Beliefs.Take(3))
            {
                Console.WriteLine($"  信念[{b.Source}/{b.Confidence}] {b.Content}");
            }
        }

        var form = container.Resolve<MainForm>();
        form.Show();
        Application.DoEvents();
        Console.WriteLine($"UI 绑定: NPC 行 {form.NpcRowCount}，事件行 {form.EventRowCount}，张力行 {form.TensionRowCount}，组织行 {form.FactionRowCount}");
        form.Close();
        Console.WriteLine("SMOKE OK");
    }
}
