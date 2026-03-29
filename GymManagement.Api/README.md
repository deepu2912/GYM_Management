# GymManagement.Api

Initial Web API scaffold for Gym Management System with:

- JWT authentication
- Role-based authorization (`Admin`, `Trainer`, `Member`)
- Member management APIs
- Trainer API for assigned members
- SQL Server persistence using EF Core

## Prerequisites

- .NET 10 SDK (required by project target `net10.0`)
- Access to NuGet sources (if your environment uses package source mapping)

## Seed Users

- Admin: `admin@gym.local` / `Admin@123`
- Trainer: `trainer@gym.local` / `Trainer@123`

## Database

- Server: `DESKTOP-UK6PV96\SQLEXPRESS`
- Database: `GymManagementDb`
- Login: `sa1`
- Tables synced: `Users`, `Members`

## Run

```bash
dotnet restore
dotnet run
```

## Main APIs

- `POST /api/auth/register-member`
- `POST /api/auth/login`
- `GET /api/members` (Admin, Trainer)
- `GET /api/members/{id}` (Admin, Trainer, own Member record)
- `POST /api/members` (Admin)
- `PUT /api/members/{id}` (Admin)
- `DELETE /api/members/{id}` (Admin)
- `PUT /api/members/{id}/assign-trainer` (Admin)
- `POST /api/members/{id}/upload-photo` (Admin or own Member record)
- `GET /api/trainers` (Admin)
- `POST /api/trainers` (Admin)
- `PUT /api/trainers/{trainerId}/assign-members` (Admin)
- `GET /api/trainers/assigned-members` (Trainer)
- `GET /api/trainers/{trainerId}/assigned-members` (Admin, Trainer-self)
- `POST /api/trainers/{trainerId}/salaries` (Admin)
- `GET /api/trainers/{trainerId}/salaries` (Admin, Trainer-self)
- `GET /api/trainers/salaries/{salaryId}` (Admin)
- `PUT /api/trainers/salaries/{salaryId}` (Admin)
- `POST /api/payments` (Admin)
- `GET /api/payments/{id}` (Admin, Member-own)
- `GET /api/payments/member/{memberId}` (Admin, Member-own)
- `GET /api/payments/{paymentId}/invoice` (Admin, Member-own)
- `GET /api/payments/{paymentId}/invoice/pdf` (Admin, Member-own)
- `GET /api/payments/due-report` (Admin)
- `GET /api/payments/member/{memberId}/due` (Admin, Member-own)
- `POST /api/attendance/mark` (Admin, Trainer)
- `GET /api/attendance/report` (Admin, Trainer, Member-own)
- `GET /api/attendance/summary/monthly?year=2026&month=2&memberId={optional}` (Admin, Trainer, Member-own)
- `GET /api/gymprofile` (Admin, Trainer, Member)
- `PUT /api/gymprofile` (Admin)
- `GET /api/membershipplans`
- `POST /api/membershipplans` (Admin)
- `PUT /api/membershipplans/{id}` (Admin)
- `DELETE /api/membershipplans/{id}` (Admin)
- `GET /api/membermemberships` (Admin, Trainer)
- `POST /api/membermemberships` (Admin)
- `PUT /api/membermemberships/{id}` (Admin)
- `DELETE /api/membermemberships/{id}` (Admin)
