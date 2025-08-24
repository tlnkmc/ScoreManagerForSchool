# Releases

这里包含跨平台的自包含分发包：

- Windows
  - scoremgr-win-x64.zip（解压后运行 `scoremgr.exe`）
  - scoremgr-win-arm64.zip（解压后运行 `scoremgr.exe`）
- Linux
  - scoremgr-linux-x64.tar.gz（解压后 `chmod +x ./scoremgr && ./scoremgr`）
- macOS
  - scoremgr-osx-x64.tar.gz（解压后 `chmod +x ./scoremgr && ./scoremgr`）
  - scoremgr-osx-arm64.tar.gz（同上）

注意事项：
- 首次运行将进入 OOBE（设置主密码与安全问题，并可选择导入学生/班级/方案）。
- Windows 可能需要取消 SmartScreen 阻止；macOS 可能需通过 Gatekeeper（系统设置 > 安全性与隐私）。
- Linux/macOS 上如果提示“无法打开文件”，请确认赋予执行权限：`chmod +x ./scoremgr`。
- 数据文件默认存储在应用数据目录（Database1.json 加密存储、students.json、classes.json、schemes.json、appconfig.json、secqa.json）。

版本历史：
- 2025-08-24 首次跨平台打包。
