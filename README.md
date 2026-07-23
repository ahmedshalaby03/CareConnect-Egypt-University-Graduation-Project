<div align="center">

```
 ██╗  ██╗███████╗ █████╗ ██╗  ████████╗██╗  ██╗ ██████╗ █████╗ ██████╗ ███████╗
 ██║  ██║██╔════╝██╔══██╗██║  ╚══██╔══╝██║  ██║██╔════╝██╔══██╗██╔══██╗██╔════╝
 ███████║█████╗  ███████║██║     ██║   ███████║██║     ███████║██████╔╝█████╗  
 ██╔══██║██╔══╝  ██╔══██║██║     ██║   ██╔══██║██║     ██╔══██║██╔══██╗██╔══╝  
 ██║  ██║███████╗██║  ██║███████╗██║   ██║  ██║╚██████╗██║  ██║██║  ██║███████╗
 ╚═╝  ╚═╝╚══════╝╚═╝  ╚═╝╚══════╝╚═╝   ╚═╝  ╚═╝ ╚═════╝╚═╝  ╚═╝╚═╝  ╚═╝╚══════╝
```

### **Smart Healthcare Platform**
*One platform. Every patient. Every doctor. Every service.*

🎓 **Graduation Project** — Faculty of Computer Science

<br/>

![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core_MVC-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)
![OpenAI](https://img.shields.io/badge/OpenAI_GPT-412991?style=for-the-badge&logo=openai&logoColor=white)
![Bootstrap](https://img.shields.io/badge/Bootstrap-7952B3?style=for-the-badge&logo=bootstrap&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white)
![Entity Framework](https://img.shields.io/badge/Entity_Framework_Core-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)

</div>

---

## 📌 Overview

**Smart Healthcare Platform** is a comprehensive, AI-powered healthcare management system built as a graduation project. The platform bridges the gap between **patients**, **doctors**, **hospitals**, and **medical services** — all within a single unified system.

From booking appointments and managing health insurance digitally, to discovering nearby hospitals and getting AI-driven medical guidance — this platform reimagines how healthcare services are accessed and delivered.

---

## 🏗️ System Architecture

```
Smart-Healthcare-Platform/
├── Controllers/          # MVC Controllers (Patient, Doctor, Hospital, Admin, AI)
├── Models/               # Domain Models & ViewModels
├── Views/                # Razor Pages (MVC Views)
│   ├── Patient/
│   ├── Doctor/
│   ├── Hospital/
│   ├── BloodBank/
│   ├── Insurance/
│   └── Admin/
├── Services/             # Business Logic & External Integrations
│   ├── OpenAIService.cs  # GPT Integration
│   ├── LocationService.cs
│   └── NotificationService.cs
├── Data/                 # EF Core DbContext & Migrations
├── wwwroot/              # Static Files (CSS, JS, Images)
└── appsettings.json
```

---

## ⚡ Tech Stack

| Layer | Technology |
|---|---|
| **Framework** | ASP.NET Core MVC (.NET 8) |
| **Frontend** | Razor Pages, Bootstrap 5, HTML/CSS/JS |
| **Database** | Microsoft SQL Server |
| **ORM** | Entity Framework Core |
| **AI Integration** | OpenAI GPT API (ChatGPT) |
| **Authentication** | ASP.NET Identity + Role-based Auth |
| **Maps & Location** | Location-based Hospital Discovery |
| **Architecture** | MVC Pattern + Service Layer |

---

## ✨ Features

### 👤 Patient Portal
- 🔐 Secure registration, login, and profile management
- 📅 **Appointment Booking** — search and book doctors by specialty
- 🏥 **Hospital Discovery** — find nearby hospitals based on location
- 🩸 **Blood Bank** — request blood units by type and location
- 📋 **Digital Health Insurance** — submit and track insurance requests online
- ⭐ **Reviews & Ratings** — rate doctors and hospital services
- 🤖 **AI Medical Assistant** — get instant medical guidance powered by OpenAI GPT

### 👨‍⚕️ Doctor Portal
- 📆 Manage availability and appointment schedule
- 👁️ View patient appointment history
- ✅ Accept / Cancel appointment requests
- ⭐ Receive patient reviews and ratings

### 🏥 Hospital Portal
- 🗂️ Manage hospital profile and listed services
- 🩸 Manage blood bank inventory (available blood types & quantities)
- 📊 View incoming appointment and service requests

### 🛡️ Admin Dashboard
- 👥 Full user management (Patients, Doctors, Hospitals)
- ✅ Approve / Reject doctor and hospital registrations
- 📊 Platform-wide analytics and statistics
- 🗂️ Manage categories, specialties, and system settings

### 🤖 AI-Powered Medical Assistant
- Integrated with **OpenAI GPT API**
- Answers patient medical questions in natural language
- Suggests relevant specialties based on symptoms
- Available 24/7 within the platform

---

## 🚀 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server) (or LocalDB)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or VS Code
- OpenAI API Key → [platform.openai.com](https://platform.openai.com)

---

### ⚙️ Setup & Run

```bash
# 1. Clone the repository
git clone https://github.com/YOUR_USERNAME/Smart-Healthcare-Platform.git
cd Smart-Healthcare-Platform

# 2. Restore packages
dotnet restore

# 3. Configure your settings (see Configuration section below)

# 4. Apply database migrations
dotnet ef database update

# 5. Run the application
dotnet run
```

✅ App will be running at: **`https://localhost:7000`**

---

## ⚙️ Configuration

Update `appsettings.json` with your credentials:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=SmartHealthcareDb;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "OpenAI": {
    "ApiKey": "YOUR_OPENAI_API_KEY_HERE",
    "Model": "gpt-4o"
  }
}
```

> ⚠️ **Never commit your API key to GitHub.** Use environment variables or [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) in development.

### Using .NET User Secrets (Recommended)

```bash
dotnet user-secrets set "OpenAI:ApiKey" "your-api-key-here"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your-connection-string"
```

---

## 🔐 Roles & Access Control

| Role | Access |
|---|---|
| **Patient** | Book appointments, insurance, blood requests, AI assistant, reviews |
| **Doctor** | Manage schedule, view patients, respond to bookings |
| **Hospital** | Manage profile, blood bank, services |
| **Admin** | Full platform control, approvals, analytics |

---

## 🗄️ Database Schema (Key Entities)

```
Users (ASP.NET Identity)
    ├── Patients          → Appointments, InsuranceRequests, Reviews
    ├── Doctors           → Specialties, Schedules, Reviews
    └── Hospitals         → Services, BloodBank, Location

Appointments              → Patient ↔ Doctor
BloodBank                 → Hospital ↔ BloodRequests
HealthInsurance           → Patient → InsuranceRequest
Reviews                   → Patient → Doctor / Hospital
```

---

## 🤖 AI Integration — How It Works

```
[Patient types a medical question]
            │
            ▼
   [OpenAI GPT API Call]
   System Prompt: "You are a helpful medical assistant..."
   User Message: Patient's question
            │
            ▼
   [GPT Response received]
            │
            ▼
   [Displayed to patient in real-time]
   + Suggested specialty if applicable
```

The AI assistant is context-aware and always recommends consulting a real doctor for diagnosis.

---

## 📸 Screenshots

> *(Add screenshots of your platform here)*

| Dashboard | Appointment Booking | AI Assistant |
|---|---|---|
| ![Dashboard](screenshots/dashboard.png) | ![Booking](screenshots/booking.png) | ![AI](screenshots/ai.png) |

---

## 🧪 Seeded Test Accounts

After running migrations, use these to explore the platform:

| Role | Email | Password |
|---|---|---|
| Admin | `admin@healthcare.com` | `Admin@123` |
| Doctor | `doctor@healthcare.com` | `Doctor@123` |
| Patient | `patient@healthcare.com` | `Patient@123` |

> *(Update these with your actual seeded credentials)*

---

## 👥 Team

| Name | Role |
|---|---|
| Ahmed Saeed Shalaby | Full-Stack Developer |
| Eslam Salem | Full-Stack Developer |
| Abdelrahman Rabea | Full-Stack Developer |
| Abdelrahman Siam | Flutter Developer |
| Alaa Naser | Flutter Developer |
| Saif Omran | Cloud Architect |

---

## 📄 License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for details.

---

<div align="center">

**🎓 Graduation Project — Built with passion, purpose, and a lot of coffee ☕**

*Smart Healthcare Platform — Because healthcare should be accessible to everyone.*

⭐ If you found this project interesting, please give it a star!

</div>
