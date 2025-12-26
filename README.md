# OpenPrinterWeb

[中文版](./README_zh.md)

OpenPrinterWeb is a modern, cross-platform Blazor Server application designed for managing printers and print jobs over the network using the IPP (Internet Printing Protocol). It features a sleek UI built with MudBlazor, real-time status updates via SignalR, and robust JWT-based authentication.

## 🚀 Features

- **Real-time Print Management**: Live updates of printer status and active jobs using SignalR.
- **Modern UI**: Response and interactive interface powered by MudBlazor.
- **Secure by Design**: JWT authentication with persistent sessions and protected resource access (including static files).
- **Multi-language Support**: Fully localized into 9 languages (EN, ZH, JA, RU, DE, ES, IT, KO, FR).
- **Cross-platform**: Built on .NET 10.0, ready to run on Windows, Linux, or macOS via Docker.

## 🏗️ Architecture

- **Frontend**: Blazor Server with MudBlazor components and Vanilla CSS for custom styling.
- **Backend API**: ASP.NET Core with JWT Authentication middleware.
- **Real-time Communication**: SignalR hub for broadcasting printer updates.
- **Printer Integration**: `SharpIppNext` library for standard IPP communication.
- **Infrastructure**: Dockerized with support for graceful shutdowns (`SIGTERM`) and persistent data protection keys.
- **CI/CD**: GitHub Actions for automated building and publishing to GHCR.io.

## 🖨️ About IPP (Internet Printing Protocol)

IPP is a standard network protocol for remote printing and managing print jobs. Most modern network printers support IPP natively.

**My printer doesn't support IPP?**
If you have an older USB printer, you can still use this application by:
1. Connecting your printer via USB to a router, Raspberry Pi, or any Linux-based host.
2. Installing **CUPS** (Common Unix Printing System) on that host.
3. Sharing the printer through CUPS, which will provide an IPP URI (usually `ipp://<host-ip>:631/printers/<printer-name>`) compatible with OpenPrinterWeb.

## 📦 Deployment

### Prerequisites

- Docker installed
- Access to a network printer supporting IPP

### Docker Run

To run the application quickly with Docker (mounting a volume to persist security keys):

```bash
docker run -d \
  -p 5180:8080 \
  --name openprinterweb \
  -e "PrinterSettings__Uri=ipp://192.168.1.1:631/printers/Default" \
  -e "Passwords=yourpassword1,yourpassword2" \
  -e "JwtSettings__Secret=A_Long_Secure_Random_Secret_Key_32_Chars" \
  -v ./data:/app/data \
  ghcr.io/lginc/openprinterweb:latest
```

### Docker Compose

Alternatively, use `docker-compose.yml`:

```yaml
services:
  openprinterweb:
    image: ghcr.io/lginc/openprinterweb:latest
    ports:
      - "5180:8080"
    environment:
      - PrinterSettings__Uri=ipp://192.168.1.1:631/printers/Default
      - Passwords=admin123,user456
      - JwtSettings__Secret=your_secure_secret_here_32_chars_long
    volumes:
      - ./data:/app/data
    restart: unless-stopped
    security_opt:
      - no-new-privileges:true
```

## 🛠️ Configuration

Key settings in `appsettings.json` can be overridden via environment variables:

- `PrinterSettings__Uri`: The IPP URI of your printer.
- `Passwords`: Comma-separated list of allowed passwords for login.
- `JwtSettings__Secret`: A secure key used to sign JWT tokens. **Must be at least 32 characters (256 bits) long.**

> **Note**: Mounting `/app/data` is highly recommended to persist encryption keys and uploaded print files. Without this, users will be logged out and all uploaded documents will be lost every time the container restarts.

## 🌐 Localization

The application follows the browser's language settings but also provides a manual switcher in the top-right menu. Supported cultures: `en-US`, `zh-Hans`, `ja`, `ru`, `de`, `es`, `it`, `ko`, `fr`.

## 📜 License

This project is licensed under the MIT License.
