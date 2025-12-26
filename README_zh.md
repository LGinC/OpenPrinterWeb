# OpenPrinterWeb

OpenPrinterWeb 是一个现代化的、跨平台的 Blazor Server 应用程序，旨在通过 IPP（互联网打印协议）管理网络打印机和打印任务。它拥有基于 MudBlazor 的精美界面、通过 SignalR 实现的实时状态更新以及强大的 JWT 身份验证系统。

## 🚀 功能特性

- **实时打印管理**：使用 SignalR 实时同步打印机状态和当前打印任务。
- **现代 UI**：由 MudBlazor 驱动的自适应且交互性强的用户界面。
- **安全设计**：具备持久化会话的 JWT 身份验证，并保护所有敏感资源（包括静态文件）。
- **多语言支持**：原生支持 9 种语言（中、英、日、俄、德、西、意、韩、法）。
- **跨平台**：基于 .NET 10.0 构建，支持通过 Docker 在 Windows、Linux 或 macOS 上运行。

## 🏗️ 技术架构

- **前端**：Blazor Server 配合 MudBlazor 组件库，并使用原生 CSS 进行精细化样式调整。
- **后端 API**：集成 JWT 身份验证中间件的 ASP.NET Core。
- **实时通信**：SignalR Hub 用于广播打印机状态更新。
- **打印机集成**：使用 `SharpIppNext` 库实现标准 IPP 通信。
- **基础设施**：容器化支持，配置了优雅退出（Graceful Shutdown）以及数据保护密钥的持久化。
- **CI/CD**：GitHub Actions 自动构建并推送到 GHCR.io。

## 🖨️ 关于 IPP (互联网打印协议)

IPP 是用于远程打印和管理打印任务的标准网络协议。大多数现代网络打印机都原生支持 IPP。

**如果我的打印机不支持 IPP 怎么办？**
如果您使用的是旧式 USB 打印机，仍可以通过以下方式使用本项目：
1. 将打印机通过 USB 连接到路由器、树莓派或任何 Linux 主机。
2. 在该主机上安装 **CUPS** (Common Unix Printing System)。
3. 通过 CUPS 共享打印机，它将提供一个与 OpenPrinterWeb 兼容的 IPP URI（通常为 `ipp://<主机IP>:631/printers/<打印机名称>`）。

## 📦 部署指南

### 前置条件

- 已安装 Docker
- 拥有一台支持 IPP 协议的网络打印机

### Docker 运行

使用 Docker 快速启动应用（建议挂载卷以持久化安全密钥）：

```bash
docker run -d \
  -p 5180:8080 \
  --name openprinterweb \
  -e "PrinterSettings__Uri=ipp://192.168.1.1:631/printers/Default" \
  -e "Passwords=你的密码1,你的密码2" \
  -e "JwtSettings__Secret=一段足够长且安全的随机密钥_32位以上" \
  -v ./data:/app/data \
  ghcr.io/lginc/openprinterweb:latest
```

### Docker Compose

也可以使用 `docker-compose.yml` 进行部署：

```yaml
services:
  openprinterweb:
    image: ghcr.io/lginc/openprinterweb:latest
    ports:
      - "5180:8080"
    environment:
      - PrinterSettings__Uri=ipp://192.168.1.1:631/printers/Default
      - Passwords=admin123,user456
      - JwtSettings__Secret=一段足够长且安全的随机密钥_32位以上
    volumes:
      - ./data:/app/data
    restart: unless-stopped
    security_opt:
      - no-new-privileges:true
```

## 🛠️ 配置说明

`appsettings.json` 中的关键设置可以通过环境变量覆盖：

- `PrinterSettings__Uri`：打印机的 IPP URI。
- `Passwords`：登录所需的密码列表（逗号分隔）。
- `JwtSettings__Secret`：用于签署 JWT 令牌的安全密钥。**必须至少为 32 个字符（256 位）。**

> **注意**：强烈建议挂载 `/app/data` 目录。该目录用于持久化存储加密密钥和上传的待打印文件。如果不挂载，每次容器重启时所有登录状态和已上传文件都会丢失。

## 🌐 国际化

应用会根据浏览器设置自动选择语言，同时也支持在页面右上角的菜单中手动切换。支持的语言包括：`zh-Hans` (简体中文), `en-US`, `ja`, `ru`, `de`, `es`, `it`, `ko`, `fr`。

## 📜 开源协议

本项目基于 MIT 协议开源。
