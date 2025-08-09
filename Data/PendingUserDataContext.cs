using System.Text.Json.Serialization;

namespace TreePassBot.Data;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(PendingUserData))]
internal partial class PendingUserDataContext : JsonSerializerContext
{
}