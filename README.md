<div align="center">

🏥 CareConnect Egypt

A full-stack healthcare coordination platform for patients, doctors, hospitals, and administrators

Graduation Project · ASP.NET Core Web API · Angular · Clean Architecture

<br>



<br>

Connecting healthcare services through one secure, role-based platform.

</div>

Overview

CareConnect Egypt is an academic healthcare platform designed to connect patients with doctors and hospitals through a unified digital experience.

The system supports the complete journey from discovering healthcare providers and booking appointments to submitting digital insurance requests, locating hospitals, and requesting blood units. Each role receives a dedicated dashboard and securely scoped workflows.

The repository contains a .NET 10 ASP.NET Core Web API, an Angular 21 standalone frontend, a SQL Server database, and an integration-test project.

Current Project Status

Module

Status

Authentication, refresh tokens, and role-based access

✅ Implemented

Patient, doctor, and hospital profiles

✅ Implemented

Medical specialties and hospital affiliations

✅ Implemented

Doctor schedules and unavailable periods

✅ Implemented

Appointment booking and management

✅ Implemented

Digital insurance request workflow

✅ Implemented

Insurance company administration

✅ Implemented

Hospital blood stock and patient blood requests

✅ Implemented

Hospital location and nearby discovery

✅ Implemented

SuperAdmin user management

✅ Implemented

MedicalServiceProvider full business workflow

🟡 Planned

AI medical assistant

🟡 Planned

Reviews and ratings

🟡 Planned

The README distinguishes implemented functionality from planned modules so the repository accurately reflects the current codebase.

Core Features

👤 Patient Portal

Secure registration, login, token refresh, and password management

Browse doctors and hospitals

Filter healthcare providers by specialty and location

Book appointments from available doctor slots

Track pending, confirmed, completed, rejected, cancelled, and no-show appointments

Submit and track digital insurance requests

Search blood availability across hospitals

Submit and track blood requests

Discover nearby hospitals using browser geolocation

Open external directions to a hospital without requiring a paid Maps API

👨‍⚕️ Doctor Portal

Manage the doctor profile and professional information

Request affiliation with hospitals

Track hospital affiliation requests

Configure recurring weekly availability

Add temporary unavailable periods

Review patient appointments

Confirm, reject, complete, cancel, or mark appointments as no-show according to the allowed workflow

🏥 Hospital Portal

Manage hospital profile information

Maintain address, governorate, city, latitude, and longitude

Review doctor affiliation requests

View affiliated doctors

View appointments associated with the hospital

Review, approve, or reject insurance requests

Manage blood stock for supported blood groups

Review, approve, reject, and fulfill patient blood requests

🛡️ SuperAdmin Portal

Search and manage platform users

Activate or deactivate accounts

Manage medical specialties

Manage insurance companies

Access role-scoped administrative dashboards

🔎 Shared Directories

Doctor directory and doctor details

Hospital directory and hospital details

Location-aware hospital discovery

Hospital blood-bank availability

Role-aware actions such as appointment booking and blood requests

Roles and Access Control

Role

Main Responsibilities

Patient

Discover providers, book appointments, submit insurance requests, and request blood

Doctor

Manage profile, affiliations, availability, unavailable periods, and appointments

Hospital

Manage profile/location, doctors, appointments, insurance requests, and blood stock

SuperAdmin

Manage users, specialties, insurance companies, and platform access

MedicalServiceProvider

Role scaffolded for a future dedicated service-provider module

Authorization is enforced by the API. Angular guards improve navigation and user experience but are not treated as the primary security boundary.

Architecture

flowchart LR
    UI[Angular 21 Client] -->|HTTPS / JSON| API[ASP.NET Core Web API]
    API --> AUTH[Identity + JWT + Refresh Tokens]
    API --> APP[Application Layer]
    APP --> DOMAIN[Domain Layer]
    APP --> INFRA[Infrastructure Layer]
    INFRA --> DB[(SQL Server)]
    INFRA --> LOGS[Serilog]

Clean Architecture Layers

CareConnectEgypt/
├── CareConnect.slnx
├── src/
│   ├── CareConnect.Domain/
│   │   └── Entities, enums, domain constants, and core rules
│   ├── CareConnect.Application/
│   │   └── DTOs, interfaces, validation, filters, and service contracts
│   ├── CareConnect.Infrastructure/
│   │   └── EF Core, SQL Server, Identity, JWT, services, configurations, and seeding
│   └── CareConnect.Api/
│       └── Controllers, middleware, dependency injection, Swagger, and Serilog
├── careconnect-client/
│   └── Angular standalone application
└── tests/
    └── CareConnect.Api.IntegrationTests/

