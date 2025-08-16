# TreePassBot

TreePassBot 是一个基于 .NET 8 开发的审核机器人，旨在帮助管理员管理群组用户的审核、黑名单管理以及其他相关功能。该项目使用了依赖注入、配置绑定和异步编程等现代开发技术。

## 项目结构

```
TreePassBot
├── Data                  # 数据存储相关代码
│   ├── JsonDataStore.cs  # JSON 数据存储实现
│   └── Entities          # 数据实体
├── Handlers             # 事件处理器和命令处理器
│   ├── AdminCommands     # 管理员命令
│   ├── GroupMessageEventHandler.cs
│   ├── GroupRequestEventHandler.cs
│   └── PrivateMessageEventHandler.cs
├── Models               # 数据模型
│   └── BotConfig.cs      # 机器人配置模型
├── Services             # 服务层
│   ├── Interfaces        # 服务接口
│   ├── AuditService.cs   # 审核服务
│   ├── MessageService.cs # 消息服务
│   └── UserService.cs    # 用户服务
├── Program.cs           # 应用程序入口
└── appsettings.json     # 配置文件
```

## 配置项说明

配置项存储在 `appsettings.json` 文件中，主要包括以下内容：

- `BotQqId`: 机器人的 QQ 号。
- `AuditGroupId`: 审核群组的 ID。
- `MainGroupIds`: 主群组的 ID 列表。
- `AuditorQqIds`: 审核员的 QQ 号列表。
- `AdminQqIds`: 管理员的 QQ 号列表。
- `DataFile`: 数据存储文件的路径。

## 命令系统

命令系统位于 `Handlers\AdminCommands` 命名空间中，支持以下命令：

### 管理员命令

- **`.rand`**: 生成一个随机验证码。
  - **描述**: 生成一个随机验证码并检查其唯一性。
  - **权限**: Auditor, BotAdmin, GroupAdmin

- **`.check [QQ号]`**: 查询用户状态。
  - **描述**: 查询指定用户的状态，包括是否在黑名单中。
  - **权限**: Auditor, BotAdmin, GroupAdmin

- **`.add-user [QQ号]`**: 添加用户到待审核列表。
  - **描述**: 将指定用户添加到待审核列表。
  - **权限**: Auditor, BotAdmin, GroupAdmin

- **`.reset [QQ号]`**: 重置用户状态。
  - **描述**: 将指定用户重置为待审核状态。
  - **权限**: Auditor, BotAdmin, GroupAdmin

- **`.audit-help`**: 查看审核相关命令。
  - **描述**: 显示审核员可用的命令列表。
  - **权限**: Auditor, BotAdmin, GroupAdmin

- **`.add-black [QQ号]`**: 将用户添加到黑名单。
  - **描述**: 将指定用户添加到黑名单。
  - **权限**: Auditor, BotAdmin, GroupAdmin

- **`.rm-black [QQ号]`**: 将用户从黑名单中移除。
  - **描述**: 将指定用户从黑名单中移除。
  - **权限**: Auditor, BotAdmin, GroupAdmin

- **`.help`**: 显示所有的帮助信息。
  - **描述**: 显示所有可用命令的帮助信息。
  - **权限**: Auditor, BotAdmin, GroupAdmin

## 如何开发

1. **克隆项目**
   ```bash
   git clone <repository-url>
   cd TreePassBot
   ```

2. **安装依赖**
   确保已安装 .NET 8 SDK，然后运行：
   ```bash
   dotnet restore
   ```

3. **运行项目**
   使用以下命令运行项目：
   ```bash
   dotnet run
   ```

4. **修改代码**
   - 添加新命令：在 `Handlers\AdminCommands` 中创建新的命令方法，并使用 `[BotCommand]` 和 `[RequiredPremission]` 属性进行标注。
   - 修改配置：更新 `appsettings.json` 文件。

5. **测试项目**
   使用以下命令运行单元测试：
   ```bash
   dotnet test
   ```

6. **发布项目**
   使用以下命令发布项目：
   ```bash
   dotnet publish -c Release
   ```

## 贡献

欢迎提交 Issue 和 Pull Request 来改进此项目！