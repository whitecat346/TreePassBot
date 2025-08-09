using System.Text.Json;
using Microsoft.Extensions.Options;
using TreePassBot.Data.Entities;
using TreePassBot.Models;

namespace TreePassBot.Data;

public class JsonDataStore
{
    private readonly string _filePath;
    private readonly PendingUserData _data;
    private static readonly object Lock = new();

    public JsonDataStore(IOptions<BotConfig> config)
    {
        _filePath = config.Value.DataFile;
        _data = LoadData();
    }

    private PendingUserData LoadData()
    {
        lock (Lock)
        {
            if (!File.Exists(_filePath))
            {
                return new PendingUserData();
            }

            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize(
                       json, typeof(PendingUserData), PendingUserDataContext.Default) as PendingUserData
                   ?? new PendingUserData();
        }
    }

    private void SaveChange()
    {
        lock (Lock)
        {
            var json = JsonSerializer.Serialize(_data, typeof(PendingUserData), PendingUserDataContext.Default);
            File.WriteAllText(_filePath, json);
        }
    }

    public PendingUser? GetUserByQqId(ulong qqId)
    {
        lock (Lock)
        {
            return _data.Users.FirstOrDefault(u => u.QqId == qqId);
        }
    }

    public bool UserExists(ulong qqId)
    {
        lock (Lock)
        {
            return _data.Users.Any(u => u.QqId == qqId);
        }
    }

    public bool PasscodeExists(string passcode)
    {
        lock (Lock)
        {
            return _data.Users.Any(u => u.Passcode.Equals(passcode, StringComparison.OrdinalIgnoreCase));
        }
    }

    public void AddUser(PendingUser user)
    {
        lock (Lock)
        {
            if (_data.Users.Any(u => u.QqId == user.QqId))
            {
                return;
            }

            _data.Users.Add(user);
            SaveChange();
        }
    }

    public void UpdateUser(PendingUser user)
    {
        lock (Lock)
        {
            var existingUser = _data.Users.FirstOrDefault(u => u.QqId == user.QqId);
            if (existingUser == null)
            {
                return;
            }

            existingUser.Status = user.Status;
            existingUser.Passcode = user.Passcode;
            existingUser.UpdatedAt = DateTime.UtcNow;
            SaveChange();
        }
    }
}