Dependency Direction

Domain
  ↑
Application
  ↑
Infrastructure
  ↑
API

Angular Client → API only

Technology Stack

Area

Technology

Backend

ASP.NET Core Web API on .NET 10

Frontend

Angular 21 standalone components

UI

Angular Material and Angular CDK

Language

C# and TypeScript

Database

Microsoft SQL Server

ORM

Entity Framework Core 10

Authentication

ASP.NET Core Identity

API Security

JWT access tokens and rotating refresh tokens

Authorization

Role-based and ownership-based authorization

Logging

Serilog console and rolling-file sinks

API Documentation

Swagger / OpenAPI

Testing

xUnit, ASP.NET Core TestServer, FluentAssertions, and SQLite

Architecture

Clean Architecture

Important Domain Relationships

erDiagram
    APPLICATION_USER ||--o| PATIENT_PROFILE : owns
    APPLICATION_USER ||--o| DOCTOR_PROFILE : owns
    APPLICATION_USER ||--o| HOSPITAL_PROFILE : owns

    DOCTOR_PROFILE }o--|| SPECIALTY : belongs_to
    DOCTOR_PROFILE ||--o{ DOCTOR_HOSPITAL_AFFILIATION : requests
    HOSPITAL_PROFILE ||--o{ DOCTOR_HOSPITAL_AFFILIATION : receives
    HOSPITAL_PROFILE ||--o{ HOSPITAL_SPECIALTY : offers
    SPECIALTY ||--o{ HOSPITAL_SPECIALTY : categorizes

    PATIENT_PROFILE ||--o{ APPOINTMENT : books
    DOCTOR_PROFILE ||--o{ APPOINTMENT : attends
    HOSPITAL_PROFILE ||--o{ APPOINTMENT : hosts

    PATIENT_PROFILE ||--o{ INSURANCE_REQUEST : submits
    HOSPITAL_PROFILE ||--o{ INSURANCE_REQUEST : reviews
    INSURANCE_COMPANY ||--o{ INSURANCE_REQUEST : covers

    HOSPITAL_PROFILE ||--o{ BLOOD_STOCK : manages
    PATIENT_PROFILE ||--o{ BLOOD_REQUEST : submits
    HOSPITAL_PROFILE ||--o{ BLOOD_REQUEST : processes

Main Workflows

Appointment Booking

Patient selects doctor
        ↓
API generates available slots
        ↓
Schedule, unavailable periods, and existing appointments are checked
        ↓
Patient submits booking
        ↓
Doctor confirms or rejects
        ↓
Appointment reaches its final status

Digital Insurance

Patient selects an eligible appointment
        ↓
Patient submits insurance information
        ↓
Hospital starts review
        ↓
Hospital approves or rejects
        ↓
Patient tracks the final decision

Blood Bank

Hospital maintains blood stock
        ↓
Patient searches availability
        ↓
Patient submits a blood request
        ↓
Hospital approves only when stock is sufficient
        ↓
Allocated units are deducted transactionally
        ↓
Hospital marks the request as fulfilled

Hospital Discovery

User explicitly shares browser location
        ↓
API applies a geographic bounding box
        ↓
Haversine distance is calculated
        ↓
Hospitals inside the selected radius are returned
        ↓
User opens destination directions

The user's current search coordinates are used for the request and are not intended to be persisted as location history.

Getting Started

Prerequisites

Install:

.NET 10 SDK

SQL Server or SQL Server Express/LocalDB

Node.js 20 or later

npm

Visual Studio 2022, JetBrains Rider, or VS Code

Entity Framework CLI when using terminal migration commands

dotnet tool install --global dotnet-ef

1. Clone the Repository

git clone https://github.com/ahmedshalaby03/CareConnect-Egypt-University-Graduation-Project.git
cd CareConnect-Egypt-University-Graduation-Project

2. Restore Backend Dependencies

dotnet restore CareConnect.slnx

3. Install Frontend Dependencies

npm install --prefix careconnect-client

Development Configuration

Keep secrets outside source control. Configure them through .NET User Secrets or environment variables.

Run these commands from the repository root:

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.;Database=CareConnectEgypt;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True" --project src/CareConnect.Api

dotnet user-secrets set "Jwt:Key" "<use-a-long-random-secret-of-at-least-32-characters>" --project src/CareConnect.Api

dotnet user-secrets set "SuperAdmin:Email" "admin@gmail.com" --project src/CareConnect.Api

dotnet user-secrets set "SuperAdmin:Password" "Admin@123" --project src/CareConnect.Api

Recommended development settings:

{
  "Jwt": {
    "Issuer": "CareConnectEgypt",
    "Audience": "CareConnectEgyptClient",
    "AccessTokenMinutes": 60,
    "RefreshTokenDays": 7,
    "ClockSkewSeconds": 30
  },
  "DemoData": {
    "Enabled": true
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:4200",
      "https://localhost:4200"
    ]
  }
}

Never commit real connection strings, JWT signing keys, production credentials, reset tokens, or third-party API keys.

Database Setup

The repository uses Entity Framework Core migrations.

Terminal

dotnet ef database update \
  --project src/CareConnect.Infrastructure \
  --startup-project src/CareConnect.Api

Visual Studio Package Manager Console

Default Project: CareConnect.Infrastructure
Startup Project: CareConnect.Api

Update-Database

Create a new migration only when the EF Core model or database schema actually changes:

dotnet ef migrations add MigrationName \
  --project src/CareConnect.Infrastructure \
  --startup-project src/CareConnect.Api

Adding demo records, changing an Identity email, or resetting a password is a data change and does not require an empty migration.

Run the Application

Backend API

dotnet run --project src/CareConnect.Api

Development endpoints:

API over HTTP: http://localhost:5290

Swagger: http://localhost:5290/swagger

API over HTTPS: https://localhost:7122

Angular Client

npm start --prefix careconnect-client

Frontend:

http://localhost:4200

Development Demo Accounts

These accounts are intended only for a local development database.

Role

Email

Password

SuperAdmin

admin@gmail.com

Admin@123

Patient

ahmed@gmail.com

Ahmed@123

Doctor

doctor.cardiology@careconnect.local

CareConnect@123

Hospital

cairohospital@careconnect.local

CareConnect@123

Doctor and hospital accounts are available after the development demo-data process completes successfully.

Change or remove all demo credentials before any public, shared, staging, or production deployment.

Testing

Run the complete backend test suite:

dotnet test

The integration-test project uses:

xUnit

ASP.NET Core MVC testing infrastructure

FluentAssertions

SQLite

Coverlet

Build the Angular application:

npm run build --prefix careconnect-client

Security Highlights

ASP.NET Core Identity password hashing

JWT access tokens with refresh-token rotation

Role-based endpoint authorization

Ownership checks for patient, doctor, and hospital resources

Active-account enforcement

Server-side status-transition validation

Server-side duplicate-request protection

Restricted or no-action delete behavior for historical healthcare records

DTO-based responses instead of exposing EF Core entities

Server-side validation of appointment, stock, insurance, and location rules

No direct password-hash manipulation

No browser-location history persistence

Project Roadmap

Next Priorities

AI medical assistant with safe medical disclaimers and specialty guidance

Dedicated MedicalServiceProvider module

Reviews and ratings

Notification center

Profile-image and document-upload strategy

Deployment configuration and CI/CD

Expanded integration and authorization coverage

Production observability and health checks

Explicitly Outside the Current MVP

Medical diagnosis by AI

Emergency dispatch

Ambulance tracking

Real-time patient tracking

Payment processing

External insurance-provider integration

National blood-bank integration

Laboratory cross-matching

Telemedicine video calls

Git Workflow

Feature development follows dedicated branches:

feature/authentication-and-user-management
feature/specialties-and-doctor-hospital-affiliations
feature/doctor-schedules-and-appointments
feature/digital-insurance-requests
feature/blood-bank-module
feature/hospital-location-discovery
feature/development-demo-data

Recommended workflow:

git checkout main
git pull origin main
git checkout -b feature/feature-name

# implement and verify

git add .
git commit -m "feat: describe the feature"
git push -u origin feature/feature-name

Merge through a Pull Request into main.

Team

Name

Role

Ahmed Saeed Shalaby

Full-Stack .NET Developer

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

Academic Purpose

CareConnect Egypt was developed as a graduation project to demonstrate:

Full-stack application design

Clean Architecture

REST API development

Angular application development

Relational database modeling

Authentication and authorization

Complex business workflows

Entity Framework Core migrations

Secure ownership validation

Collaborative Git and GitHub practices

<div align="center">

Built to make healthcare access more connected, organized, and transparent.

CareConnect Egypt

⭐ Star the repository if you find the project useful.

</div>
