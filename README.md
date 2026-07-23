<div align="center">

```
 ██████╗ █████╗ ██████╗ ███████╗ ██████╗ ██████╗ ███╗   ██╗███╗   ██╗███████╗ ██████╗████████╗
██╔════╝██╔══██╗██╔══██╗██╔════╝██╔════╝██╔═══██╗████╗  ██║████╗  ██║██╔════╝██╔════╝╚══██╔══╝
██║     ███████║██████╔╝█████╗  ██║     ██║   ██║██╔██╗ ██║██╔██╗ ██║█████╗  ██║        ██║   
██║     ██╔══██║██╔══██╗██╔══╝  ██║     ██║   ██║██║╚██╗██║██║╚██╗██║██╔══╝  ██║        ██║   
╚██████╗██║  ██║██║  ██║███████╗╚██████╗╚██████╔╝██║ ╚████║██║ ╚████║███████╗╚██████╗   ██║   
 ╚═════╝╚═╝  ╚═╝╚═╝  ╚═╝╚══════╝ ╚═════╝ ╚═════╝ ╚═╝  ╚═══╝╚═╝  ╚═══╝╚══════╝ ╚═════╝   ╚═╝   
                                    E G Y P T
```

### **Smart Healthcare Platform — Egypt**
*One platform. Every patient. Every doctor. Every hospital.*

🎓 **University Graduation Project**

<br/>

