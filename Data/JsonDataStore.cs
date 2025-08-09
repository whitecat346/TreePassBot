using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TreePassBot.Data.Entities;
using TreePassBot.Models;

namespace TreePassBot.Data;

public class JsonDataStore
{
    private readonly string _filePath;
    private AppData _data;
    private static readonly object _lock = new();

    public JsonDataStore(IOptions<BotConfig> config)
    {
        _filePath = config.Value.DataFile;
        LoadData();
    }

    private void LoadData()
    {
        lock (_lock)
        {
            if (!File.Exists(_filePath))
            {
                _data = new AppData();
                return;
            }

            var json = File.ReadAllText(_filePath);
            _data = JsonSerializer.Deserialize<AppData>(json) ?? new AppData();
        }
    }

    private void SaveChange()
    {
        lock (_lock)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_data, options);
            File.WriteAllText(_filePath, json);
        }
    }

    public PendingUser? GetUserByQqId(ulong qqId)
    {
        lock (_lock)
        {
            return _data.Users.FirstOrDefault(u => u.QqId == qqId);
        }
    }

    public bool UserExists(ulong qqId)
    {
        lock (_lock)
        {
            return _data.Users.Any(u => u.QqId == qqId);
        }
    }

    public bool PasscodeExists(string passcode)
    {
        lock (_lock)
        {
            return _data.Users.Any(u => u.Passcode.Equals(passcode, StringComparison.OrdinalIgnoreCase));
        }
    }

    public void AddUser(PendingUser user)
    {
        lock (_lock)
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
        lock (_lock)
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