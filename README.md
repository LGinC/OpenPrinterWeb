# OpenPrinterWeb

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
- **Infrastructure**: Dockerized with support for graceful shutdowns (`SIGTERM`).
- **CI/CD**: GitHub Actions for automated building and publishing to GHCR.io.

## 📦 Deployment

### Prerequisites

- Docker installed
- Access to a network printer supporting IPP

### Docker Run

To run the application quickly with Docker:

```bash
docker run -d \
  -p 5180:8080 \
  --name openprinterweb \
  -e "PrinterSettings__Uri=ipp://<your-printer-ip>:631/printers/Default" \
  -e "Passwords=yourpassword1,yourpassword2" \
  -e "JwtSettings__Secret=A_Long_Secure_Random_Secret_Key" \
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
      - PrinterSettings__Uri=ipp://192.168.1.100:631/printers/Default
      - Passwords=admin123,user456
      - JwtSettings__Secret=your_secure_secret_here
    restart: unless-stopped
    security_opt:
      - no-new-privileges:true
```

## 🛠️ Configuration

Key settings in `appsettings.json` can be overridden via environment variables:

- `PrinterSettings__Uri`: The IPP URI of your printer.
- `Passwords`: Comma-separated list of allowed passwords for login.
- `JwtSettings__Secret`: A secure key used to sign JWT tokens.

## 🌐 Localization

The application follows the browser's language settings but also provides a manual switcher in the top-right menu. Supported cultures: `en-US`, `zh-Hans`, `ja`, `ru`, `de`, `es`, `it`, `ko`, `fr`.

## 📜 License

This project is licensed under the MIT License.
