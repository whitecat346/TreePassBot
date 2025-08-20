using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TreePassBot.Data.Entities;
using TreePassBot.Models;
using Timer = System.Threading.Timer;

namespace TreePassBot.Data;

public sealed class JsonDataStore : IDisposable
{
    private readonly string _filePath;
    private readonly ILogger<JsonDataStore> _logger;

    // 使用读写锁替代全局锁，提高并发读取性能
    private readonly ReaderWriterLockSlim _rwLock = new();

    // 内存中的数据缓存，使用线程安全的集合
    private readonly ConcurrentDictionary<ulong, UserInfo> _users = new();
    private readonly ConcurrentHashSet<ulong> _blackList = new();

    // 用于批量写入的变更跟踪
    private readonly ConcurrentQueue<DataChange> _pendingChanges = new();
    private volatile bool _hasChanges;

    // 定时器和控制
    private readonly Timer _expireTimer;
    private readonly Timer _saveTimer;
    private volatile bool _disposed;
    private readonly SemaphoreSlim _saveSemaphore = new(1, 1);

    // 配置参数
    private readonly TimeSpan _saveInterval = TimeSpan.FromSeconds(5);    // 批量保存间隔
    private readonly TimeSpan _expireInterval = TimeSpan.FromSeconds(10); // 过期检查间隔

    public JsonDataStore(IOptions<BotConfig> config, ILogger<JsonDataStore> logger)
    {
        _logger = logger;
        _filePath = config.Value.DataFile;

        // 初始加载数据
        LoadDataAsync().GetAwaiter().GetResult();

        // 启动定时器
        _expireTimer = new Timer(ExpireTimeOutPasscode, null, _expireInterval, _expireInterval);
        _saveTimer = new Timer(SaveChangesIfNeeded, null, _saveInterval, _saveInterval);
    }

