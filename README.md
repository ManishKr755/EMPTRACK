# EMPTRACK – Employee Management System

**EMPTRACK** is a web-based Employee Management System developed using ASP.NET Core MVC and C#. It is designed to organize employee-related information and streamline administrative operations through a centralized web application.

The project uses a structured MVC architecture, Entity Framework Core for database operations, and SQL Server for data storage.

## 📌 Project Overview

Managing employee information and day-to-day administrative activities manually can be time-consuming and difficult to maintain. EMPTRACK aims to provide a centralized system for managing employee-related records and supporting common organizational workflows.

The application follows the Model–View–Controller (MVC) architecture to separate application data, user interface, and request-handling logic.

## 🎯 Objectives

- Develop a centralized employee management application.
- Organize employee information and account records.
- Provide authentication for controlled application access.
- Support employee-related administrative workflows.
- Manage database operations using Entity Framework Core.
- Improve maintainability through a structured application architecture.

## ✨ Key Features

- **Employee Management:** Support for employee-related records and information.
- **Authentication:** Cookie-based authentication for user sessions.
- **Role-Based Access:** Role information is used to distinguish administrator and employee access.
- **Account Validation:** Application checks account status and employee employment status during authentication validation.
- **Leave Management Services:** Includes leave balance-related services and a background worker for leave balance accrual.
- **Employee Code Service:** Includes a service for employee code-related operations.
- **Notification Service:** Includes a service for application notifications.
- **Database Integration:** SQL Server integration through Entity Framework Core.
- **MVC Architecture:** Separate controllers, models, services, and views.
- **Security Measures:** Includes antiforgery validation for unsafe MVC requests, HTTPS redirection, and production error handling.

*The availability of individual operations depends on the corresponding implementation in the application.*

## 🛠️ Technology Stack

| Technology | Purpose |
|---|---|
| C# | Application programming language |
| ASP.NET Core MVC | Web application framework |
| .NET 10 | Target framework |
| Entity Framework Core 10 | Database access and ORM |
| Microsoft SQL Server | Relational database |
| Razor Views | Server-rendered user interface |
| HTML, CSS and JavaScript | Frontend development |
| Visual Studio / IIS Express | Development and local hosting |

## 🏗️ Project Structure

```text
EMPTRACK/
├── Controllers/              # Request handling and application logic
├── Data/                     # Database context and data access
├── Database/                 # Database-related project files
├── Models/                   # Application data models
├── Properties/               # Project configuration
├── Services/                 # Reusable business services
├── ViewComponents/           # Reusable UI components
├── Views/                    # MVC Razor views
├── wwwroot/                  # Static assets
├── DataProtection-Keys/      # Data protection key storage
├── EMPTRACK.csproj           # Project and package configuration
├── EMPTRACK.slnx             # Solution file
├── Program.cs                # Application configuration and startup
├── appsettings.json          # Application configuration
└── README.md                 # Project documentation
```

## ⚙️ Prerequisites

Before running the project, make sure you have:

- Windows, or another supported development environment.
- Visual Studio with the ASP.NET and web development workload, or the .NET SDK.
- .NET 10 SDK.
- Microsoft SQL Server.
- SQL Server Management Studio (SSMS), or another suitable database management tool.

## 🚀 Installation and Setup

### 1. Clone the repository

Open a terminal and run:

```bash
git clone https://github.com/ManishKr755/EMPTRACK.git
```

Navigate to the project directory:

```bash
cd EMPTRACK
```

### 2. Configure the database

Open `appsettings.json` and configure the `DefaultConnection` connection string for your SQL Server instance.

Example:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=EMPTRACKDB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

Replace `YOUR_SERVER` with your actual SQL Server instance name and adjust the authentication settings according to your environment.

**Important:** Keep your real database credentials private. Do not commit passwords, production connection strings, or other secrets to GitHub.

### 3. Restore dependencies

Run the following command in the project directory:

```bash
dotnet restore
```

### 4. Apply database migrations

If the project contains Entity Framework Core migrations, apply them using:

```bash
dotnet ef database update
```

If the EF Core command is unavailable, install the required EF Core CLI tool and ensure that the migrations and database configuration are present.

### 5. Run the application

Start the application using:

```bash
dotnet run
```

Alternatively, open `EMPTRACK.slnx` in a compatible version of Visual Studio, set `EMPTRACK` as the startup project, and run it using IIS Express.

### 6. Open the application

Open the local URL displayed in the terminal or Visual Studio output window.

The application's default MVC route directs unauthenticated users to the account login page.

## 🔐 Security

The application includes several security-related mechanisms:

- Cookie-based authentication.
- Authentication validation against account and employee status.
- Refreshing role claims from the database during authentication validation.
- Antiforgery token validation for unsafe MVC requests.
- HTTPS redirection.
- HTTP Strict Transport Security (HSTS) and centralized error handling outside development mode.
- ASP.NET Core Data Protection configuration.

For deployment, use secure configuration management, HTTPS, appropriate database permissions, and protected storage for sensitive keys.

## 🧩 Application Architecture

EMPTRACK follows the MVC pattern:

- **Model:** Represents application data and domain entities.
- **View:** Displays application pages using Razor views.
- **Controller:** Handles incoming requests and coordinates application operations.
- **Services:** Encapsulate reusable business operations.
- **Data Layer:** Uses Entity Framework Core to communicate with SQL Server.

This separation helps keep the application organized and makes future maintenance easier.

## 💡 Benefits

- Centralized employee-related information.
- Organized application structure.
- Reduced dependence on scattered manual records.
- Reusable services for common operations.
- Database-backed data management.
- A foundation for extending employee administration workflows.

## 🔮 Future Enhancements

Possible future improvements include:

- Enhanced employee search and filtering.
- Attendance and working-hours management.
- Leave request submission and approval workflows.
- Employee and administrator dashboards.
- Reports and data export.
- Automated notifications and reminders.
- Improved automated testing and deployment.

These are potential enhancements and should not be interpreted as existing features unless implemented in the project.

## 👨‍💻 Author

**Manish Kumar** ||
**Anish Kumar**  ||
**Sahil Kumar**

GitHub Repository: [ManishKr755/EMPTRACK](https://github.com/ManishKr755/EMPTRACK)

---

*EMPTRACK – Organizing employee information through a structured web-based management system.*
