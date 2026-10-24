# Player Tags Plus

把玩家姓名换成职业名，保留 Player Tags 原有功能，并补上这几个位置：

- 小队列表、团队列表
- 角色面板右上角
- Focus Target、Target of Target

PvP 不替换；五个位置可以分别开关。首次运行会复制原 Player Tags 配置，职业名沿用你的标签文字。

先停用原 Player Tags，再从个人插件仓库安装 Player Tags Plus。输入 `/playertagsplus` 打开设置。

仓库 / Repository: `https://raw.githubusercontent.com/weileliNS/DalamudPlugins/main/repo.json?channel=playertagsplus`

---

Replace player names with job names. This fork keeps Player Tags and adds party/alliance lists, the Character header, Focus Target and Target of Target.

Each location has its own switch. PvP is excluded. Existing Player Tags settings are copied on first launch; custom job text is reused.

Disable the original Player Tags before installing. Open settings with `/playertagsplus`.

## Build / 编译

Clone with submodules, install .NET 10, then build with your API 15 SDK / 克隆时包含子模块，使用 .NET 10 和 API 15 SDK：

`dotnet build PlayerTags -c Release -p:DalamudLibPath="path/to/Hooks/15.0.3.9"`

`dotnet run --project Tests` checks shortened names, ambiguous initials, target isolation and restoration / 检查姓名缩写、重名缩写、目标隔离及还原。

MIT. Based on [r00telement/PlayerTags](https://github.com/r00telement/PlayerTags) and [Pilzinsel64's maintained version](https://git.pilzinsel64.de/pilzinsel64/playertags). Upstream notes: [UPSTREAM.md](UPSTREAM.md).
