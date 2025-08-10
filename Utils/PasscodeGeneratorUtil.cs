using TreePassBot.Data;

namespace TreePassBot.Utils;

public class PasscodeGeneratorUtil(JsonDataStore dataStore)
{
    public Task<string> GenerateUniquePasscodeAsync()
    {
        // 循环生成直到找到一个唯一的
        while (true)
        {
            var passcode = new string(Enumerable.Range(0, 10)
                .Select(_ => (char)Random.Shared.Next('0', '9' + 1))
                .ToArray());

            if (!dataStore.PasscodeExists(passcode))
            {
                return Task.FromResult(passcode);
            }
        }
    }
}