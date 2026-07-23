<<<<<<< HEAD

CareConnect Egypt

Academic healthcare platform. Step 1 — foundation and authentication only.

Appointments, insurance, blood bank, maps and AI are intentionally out of scope in this step.

Solution layout

CareConnectEgypt/
├── CareConnect.slnx
├── src/
│   ├── CareConnect.Domain          # Entities, role/claim constants (no dependencies)
│   ├── CareConnect.Application      # Interfaces, DTOs, validation, Result/ApiResponse
│   ├── CareConnect.Infrastructure   # EF Core, Identity, JWT, services, seeding
│   └── CareConnect.Api              # Controllers, auth/authz wiring, Swagger, Serilog
├── tests/
│   └── CareConnect.Api.IntegrationTests   # 37 end-to-end tests over an in-memory SQLite DB
└── careconnect-client               # Angular 21 standalone app

Prerequisites

.NET 10 SDK

SQL Server (LocalDB is fine for development)

Node.js 20+ and npm

dotnet-ef global tool: dotnet tool install --global dotnet-ef

Backend configuration

Secrets are not committed. For development, values are read from user secrets first,then appsettings.Development.json. The Jwt:Key, SuperAdmin:Email andSuperAdmin:Password used at runtime are set in user secrets (see below).

Required keys:

Key

Purpose

Example (development)

ConnectionStrings:DefaultConnection

SQL Server connection

Server=(localdb)\MSSQLLocalDB;Database=CareConnectEgypt;Trusted_Connection=True;TrustServerCertificate=True

Jwt:Key

HMAC signing key, ≥ 32 chars

a 64+ char random string

Jwt:Issuer / Jwt:Audience

token issuer / audience

CareConnectEgypt / CareConnectEgyptClient

SuperAdmin:Email

seeded admin login

admin@careconnect.com

SuperAdmin:Password

seeded admin password

ChangeThisPassword123!

Cors:AllowedOrigins

allowed browser origins

["http://localhost:4200"]

Set the sensitive ones as user secrets (run from the repo root):

dotnet user-secrets set "Jwt:Key" "<a-64-char-random-string>" --project src/CareConnect.Api
dotnet user-secrets set "SuperAdmin:Email" "admin@careconnect.com" --project src/CareConnect.Api
dotnet user-secrets set "SuperAdmin:Password" "ChangeThisPassword123!" --project src/CareConnect.Api

Database — run migrations manually

Migrations are not applied automatically. Create and apply the initial migration yourself:

dotnet ef migrations add InitialIdentityAndProfiles -p src/CareConnect.Infrastructure -s src/CareConnect.Api

dotnet ef database update -p src/CareConnect.Infrastructure -s src/CareConnect.Api

Roles and the SuperAdmin account are seeded automatically on API start-up (idempotent). Theschema is never touched by seeding — only the migration commands above change the database.

Run

Backend (serves Swagger at the root in Development):

dotnet run --project src/CareConnect.Api

HTTP: http://localhost:5290  ·  Swagger: http://localhost:5290/swagger

HTTPS: https://localhost:7122

Frontend:

npm start --prefix careconnect-client

App: http://localhost:4200 (the API's CORS policy already allows this origin)

Tests

dotnet test

37 integration tests boot the real API over an in-memory SQLite database (no SQL Server andno migration needed) and cover registration for all four roles, login, /me, refresh-tokenrotation and reuse detection, inactive-user lockout, and SuperAdmin authorization.

Seeded SuperAdmin credentials (development)

Email: admin@careconnect.com

Password: ChangeThisPassword123!

Change this password before any non-local deployment. Registration cannot create aSuperAdmin — the role is seed-only.

API surface

POST /api/auth/register
POST /api/auth/login
POST /api/auth/refresh-token
POST /api/auth/revoke-token       (auth)
POST /api/auth/change-password    (auth)
POST /api/auth/logout             (auth)
GET  /api/auth/me                 (auth)

GET   /api/super-admin/users                         (SuperAdmin) — search, role/status filter, paging
PATCH /api/super-admin/users/{userId}/toggle-status  (SuperAdmin)

=======

<div align="center">

 ██╗  ██╗███████╗ █████╗ ██╗  ████████╗██╗  ██╗ ██████╗ █████╗ ██████╗ ███████╗
 ██║  ██║██╔════╝██╔══██╗██║  ╚══██╔══╝██║  ██║██╔════╝██╔══██╗██╔══██╗██╔════╝
 ███████║█████╗  ███████║██║     ██║   ███████║██║     ███████║██████╔╝█████╗  
 ██╔══██║██╔══╝  ██╔══██║██║     ██║   ██╔══██║██║     ██╔══██║██╔══██╗██╔══╝  
 ██║  ██║███████╗██║  ██║███████╗██║   ██║  ██║╚██████╗██║  ██║██║  ██║███████╗
 ╚═╝  ╚═╝╚══════╝╚═╝  ╚═╝╚══════╝╚═╝   ╚═╝  ╚═╝ ╚═════╝╚═╝  ╚═╝╚═╝  ╚═╝╚══════╝

