using System.Text.Json;
using System.Timers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TreePassBot.Data.Entities;
using TreePassBot.Models;
using Timer = System.Timers.Timer;

namespace TreePassBot.Data;

public sealed class JsonDataStore : IDisposable
{
    private readonly string _filePath;
    private readonly UserData _data;
    private static readonly object Lock = new();
    private volatile bool _disposed;
    private readonly ILogger<JsonDataStore> _logger;
    private readonly Timer _timer = new(TimeSpan.TicksPerMinute);

    public JsonDataStore(IOptions<BotConfig> config, ILogger<JsonDataStore> logger)
    {
        _logger = logger;
        _filePath = config.Value.DataFile;

        _timer.AutoReset = true;
        _timer.Enabled = true;
        _timer.Elapsed += ExprieTimeOutPasscode;

        // Load data first before starting timer
        _data = LoadData();

        _timer.Start();
    }

    private void ExprieTimeOutPasscode(object? sender, ElapsedEventArgs e)
    {
        lock (Lock)
        {
            foreach (var pendingUser in _data.Users.Where(pendingUser => pendingUser.ExpriedAt < DateTime.UtcNow))
            {
                pendingUser.Status = AuditStatus.Expried;
                pendingUser.Passcode = string.Empty;
            }

            SaveChange();
        }
    }

    private UserData LoadData()
    {
        lock (Lock)
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    return new UserData();
                }

                var json = File.ReadAllText(_filePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new UserData();
                }

                return JsonSerializer.Deserialize(json, typeof(UserData), UserDataContext.Default) as UserData
                       ?? new UserData();
            }
            catch (Exception ex)
            {
                // 文件损坏时返回新实例而不是抛出异常
                _logger.LogWarning($"Error loading data file {_filePath}: {ex.Message}");
                return new UserData();
            }
        }
    }

    private void SaveChange()
    {
        lock (Lock)
        {
            if (_disposed) return;

            try
            {
                // 创建目录（如果不存在）
                var directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(_data, typeof(UserData), UserDataContext.Default);

                // 原子写入：先写入临时文件，然后移动到目标文件
                var tempPath = _filePath + ".tmp";
                File.WriteAllText(tempPath, json);
                File.Move(tempPath, _filePath, true);
            }
            catch (Exception ex)
            {
                // 记录保存错误，但不抛出异常
                _logger.LogWarning($"Error saving data to {_filePath}: {ex.Message}");

                // 清理临时文件
                try
                {
                    var tempPath = _filePath + ".tmp";
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
                catch
                {
                    // 忽略清理错误
                }
            }
        }
    }

    public PendingUser? GetUserByQqId(ulong qqId)
    {
        lock (Lock)
        {
            if (_disposed) return null;

            // 返回副本以避免外部修改影响内部数据
            var user = _data.Users.FirstOrDefault(u => u.QqId == qqId);
            if (user == null) return null;

            // 创建用户对象的副本
            return new PendingUser
            {
                QqId = user.QqId,
                Status = user.Status,
                Passcode = user.Passcode,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }
    }

    public bool UserExists(ulong qqId)
    {
        lock (Lock)
        {
            if (_disposed) return false;
            return _data.Users.Any(u => u.QqId == qqId);
        }
    }

    public bool PasscodeExists(string passcode)
    {
        lock (Lock)
        {
            if (_disposed) return false;

            if (string.IsNullOrWhiteSpace(passcode))
            {
                return false;
            }

            return _data.Users.Any(u => u.Passcode.Equals(passcode, StringComparison.OrdinalIgnoreCase));
        }
    }

    public bool AddUser(PendingUser user)
    {
        lock (Lock)
        {
            if (_disposed) return false;

            // 检查用户是否已存在
            if (_data.Users.Any(u => u.QqId == user.QqId))
            {
                return false;
            }

            // 创建用户副本并设置时间戳
            var newUser = new PendingUser
            {
                QqId = user.QqId,
                Status = user.Status,
                Passcode = user.Passcode,
                CreatedAt = user.CreatedAt == default ? DateTime.UtcNow : user.CreatedAt,
                UpdatedAt = DateTime.UtcNow,
            };

            _data.Users.Add(newUser);

            SaveChange();
            return true;
        }
    }

    public bool UpdateUser(PendingUser user)
    {
        lock (Lock)
        {
            if (_disposed) return false;

            var existingUser = _data.Users.FirstOrDefault(u => u.QqId == user.QqId);
            if (existingUser == null)
            {
                return false;
            }

            // 更新现有用户的属性
            existingUser.Status = user.Status;
            existingUser.Passcode = user.Passcode;
            existingUser.UpdatedAt = DateTime.UtcNow;

            SaveChange();
            return true;
        }
    }

    public bool DeleteUser(ulong qqId)
    {
        lock (Lock)
        {
            if (_disposed) return false;

            var user = _data.Users.FirstOrDefault(u => u.QqId == qqId);
            if (user == null)
            {
                return false;
            }

            _data.Users.Remove(user);

            SaveChange();
            return true;
        }
    }

    // 新增：获取所有用户的安全副本
    public List<PendingUser> GetAllUsers()
    {
        lock (Lock)
        {
            if (_disposed) return [];

            return _data.Users.Select(u => new PendingUser
            {
                QqId = u.QqId,
                Status = u.Status,
                Passcode = u.Passcode,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            }).ToList();
        }
    }

    // 新增：获取用户数量
    public int GetUserCount()
    {
        lock (Lock)
        {
            return _disposed ? 0 : _data.Users.Count;
        }
    }

    public void Dispose()
    {
        Dispose(true);
    }

    private void Dispose(bool disposing)
    {
        lock (Lock)
        {
            if (!_disposed && disposing)
            {
                // 最后保存一次数据
                try
                {
                    _timer.Stop();
                    SaveChange();
                }
                catch
                {
                    // 忽略dispose时的保存错误
                }

                _disposed = true;
            }
        }
    }
}