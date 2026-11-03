using System.Text.Json;
using GWE.Domain.Entities;

namespace GWE.Service.Turn;

/// <summary>读取 personality_json 中的性格轴数值（如 好奇/野心），缺省返回 fallback。</summary>
public static class Personality
{
    public static int Get(Npc npc, string key, int fallback = 0)
    {
        try
        {
            using var doc = JsonDocument.Parse(npc.PersonalityJson);
            if (doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty(key, out var value)
                && value.ValueKind == JsonValueKind.Number)
            {
                return value.GetInt32();
            }
        }
        catch (JsonException)
        {
            // 性格数据损坏时按无倾向处理，不阻断回合。
        }
        return fallback;
    }
}
