using System.Text.Json;
using GWE.Domain.Entities;

namespace GWE.Service.Turn;

/// <summary>背包 JSON 读写（inventory_json）。</summary>
public static class Inventory
{
    public static List<string> Load(Npc npc)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(npc.InventoryJson) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static bool Has(Npc npc, string item) => Load(npc).Contains(item);

    public static int Count(Npc npc, string item) => Load(npc).Count(i => i == item);

    public static void Add(Npc npc, string item)
    {
        var items = Load(npc);
        items.Add(item);
        npc.InventoryJson = JsonSerializer.Serialize(items);
    }

    public static void Remove(Npc npc, string item)
    {
        var items = Load(npc);
        if (items.Remove(item))
        {
            npc.InventoryJson = JsonSerializer.Serialize(items);
        }
    }
}
