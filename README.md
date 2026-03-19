<div align="center">

<img src="https://img.shields.io/badge/Discord%20Token%20Checker-v1.2.0-5865F2?style=for-the-badge&logo=discord&logoColor=white"/>
<img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white"/>
<img src="https://img.shields.io/badge/Platform-Windows-0078D6?style=for-the-badge&logo=windows&logoColor=white"/>
<img src="https://img.shields.io/badge/License-MIT-green?style=for-the-badge"/>

<br/>
<br/>

**⚡ Discord Token Checker** is a fast, multi-threaded WPF application for validating Discord tokens and extracting detailed account information.

</div>

---

## 📸 Preview

> *[Screenshot here](https://i.imgur.com/v8h39l6.png)*

---

## ✨ Features

| Feature | Details |
|---|---|
| 🔍 **Token Validation** | Checks validity using Discord's API v9 |
| 👤 **Account Info** | Username, Email, Phone, Locale, Verified status |
| 💜 **Nitro Detection** | Type (None / Basic / Nitro) + since when |
| 🔐 **2FA Check** | Detects if two-factor auth is enabled |
| 🔗 **Connected Accounts** | Spotify, Steam, and more |
| 💳 **Payment Info** | Card brand + last 4 digits, or PayPal email |
| 🎟️ **Nitro Entitlements** | Detects unclaimed gifts and subscriptions |
| 🌐 **Proxy Support** | `IP:PORT` or `IP:PORT:USER:PASS` format |
| 🔓 **Proxyless Mode** | Works out of the box without proxies |
| 📣 **Discord Webhook** | Rich embed notification on every hit |
| 📬 **Telegram Bot** | Instant hit notifications to your chat/channel |
| 📊 **Live Stats** | Valid / Invalid / Errors / CPM counter |
| 💾 **Export** | Export hits or all results to `.txt` |
| ⚡ **Multi-threaded** | 1–100 threads, adjustable via slider |

---

## 🚀 Getting Started

### Prerequisites

- Windows 10 / 11
- [.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

### Installation

1. Go to the [Releases](../../releases) page
2. Download the latest `.zip`
3. Extract and run `DiscordTokenChecker.exe`

---

## 📖 Usage

### 1. Load Tokens
Click **📂 Load Tokens** and select a `.txt` file with one token per line.

### 2. Load Proxies *(optional)*
Click **🌐 Load Proxies** and select a `.txt` file. Supported formats:
```
IP:PORT
IP:PORT:USERNAME:PASSWORD
```
If no proxies are loaded, the tool runs in **proxyless mode** automatically.

### 3. Configure Notifications *(optional)*

- **Discord Webhook** – paste your webhook URL in the field
- **Telegram** – paste your Bot Token and Chat ID

### 4. Start
Set the thread count with the slider and click **▶ Start Checking**.

### 5. Export Results
Use **💾 Export Hits** to save valid tokens, or **📋 Export All** for everything.

---

## 📤 Notification Examples

### Discord Webhook (Rich Embed)
Each valid token triggers an embed with all extracted account data sent directly to your Discord server.

### Telegram Bot
Each valid token sends a formatted message to your Telegram chat or channel.

---

## ⚙️ Built With

- [C# / .NET 8](https://dotnet.microsoft.com/)
- [WPF](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/) – UI Framework
- `System.Net.Http` – HTTP requests
- `System.Text.Json` – JSON parsing

---

## ⚠️ Disclaimer

This tool is provided for **educational purposes only**. The author is not responsible for any misuse. Only use on accounts you own or have explicit permission to test.

---

<div align="center">

Made by **pierdas** &nbsp;•&nbsp; v1.2.0

</div>