![.NET 10](https://img.shields.io/badge/.NET_10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![ASP.NET Core Web API](https://img.shields.io/badge/ASP.NET_Core_Web_API-5C2D91?style=for-the-badge&logo=dotnet&logoColor=white)
![Angular](https://img.shields.io/badge/Angular_21-DD0031?style=for-the-badge&logo=angular&logoColor=white)
![TypeScript](https://img.shields.io/badge/TypeScript-3178C6?style=for-the-badge&logo=typescript&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)
![Entity Framework Core](https://img.shields.io/badge/EF_Core-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![JWT](https://img.shields.io/badge/JWT_Auth-000000?style=for-the-badge&logo=jsonwebtokens&logoColor=white)
![Clean Architecture](https://img.shields.io/badge/Clean_Architecture-2E8B57?style=for-the-badge&logo=archlinux&logoColor=white)

</div>

---

## 📌 Overview

**CareConnect Egypt** is a full-stack, role-based healthcare management platform connecting **patients**, **doctors**, **hospitals**, **medical service providers**, and platform **administrators** in one system.

It handles the real, everyday friction of accessing healthcare in Egypt: finding a doctor by specialty, booking an appointment, tracking a digital insurance claim, checking whether a nearby hospital has the blood type you need — all through a modern API-driven backend and an Angular single-page frontend.

The platform is built on **Clean Architecture** principles on the backend and a fully **standalone-component Angular** frontend — no legacy NgModules, no third-party map billing, no shortcuts.

---

## 🏗️ System Architecture

```
CareConnectEgypt/
├── src/
│   ├── CareConnect.Domain/           # Entities, Enums, Constants
│   ├── CareConnect.Application/      # DTOs, Interfaces, Validators, Common
│   ├── CareConnect.Infrastructure/   # EF Core, Services, Migrations, Seed
│   └── CareConnect.Api/              # Controllers, Program.cs, appsettings
├── tests/
│   └── CareConnect.Api.IntegrationTests/
└── careconnect-client/               # Angular app
    └── src/app/
        ├── core/                     # models, services, guards, interceptors
        ├── features/                 # per-role pages (patient, doctor, hospital, super-admin, blood-bank, directory...)
        ├── layouts/
        └── shared/                   # dialogs, shared components
```

The backend follows **Clean Architecture**: `Domain` has zero external dependencies, `Application` defines the use cases and contracts, `Infrastructure` implements persistence and external services, and `Api` wires everything together and exposes it over HTTP.

---

## ⚡ Tech Stack

| Layer | Technology |
|---|---|
| **Backend Framework** | ASP.NET Core Web API (.NET 10) |
| **Architecture** | Clean Architecture (4-project separation) |
| **ORM** | Entity Framework Core |
| **Database** | Microsoft SQL Server |
| **Auth** | ASP.NET Core Identity + JWT + Refresh Tokens |
| **Validation** | FluentValidation |
| **Logging** | Serilog |
| **API Docs** | Swagger / OpenAPI |
| **Frontend Framework** | Angular 21 (standalone components) |
| **UI Library** | Angular Material |
| **Forms** | Reactive Forms |
| **State** | Angular Signals |
| **Language** | TypeScript |

> No OpenAI, no Bootstrap, no paid maps API — geolocation search runs on the Haversine formula, computed entirely server-side.

---

## 🔐 Roles & Access Control

| Role | Access |
|---|---|
| **SuperAdmin** | Full platform control: specialties, insurance companies, approvals, analytics |
| **Patient** | Search directory, book appointments, request insurance, request blood, view profile |
| **Doctor** | Manage weekly availability, respond to appointments, manage hospital affiliations |
| **Hospital** | Manage profile, specialties, blood bank inventory, location, incoming requests |
| **MedicalServiceProvider** | Manage service listings and profile |

---

## ✨ Features (Implemented & Working)

### 🔑 Authentication & Profiles
- Registration & login with **JWT + Refresh Tokens**
- Multi-role authorization across 5 roles
- Dedicated profiles: `PatientProfile`, `DoctorProfile`, `HospitalProfile`, `MedicalServiceProviderProfile`

### 🩺 Specialties & Directories
- Bilingual (Arabic / English) medical specialties, managed by SuperAdmin
- Hospital ↔ Specialty linking (`HospitalSpecialties`)
- Doctor ↔ Hospital affiliations with request / approve / reject workflow
- Searchable directories for doctors and hospitals

### 📅 Appointments
- Weekly doctor availability schedules (`DoctorAvailability`)
- Doctor unavailability periods (`DoctorUnavailablePeriods`)
- Full appointment lifecycle: **Pending → Confirmed → Rejected → Cancelled → Completed → NoShow**

### 🛡️ Digital Health Insurance
- Insurance requests linked to insurance companies
- Status workflow: **Pending → UnderReview → Approved → Rejected → Cancelled**
- Insurance company management from the SuperAdmin dashboard

### 🩸 Blood Bank
- Per-hospital blood inventory across all 8 blood types
- Patient blood requests with status workflow: **Pending → Approved → Rejected → Fulfilled → Cancelled**
- Search for blood availability at nearby hospitals

### 📍 Hospital Location & Nearby Discovery
- Each hospital stores latitude/longitude and address
- "Nearest hospital" search using the **Haversine formula** — no paid maps API
- Filter by specialty, blood availability, and appointment availability

### 📊 Dashboards
- Role-specific dashboards with relevant statistics for every role

### 🌱 Demo Data Seeder
- Idempotent seeder generating realistic demo data (hospitals, doctors, appointments, insurance, blood bank)
- Development-only, toggled via configuration

---

## 🚧 Not Yet Implemented

These are acknowledged next steps — not present in the current codebase:

- 🤖 AI Medical Assistant
- 📍 Real-time / ambulance tracking
- 💳 Payments
- 💬 Chat / video consultations
- 🔔 Notifications (SMS / Email / WhatsApp)

---

## 🚀 Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [SQL Server](https://www.microsoft.com/en-us/sql-server) (or LocalDB)
- [Node.js](https://nodejs.org/) + npm
- [Angular CLI](https://angular.dev/tools/cli) (`npm install -g @angular/cli`)

---

### ⚙️ Backend Setup

```bash
cd src/CareConnect.Api
dotnet restore
dotnet ef database update -p ../CareConnect.Infrastructure -s .
dotnet run
```

✅ API running at: **`http://localhost:5290`**
✅ Swagger UI at: **`http://localhost:5290/swagger`**

---

### 🎨 Frontend Setup

```bash
cd careconnect-client
npm install
ng serve
```

✅ App running at: **`http://localhost:4200`**

---

## 🌱 Demo Data

In **Development**, set `DemoData:Enabled` to `true` in `appsettings.json` to automatically seed realistic demo data — hospitals, doctors, patients, appointments, insurance requests, and blood bank inventory.

```json
{
  "DemoData": {
    "Enabled": true
  }
}
```

### Demo Credentials

> ⚠️ For local development only — not for production use.

| Role | Email | Password |
|---|---|---|
| SuperAdmin | `admin@gmail.com` | `Admin@123` |
| Patient | `ahmed@gmail.com` | `Ahmed@123` |
| Doctor | `doctor.cardiology@careconnect.local` | `CareConnect@123` |
| Hospital | `cairohospital@careconnect.local` | `CareConnect@123` |
| MedicalServiceProvider | `provider1@careconnect.local` | `CareConnect@123` |

---

## 🗄️ Database Schema (Key Entities)

```
Users (ASP.NET Identity)
    ├── PatientProfile           → Appointments, InsuranceRequests, BloodRequests
    ├── DoctorProfile            → Specialties, DoctorAvailability, DoctorHospitalAffiliations
    ├── HospitalProfile          → HospitalSpecialties, BloodBank, Location
    └── MedicalServiceProviderProfile

Appointments                     → Patient ↔ Doctor
DoctorAvailability                DoctorUnavailablePeriods
DoctorHospitalAffiliations        → Doctor ↔ Hospital
HospitalSpecialties               → Hospital ↔ Specialty
BloodBank                         → Hospital blood-type inventory
BloodRequests                     → Patient → Hospital
InsuranceRequests                 → Patient → InsuranceCompany
InsuranceCompanies                 (managed by SuperAdmin)
Specialties                        (bilingual, managed by SuperAdmin)
```

---

## 🧪 Testing

Integration tests live under `tests/CareConnect.Api.IntegrationTests`.

```bash
cd tests/CareConnect.Api.IntegrationTests
dotnet test
```

---

## 📸 Screenshots

> *(Add screenshots of the platform here)*

| Dashboard | Appointment Booking | Blood Bank |
|---|---|---|
| ![Dashboard](screenshots/dashboard.png) | ![Booking](screenshots/booking.png) | ![Blood Bank](screenshots/blood-bank.png) |

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

**🎓 University Graduation Project — Clean Architecture on the backend, standalone Angular on the front.**

*CareConnect Egypt — Because healthcare should be easy to find, book, and trust.*

⭐ If you found this project interesting, please give it a star!

</div>