Smart Healthcare Platform

One platform. Every patient. Every doctor. Every service.

🎓 Graduation Project — Faculty of Computer Science

<br/>



</div>

📌 Overview

Smart Healthcare Platform is a comprehensive, AI-powered healthcare management system built as a graduation project. The platform bridges the gap between patients, doctors, hospitals, and medical services — all within a single unified system.

From booking appointments and managing health insurance digitally, to discovering nearby hospitals and getting AI-driven medical guidance — this platform reimagines how healthcare services are accessed and delivered.

🏗️ System Architecture

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

⚡ Tech Stack

Layer

Technology

Framework

ASP.NET Core MVC (.NET 8)

Frontend

Razor Pages, Bootstrap 5, HTML/CSS/JS

Database

Microsoft SQL Server

ORM

Entity Framework Core

AI Integration

OpenAI GPT API (ChatGPT)

Authentication

ASP.NET Identity + Role-based Auth

Maps & Location

Location-based Hospital Discovery

Architecture

MVC Pattern + Service Layer

✨ Features

👤 Patient Portal

🔐 Secure registration, login, and profile management

📅 Appointment Booking — search and book doctors by specialty

🏥 Hospital Discovery — find nearby hospitals based on location

🩸 Blood Bank — request blood units by type and location

📋 Digital Health Insurance — submit and track insurance requests online

⭐ Reviews & Ratings — rate doctors and hospital services

🤖 AI Medical Assistant — get instant medical guidance powered by OpenAI GPT

👨‍⚕️ Doctor Portal

📆 Manage availability and appointment schedule

👁️ View patient appointment history

✅ Accept / Cancel appointment requests

⭐ Receive patient reviews and ratings

🏥 Hospital Portal

🗂️ Manage hospital profile and listed services

🩸 Manage blood bank inventory (available blood types & quantities)

📊 View incoming appointment and service requests

🛡️ Admin Dashboard

👥 Full user management (Patients, Doctors, Hospitals)

✅ Approve / Reject doctor and hospital registrations

📊 Platform-wide analytics and statistics

🗂️ Manage categories, specialties, and system settings

🤖 AI-Powered Medical Assistant

Integrated with OpenAI GPT API

Answers patient medical questions in natural language

Suggests relevant specialties based on symptoms

Available 24/7 within the platform

🚀 Getting Started

Prerequisites

.NET 8 SDK

SQL Server (or LocalDB)

Visual Studio 2022 or VS Code

OpenAI API Key → platform.openai.com

⚙️ Setup & Run

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

✅ App will be running at: https://localhost:7000

⚙️ Configuration

Update appsettings.json with your credentials:

{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=SmartHealthcareDb;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "OpenAI": {
    "ApiKey": "YOUR_OPENAI_API_KEY_HERE",
    "Model": "gpt-4o"
  }
}

⚠️ Never commit your API key to GitHub. Use environment variables or .NET User Secrets in development.

Using .NET User Secrets (Recommended)

dotnet user-secrets set "OpenAI:ApiKey" "your-api-key-here"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your-connection-string"

🔐 Roles & Access Control

Role

Access

Patient

Book appointments, insurance, blood requests, AI assistant, reviews

Doctor

Manage schedule, view patients, respond to bookings

Hospital

Manage profile, blood bank, services

Admin

Full platform control, approvals, analytics

🗄️ Database Schema (Key Entities)

Users (ASP.NET Identity)
    ├── Patients          → Appointments, InsuranceRequests, Reviews
    ├── Doctors           → Specialties, Schedules, Reviews
    └── Hospitals         → Services, BloodBank, Location

Appointments              → Patient ↔ Doctor
BloodBank                 → Hospital ↔ BloodRequests
HealthInsurance           → Patient → InsuranceRequest
Reviews                   → Patient → Doctor / Hospital

🤖 AI Integration — How It Works

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

The AI assistant is context-aware and always recommends consulting a real doctor for diagnosis.

📸 Screenshots

(Add screenshots of your platform here)

Dashboard

Appointment Booking

AI Assistant







🧪 Seeded Test Accounts

After running migrations, use these to explore the platform:

Role

Email

Password

Admin

admin@healthcare.com

Admin@123

Doctor

doctor@healthcare.com

Doctor@123

Patient

patient@healthcare.com

Patient@123

(Update these with your actual seeded credentials)

👥 Team

Name

Role

Ahmed Saeed Shalaby

Full-Stack Developer

Eslam Salem

Full-Stack Developer

Abdelrahman Rabea

Full-Stack Developer

Abdelrahman Siam

Flutter Developer

Alaa Naser

Flutter Developer

Saif Omran

Cloud Architect

📄 License

This project is licensed under the MIT License — see the LICENSE file for details.

<div align="center">

🎓 Graduation Project — Built with passion, purpose, and a lot of coffee ☕

Smart Healthcare Platform — Because healthcare should be accessible to everyone.

⭐ If you found this project interesting, please give it a star!

</div>
