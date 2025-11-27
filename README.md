# Qudorat System

A comprehensive web application for managing Public Health Practitioners and Service Providers Registration in Abu Dhabi, integrating with the TAMM platform.

## Overview

The Qudorat system facilitates the submission, processing, and tracking of requests submitted via the TAMM platform. It provides features such as automated request assignment, role-based access, license issuance and tracking, and archiving, supporting a seamless workflow from submission to final approval or rejection.

## Technology Stack

- **Backend**: .NET 9.0 (ASP.NET Core Web API)
- **Database**: SQL Server
- **ORM**: Entity Framework Core 9.0
- **Background Jobs**: Hangfire
- **Authentication**: Windows Authentication (SSO via Active Directory)
- **Logging**: Serilog
- **API Documentation**: Swagger/OpenAPI

## Project Structure

```
QudoratSystem/
├── src/
│   ├── Qudorat.API/           # Web API project (Controllers, Middleware, Background Jobs)
│   ├── Qudorat.Application/   # Application layer (DTOs, Validators, Mappings)
│   ├── Qudorat.Core/          # Domain layer (Entities, Interfaces, Enums)
│   └── Qudorat.Infrastructure/ # Infrastructure layer (DbContext, Repositories, Services)
├── Database/
│   └── QudoratSchema.sql      # Database schema script
└── QudoratSystem.sln          # Solution file
```

## Features

### Application Management
- Submit applications from TAMM integration
- Automated task assignment to available officers
- Review and feedback workflow (Approve/Reject/Return)
- Re-assignment capabilities for supervisors
- Application history tracking
- Document management (applicant and internal documents)

### User Roles & Permissions
- **Officer**: Basic review capabilities
- **Specialist**: Review with escalation
- **Senior Specialist**: Review with reassignment capabilities
- **Section Head**: Full review with field editing
- **Director**: Full admin access
- **System Admin**: User and system management

### License Management
- Automatic license generation upon approval
- Certificate and card generation
- License expiry monitoring
- Renewal notifications (30 days before expiry)

### SLA Management
- 5 working days SLA tracking
- Escalation alerts:
  - Day 3: Alert to Specialist and Senior Specialist
  - Day 4: Alert to Section Head and Director
  - Day 5: Final reminder to assigned user

### Reporting
- Application status reports
- User performance reports
- SLA compliance reports
- KPI dashboard

## Setup Instructions

### Prerequisites

1. .NET 9.0 SDK
2. SQL Server 2019 or later
3. Visual Studio 2022 or VS Code

### Database Setup

1. Open SQL Server Management Studio
2. Run the script `Database/QudoratSchema.sql`
3. Update the connection string in `appsettings.json`

### Running the Application

```bash
# Navigate to API project
cd src/Qudorat.API

# Restore packages
dotnet restore

# Run the application
dotnet run
```

The API will be available at:
- HTTP: http://localhost:5000
- HTTPS: https://localhost:5001
- Swagger UI: https://localhost:5001/swagger

### Configuration

Update `appsettings.json` with your settings:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=QudoratDb;Trusted_Connection=True;"
  },
  "Email": {
    "SmtpServer": "your-smtp-server",
    "SmtpPort": 587,
    "SmtpUsername": "your-username",
    "SmtpPassword": "your-password"
  }
}
```

## API Endpoints

### Applications
- `GET /api/applications` - Get all applications with filtering
- `GET /api/applications/{id}` - Get application details
- `GET /api/applications/assigned` - Get assigned tasks
- `POST /api/applications/{id}/approve` - Approve application
- `POST /api/applications/{id}/reject` - Reject application
- `POST /api/applications/{id}/return` - Return for more info
- `POST /api/applications/{id}/reassign` - Reassign to another user
- `POST /api/applications/{id}/lock` - Lock application
- `POST /api/applications/{id}/release` - Release application

### Users
- `GET /api/users` - Get all users
- `POST /api/users` - Create user
- `PUT /api/users/{id}/status` - Update user status (Online/Offline)

### Licenses
- `GET /api/licenses/search` - Search licenses (public)
- `GET /api/licenses/{id}` - Get license details
- `GET /api/licenses/{id}/certificate` - Download certificate
- `GET /api/licenses/{id}/card` - Download card

### Reports
- `GET /api/reports/application-status` - Application status report
- `GET /api/reports/user-performance` - User performance report
- `GET /api/reports/sla-compliance` - SLA compliance report

### TAMM Integration
- `POST /api/tamm/applications` - Receive application from TAMM

## Background Jobs

The system uses Hangfire for background job processing:

1. **Task Assignment** (Every 3 minutes): Automatically assigns pending applications to available officers
2. **SLA Monitoring** (Every hour): Checks SLA status and sends escalation alerts
3. **License Expiry Check** (Daily at 8 AM): Checks for expiring licenses and sends renewal notifications

Access Hangfire Dashboard at: `/hangfire`

## Workflow Matrix

| Action Owner | Scenario 1 | Scenario 2 | Scenario 3 | Scenario 4 | Scenario 5 |
|-------------|------------|------------|------------|------------|------------|
| Officer | Approve | Approve | Approve | Reject | Return |
| Specialist | Approve | Reject | Reject | / | / |
| Senior Specialist | / | Approve | Reject | / | / |
| **Result** | **Approve** | **Approve** | **Reject** | **Reject** | **Return** |

- Two approvals required for final approval
- First rejection closes the request
- Returns go back to applicant (max 3 times before auto-reject)

## Services Supported

1. Register as an OSH General Practitioner (DOH/0208)
2. Register as an OSH Senior Practitioner (DOH/0209)
3. Register as an OSH Health Auditor (DOH/0214)
4. Register as an Asbestos Supervising Consultant (DOH/0211)
5. Register as a Workplace First Aider (DOH/0212)
6. Registration as an OSH Consultancy Office (DOH/0213)
7. Registration as an OSH Auditing Office (DOH/0222)

Plus renewal services for all above.

## Security Considerations

- Windows Authentication (SSO) for internal users
- Role-based authorization
- API key authentication for TAMM integration
- Audit logging for all actions
- Soft delete for data retention

## License

Proprietary - Abu Dhabi Public Health Centre (ADPHC)

## Contact

For support or questions, contact the IT Department at ADPHC.
