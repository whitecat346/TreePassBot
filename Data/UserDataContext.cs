using System.Text.Json.Serialization;

namespace TreePassBot.Data;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(UserData))]
internal partial class UserDataContext : JsonSerializerContext
{
}