    private async Task LoadDataAsync()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return;
            }

            var json = await File.ReadAllTextAsync(_filePath).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            if (JsonSerializer.Deserialize(json, typeof(UserData), UserDataContext.Default) is UserData userData)
            {
                // 加载到内存缓存
                foreach (var user in userData.Users)
                {
                    _users.TryAdd(user.QqId, user);
                }

                foreach (var blackUserId in userData.BlackList)
                {
                    _blackList.Add(blackUserId);
                }
            }

            _logger.LogInformation("Loaded {UserCount} users and {BlackListCount} blacklisted users",
                                   _users.Count, _blackList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Error loading data file {FilePath}: {Message}", _filePath, ex.Message);
        }
    }

    private void ExpireTimeOutPasscode(object? state)
    {
        if (_disposed) return;

        var currentTime = DateTime.UtcNow;
        var expiredUsers = new List<UserInfo>();

        // 使用读锁检查过期用户
        _rwLock.EnterReadLock();
        try
        {
            foreach (var kvp in _users)
            {
                var user = kvp.Value;
                if (user.Status != AuditStatus.Expired && user.ExpriedAt < currentTime)
                {
                    expiredUsers.Add(user);
                }
            }
        }
        finally
        {
            _rwLock.ExitReadLock();
        }

        // 批量更新过期用户
        if (expiredUsers.Count <= 0) return;

        _rwLock.EnterWriteLock();
        try
        {
            foreach (var user in expiredUsers)
            {
                if (_users.TryGetValue(user.QqId, out var currentUser) &&
                    currentUser.Status != AuditStatus.Expired &&
                    currentUser.ExpriedAt < currentTime)
                {
                    currentUser.Status = AuditStatus.Expired;
                    currentUser.Passcode = string.Empty;
                    currentUser.UpdatedAt = currentTime;

                    _pendingChanges.Enqueue(new DataChange
                    {
                        Type = ChangeType.Update,
                        User = currentUser
                    });

                    _logger.LogInformation("Expired user {UserId} at {Time}", user.QqId, currentTime);
                }
            }

            _hasChanges = true;
        }
        finally
        {
            _rwLock.ExitWriteLock();
        }
    }

    private void SaveChangesIfNeeded(object? state)
    {
        if (_disposed || !_hasChanges) return;

        _ = SaveChangesAsync();
    }

    private async Task SaveChangesAsync()
    {
        if (!await _saveSemaphore.WaitAsync(100).ConfigureAwait(false))
            return;

        try
        {
            if (_disposed || !_hasChanges) return;

            _rwLock.EnterReadLock();
            UserData userData;
            try
            {
                userData = new UserData
                {
                    Users = _users.Values.ToList(),
                    BlackList = _blackList.ToList()
                };
            }
            finally
            {
                _rwLock.ExitReadLock();
            }

            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(userData, typeof(UserData), UserDataContext.Default);

            // 原子写入
            var tempPath = _filePath + ".tmp";
            await File.WriteAllTextAsync(tempPath, json).ConfigureAwait(false);
            File.Move(tempPath, _filePath, true);

            _hasChanges = false;

            // 清空变更队列
            while (_pendingChanges.TryDequeue(out _))
            {
            }

            _logger.LogDebug("Data saved successfully to {FilePath}", _filePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Error saving data to {FilePath}: {Message}", _filePath, ex.Message);

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
                // ignored
            }
        }
        finally
        {
            _saveSemaphore.Release();
        }
    }

    public UserInfo? GetUserByQqId(ulong qqId)
    {
        if (_disposed) return null;

        _rwLock.EnterReadLock();
        try
        {
            return _users.TryGetValue(qqId, out var user) ? CloneUser(user) : null;
        }
        finally
        {
            _rwLock.ExitReadLock();
        }
    }

    public bool UserExists(ulong qqId)
    {
        if (_disposed)
        {
            return false;
        }

        // 使用ConcurrentDictionary的线程安全方法，无需锁
        return _users.ContainsKey(qqId);
    }

    public bool PasscodeExists(string passcode)
    {
        if (_disposed || string.IsNullOrWhiteSpace(passcode))
        {
            return false;
        }

        _rwLock.EnterReadLock();
        try
        {
            return _users.Values.Any(u => u.Passcode.Equals(passcode, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            _rwLock.ExitReadLock();
        }
    }

    public bool AddUser(UserInfo userInfo)
    {
        if (_disposed)
        {
            return false;
        }

        var newUser = new UserInfo
        {
            QqId = userInfo.QqId,
            Status = userInfo.Status,
            Passcode = userInfo.Passcode,
            CreatedAt = userInfo.CreatedAt == default ? DateTime.UtcNow : userInfo.CreatedAt,
            UpdatedAt = DateTime.UtcNow,
            ExpriedAt = userInfo.ExpriedAt
        };

        if (_users.TryAdd(newUser.QqId, newUser))
        {
            _pendingChanges.Enqueue(new DataChange
            {
                Type = ChangeType.Add,
                User = newUser
            });
            _hasChanges = true;
            return true;
        }

        return false;
    }

    public void AddToBlackList(ulong qqId)
    {
        if (_disposed) return;

        if (_blackList.Add(qqId))
        {
            _pendingChanges.Enqueue(new DataChange
            {
                Type = ChangeType.BlacklistAdd,
                QqId = qqId
            });
            _hasChanges = true;
        }
    }

    public bool IsInBlackList(ulong qqId)
    {
        if (_disposed) return false;

        // ConcurrentHashSet是线程安全的，无需锁
        return _blackList.Contains(qqId);
    }

    public void RemoveFromBlackList(ulong qqId)
    {
        if (_disposed) return;

        if (_blackList.TryRemove(qqId))
        {
            _pendingChanges.Enqueue(new DataChange
            {
                Type = ChangeType.BlacklistRemove,
                QqId = qqId
            });
            _hasChanges = true;
        }
    }

    public bool UpdateUser(UserInfo userInfo)
    {
        if (_disposed) return false;

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        return _users.AddOrUpdate(
            userInfo.QqId,
            userInfo, // 如果不存在则添加
            (_, existingUser) =>
            {
                // 更新现有用户
                existingUser.Status = userInfo.Status;
                existingUser.Passcode = userInfo.Passcode;
                existingUser.UpdatedAt = DateTime.UtcNow;
                existingUser.ExpriedAt = userInfo.ExpriedAt;

                _pendingChanges.Enqueue(new DataChange
                {
                    Type = ChangeType.Update,
                    User = existingUser
                });
                _hasChanges = true;

                return existingUser;
            }) != null;
    }

    public bool DeleteUser(ulong qqId)
    {
        if (_disposed) return false;

        if (_users.TryRemove(qqId, out _))
        {
            _pendingChanges.Enqueue(new DataChange
            {
                Type = ChangeType.Delete,
                QqId = qqId
            });
            _hasChanges = true;
            return true;
        }

        return false;
    }

    public List<UserInfo> GetAllUsers()
    {
        if (_disposed) return [];

        _rwLock.EnterReadLock();
        try
        {
            return _users.Values.Select(CloneUser).ToList();
        }
        finally
        {
            _rwLock.ExitReadLock();
        }
    }

    public int GetUserCount()
    {
        return _disposed ? 0 : _users.Count;
    }

    // 手动触发保存
    public async Task SaveNowAsync()
    {
        if (!_disposed && _hasChanges)
        {
            await SaveChangesAsync().ConfigureAwait(false);
        }
    }

    private static UserInfo CloneUser(UserInfo userInfo)
    {
        return new UserInfo
        {
            QqId = userInfo.QqId,
            Status = userInfo.Status,
            Passcode = userInfo.Passcode,
            CreatedAt = userInfo.CreatedAt,
            UpdatedAt = userInfo.UpdatedAt,
            ExpriedAt = userInfo.ExpriedAt
        };
    }

    public void Dispose()
    {
        Dispose(true);
        // ReSharper disable once GCSuppressFinalizeForTypeWithoutDestructor
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _disposed = true;

            try
            {
                _expireTimer.Dispose();
                _saveTimer.Dispose();

                // 最后保存一次数据
                if (_hasChanges)
                {
                    SaveChangesAsync().GetAwaiter().GetResult();
                }

                _rwLock.Dispose();
                _saveSemaphore.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error during dispose: {Message}", ex.Message);
            }
        }
    }
}

// 变更跟踪相关类
public class DataChange
{
    public ChangeType Type { get; set; }
    public UserInfo? User { get; set; }
    public ulong QqId { get; set; }
}

public enum ChangeType
{
    Add,
    Update,
    Delete,
    BlacklistAdd,
    BlacklistRemove
}

// 线程安全的HashSet实现
public class ConcurrentHashSet<T> where T : notnull
{
    private readonly ConcurrentDictionary<T, byte> _dictionary = new();

    public bool Add(T item) => _dictionary.TryAdd(item, 0);

    public bool Contains(T item) => _dictionary.ContainsKey(item);

    public bool TryRemove(T item) => _dictionary.TryRemove(item, out _);

    public int Count => _dictionary.Count;

    public List<T> ToList() => _dictionary.Keys.ToList();
}