# ScoreManagerForSchool

[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Avalonia](https://img.shields.io/badge/Avalonia-11.0-purple)](https://avaloniaui.net/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20macOS%20%7C%20Linux-lightgrey)](https://github.com/tlnkmc/ScoreManagerForSchool/releases)

> 基于 .NET 8 和 Avalonia UI 构建的现代化跨平台学校成绩管理系统，为学校提供完整的学生成绩管理解决方案。

## ✨ 主要特性

### 🎯 成绩管理
- **智能录入**：支持快速成绩录入和批量导入
- **多样化评估**：支持传统分数、等级制和积分制评估
- **科目管理**：灵活的科目分组和权重设置
- **成绩统计**：实时统计分析和排名功能

### 👥 学生管理
- **信息管理**：完整的学生档案信息管理
- **班级组织**：按班级、年级分组管理
- **批量导入**：支持 CSV/Excel 格式数据导入
- **智能搜索**：基于拼音的中文姓名快速搜索

### 📊 数据分析
- **成绩统计**：班级、个人成绩统计报表
- **趋势分析**：成绩变化趋势和对比分析
- **排名管理**：支持多维度排名统计
- **数据导出**：支持多种格式的数据导出

### 🔒 安全保障
- **数据加密**：采用 AES-256 加密保护敏感数据
- **访问控制**：基于角色的权限管理
- **设备绑定**：防止未授权访问
- **数据备份**：自动化数据备份和恢复

### 🎨 用户体验
- **现代界面**：基于 Avalonia 的现代化 UI 设计
- **跨平台**：原生支持 Windows、macOS、Linux
- **响应式**：适配不同屏幕尺寸和分辨率
- **国际化**：支持多语言界面

## 🏗️ 技术架构

### 核心技术
- **UI 框架**：Avalonia 11.0 (跨平台桌面UI)
- **运行时**：.NET 8.0
- **架构模式**：MVVM (Model-View-ViewModel)
- **数据层**：JSON + CSV 混合存储
- **加密**：AES-256-CBC + PBKDF2
- **测试**：xUnit + Moq

### 项目结构
```
ScoreManagerForSchool/
├── ScoreManagerForSchool.Core/          # 核心业务逻辑
│   ├── Storage/                         # 数据存储层
│   │   ├── StudentStore.cs             # 学生数据管理
│   │   ├── EvaluationStore.cs          # 积分记录管理
│   │   ├── ClassStore.cs               # 班级管理
│   │   ├── TeacherStore.cs             # 教师管理
│   │   └── EnhancedEvaluationService.cs # 增强评价服务
│   ├── Security/                        # 安全模块
│   │   ├── CryptoUtil.cs               # 加密工具
│   │   ├── DeviceIdGenerator.cs        # 设备ID生成
│   │   └── SecurePinnedBuffer.cs       # 安全内存管理
│   └── Logging/                         # 日志系统
│       └── Logger.cs                   # 日志记录器
├── ScoreManagerForSchool.UI/            # 用户界面层
│   ├── ViewModels/                     # 视图模型
│   │   ├── MainWindowViewModel.cs      # 主窗口视图模型
│   │   ├── InfoEntryViewModel.cs       # 信息录入视图模型
│   │   ├── EvaluationListViewModel.cs  # 积分列表视图模型
│   │   └── StudentsViewModel.cs        # 学生管理视图模型
│   ├── Views/                          # 视图界面
│   │   ├── MainWindow.axaml            # 主窗口
│   │   ├── InfoEntryView.axaml         # 信息录入界面
│   │   ├── EvaluationListView.axaml    # 积分列表界面
│   │   └── StudentsListView.axaml      # 学生管理界面
│   └── Services/                       # 服务层
│       └── ErrorHandler.cs            # 错误处理服务
├── ScoreManagerForSchool.Tests/         # 单元测试
└── ScoreManagerForSchool.Cli/           # 命令行工具
```

## 🚀 安装使用

### 系统要求
- **操作系统**：Windows 10 1809+, macOS 10.15+, Linux (Ubuntu 18.04+ / CentOS 7+)
- **运行环境**：.NET 8 Runtime
- **内存要求**：最低 512MB RAM
- **存储空间**：最低 100MB 可用空间

### 快速安装

#### 从发布版本安装
1. 访问 [GitHub Releases](https://github.com/tlnkmc/ScoreManagerForSchool/releases)
2. 下载对应平台的压缩包：
   - Windows x64: `scoremgr-win-x64.zip`
   - Windows ARM64: `scoremgr-win-arm64.zip`  
   - macOS x64: `scoremgr-osx-x64.tar.gz`
   - macOS ARM64: `scoremgr-osx-arm64.tar.gz`
   - Linux x64: `scoremgr-linux-x64.tar.gz`

#### 运行程序
```bash
# Windows
解压并运行 ScoreManagerForSchool.UI.exe

# macOS/Linux
tar -xzf scoremgr-*.tar.gz
cd scoremgr/
./ScoreManagerForSchool.UI
```

### 从源码构建
```bash
# 克隆仓库
git clone https://github.com/tlnkmc/ScoreManagerForSchool.git
cd ScoreManagerForSchool

# 安装依赖
dotnet restore

# 构建项目
dotnet build --configuration Release

# 运行程序
dotnet run --project ScoreManagerForSchool.UI
```

## 📖 使用指南

### 首次使用
1. **初始设置**：首次启动时会引导您创建管理员密码
2. **导入数据**：使用"学生列表管理"导入学生CSV文件
3. **设置班级**：在"教师和班级管理"中配置班级信息
4. **开始录入**：使用"信息录入"开始记录学生积分

### 数据导入格式
学生CSV文件格式示例：
```csv
学号,姓名,班级
2024001,张三,一年级1班
2024002,李四,一年级1班
2024003,王五,一年级2班
```

### 积分筛选功能
- **班级筛选**：选择特定班级查看积分记录
- **时间筛选**：支持自定义日期范围或快速选择（今日、昨日、本周、本月）
- **分值筛选**：可单独查看负分记录
- **搜索功能**：支持学生姓名、班级、备注的模糊搜索

### 快速操作
- **默认积分**：新录入默认为2分，可自定义修改
- **批量录入**：支持粘贴多行文本进行批量处理
- **快速导航**：使用侧边栏快速切换功能模块

## 🔧 开发指南

### 开发环境要求
- Visual Studio 2022 或 JetBrains Rider
- .NET 8 SDK
- Git

### 本地开发
```bash
# 克隆并设置开发环境
git clone https://github.com/tlnkmc/ScoreManagerForSchool.git
cd ScoreManagerForSchool
dotnet restore

# 运行开发服务器
dotnet run --project ScoreManagerForSchool.UI

# 运行测试
dotnet test
```

### 代码结构
- **MVVM 模式**：使用 Model-View-ViewModel 架构
- **依赖注入**：使用构造函数注入处理依赖
- **错误处理**：统一的错误处理和日志记录
- **单元测试**：完整的单元测试覆盖

### 贡献指南
1. Fork 项目到您的 GitHub 账户
2. 创建功能分支 (`git checkout -b feature/amazing-feature`)
3. 提交更改 (`git commit -m 'Add some amazing feature'`)
4. 推送分支 (`git push origin feature/amazing-feature`)
5. 开启 Pull Request

## 📋 更新日志

### v1.0.0-beta (当前版本)
#### 新功能
- ✨ 全新的积分筛选系统：支持班级、时间、正负分多维度筛选
- ✨ 快速时间选择：一键选择今日、昨日、本周、本月
- ✨ 默认2分积分设置：优化录入体验
- ✨ 综合错误处理：全面的异常捕获和用户友好的错误提示
- ✨ 智能日志系统：按启动时间分割，存储在程序目录

#### 技术改进
- 🔧 重构错误处理架构，添加统一的错误管理服务
- 🔧 优化日志记录系统，支持多级别日志和自动文件管理
- 🔧 改进MVVM架构，增强代码可维护性
- 🔧 完善单元测试覆盖率

#### 修复
- 🐛 修复积分筛选条件的逻辑问题
- 🐛 优化中文拼音搜索性能
- 🐛 修复跨平台兼容性问题

### v1.0.0-alpha
#### 基础功能
- 📝 学生信息管理和CSV导入
- 📊 积分录入和统计功能
- 🔐 数据加密和安全认证
- 🎨 现代化Avalonia UI界面

## 🤝 支持

### 获取帮助
- **文档**：查看项目 Wiki 获取详细文档
- **问题反馈**：在 GitHub Issues 中提交问题
- **功能建议**：在 GitHub Discussions 中讨论新功能

### 社区
- **GitHub**：[项目主页](https://github.com/tlnkmc/ScoreManagerForSchool)
- **Issues**：[问题追踪](https://github.com/tlnkmc/ScoreManagerForSchool/issues)
- **Discussions**：[社区讨论](https://github.com/tlnkmc/ScoreManagerForSchool/discussions)

## 📄 许可证

本项目基于 [MIT 许可证](LICENSE) 开源。您可以自由使用、修改和分发本软件。

## 🙏 致谢

- [Avalonia UI](https://avaloniaui.net/) - 强大的跨平台UI框架
- [.NET](https://dotnet.microsoft.com/) - 优秀的开发平台
- [NPinyin](https://github.com/stulzq/NPinyin) - 中文拼音处理库
- 所有贡献者和使用者的支持

---

**学校积分管理系统** - 让积分管理更简单、更高效！

# macOS/Linux
tar -xzf scoremgr-osx-x64.tar.gz  # 或对应的文件名
cd ScoreManagerForSchool/
./ScoreManagerForSchool.UI
```

## 📖 使用指南

### 首次使用
1. **初始化设置**：首次启动会引导设置管理员密码和安全问题
2. **导入学生数据**：
   - 支持 CSV 格式：`班级,学号,姓名`
   - 支持 Excel 格式：第一列班级，第二列学号，第三列姓名
   - 程序会自动生成拼音索引用于搜索

### 基本操作
1. **学生管理**
   - 导入：`学生管理` → `导入` → 选择 CSV/Excel 文件
   - 搜索：支持学号、姓名、拼音搜索
   - 编辑：双击学生记录进行修改

2. **评价设置**
   - 创建方案：`评价管理` → `新建方案`
   - 设定分值：为不同行为设置正负积分
   - 应用到班级：选择适用的班级范围

3. **积分录入**
   - 快速录入：`积分录入` → 选择学生 → 选择评价项目
   - 批量操作：支持多学生同时录入相同积分
   - 历史查看：查看学生的积分变化记录

4. **数据导出**
   - 选择时间范围：今天/昨天/本周/自定义
   - 导出格式：CSV 文件，包含详细记录和汇总统计
   - 自动保存：导出文件保存在 `Documents/积分导出` 目录

## 🔧 高级功能

### 自动更新
- 程序支持自动检查更新
- 在 `设置` → `更新设置` 中配置更新源
- 支持 GitHub 直连、代理服务器等多种更新方式

### 数据管理
- **备份恢复**：定期备份数据到 `AppData/Roaming/ScoreManagerForSchool/backup`
- **密码重置**：通过安全问题重置管理员密码
- **数据清理**：清理历史数据和临时文件

### 快捷键
- `Ctrl+F`：快速搜索学生
- `Ctrl+N`：新建积分记录
- `Ctrl+E`：导出当前数据
- `Ctrl+S`：保存当前更改

## 🛡️ 安全说明

### 数据保护
- 所有敏感数据采用 AES-256-CBC 加密
- 密码使用 PBKDF2 进行哈希处理
- 设备绑定防止数据被恶意转移

### 隐私政策
- 本软件不会收集或上传任何用户数据
- 所有数据均本地存储，用户完全控制
- 更新检查仅发送版本号信息

## 🐛 问题排查

### 常见问题
1. **启动失败**：确认已安装 .NET 8 Runtime
2. **导入失败**：检查 CSV/Excel 文件格式是否正确
3. **搜索无结果**：确认已导入学生数据且格式正确
4. **更新失败**：检查网络连接或尝试更换更新源

### 日志文件
- Windows: `%APPDATA%\ScoreManagerForSchool\logs`
- macOS: `~/Library/Application Support/ScoreManagerForSchool/logs`
- Linux: `~/.config/ScoreManagerForSchool/logs`

## 📞 技术支持

### 报告问题
- GitHub Issues: [提交问题](https://github.com/tlnkmc/ScoreManagerForSchool/issues)
- 邮件联系: [support@example.com](mailto:support@example.com)

### 贡献代码
1. Fork 本项目
2. 创建功能分支：`git checkout -b feature/new-feature`
3. 提交更改：`git commit -am 'Add new feature'`
4. 推送分支：`git push origin feature/new-feature`
5. 创建 Pull Request

## 📄 开源协议

本项目采用 [MIT License](LICENSE) 开源协议。

## 🏗️ 技术架构

- **前端框架**: Avalonia 11 (跨平台 XAML UI)
- **运行时**: .NET 8
- **架构模式**: MVVM (Model-View-ViewModel)
- **数据存储**: JSON 文件 + AES 加密
- **拼音处理**: NPinyin 0.2.6321.26573
- **Excel 支持**: ExcelDataReader 3.7.0
- **测试框架**: xUnit

## 📚 更新日志

### 1.0.0-beta (2024-01-XX)
- ✨ 新增拼音搜索功能，支持汉字姓名的智能匹配
- ✨ 新增时间范围导出功能（今天/昨天/本周/自定义）
- 🐛 修复 Windows 特定 API 的跨平台兼容性问题
- 🐛 修复空值引用警告和未使用事件警告
- ⚡ 优化学生搜索性能，预生成拼音索引
- 🔧 改进更新服务的异常处理机制
- 📝 完善内部版本号管理和比较逻辑

### 计划中的功能
- 📊 更丰富的数据可视化图表
- 🌐 Web 管理界面支持
- 📱 移动端应用
- 🔗 与教务系统的 API 集成
- 🎨 自定义主题和界面布局

---

> 💡 **提示**: 这是测试版本，如遇到问题请及时反馈。正式版本将在测试完成后发布。

**感谢使用学校积分管理器！** 🎓
