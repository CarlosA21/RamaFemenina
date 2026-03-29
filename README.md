# ??? Rama Femenina Contra el Cáncer — Management System

<p align="center">
  <img src="Assets/icono2.ico" alt="Rama Femenina Logo" width="120"/>
</p>

<p align="center">
  <strong>A comprehensive desktop management system built for <em>Rama Femenina Contra el Cáncer, Inc.</em></strong><br/>
  A non-profit organization in Santiago, Dominican Republic, dedicated to supporting oncology patients since 1951.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white" alt=".NET 8"/>
  <img src="https://img.shields.io/badge/WinUI-3-0078D4?logo=windows&logoColor=white" alt="WinUI 3"/>
  <img src="https://img.shields.io/badge/SQL%20Server-Database-CC2927?logo=microsoftsqlserver&logoColor=white" alt="SQL Server"/>
  <img src="https://img.shields.io/badge/Entity%20Framework-Core%208-512BD4?logo=dotnet&logoColor=white" alt="EF Core 8"/>
  <img src="https://img.shields.io/badge/License-Private-lightgrey" alt="License"/>
</p>

---

## ?? Overview

This Windows desktop application streamlines the day-to-day administrative operations of **Rama Femenina Contra el Cáncer**, managing patient records, donations, invoicing, receipts, checks, and financial reporting — all through a modern, intuitive interface.

## ? Features

### ?? Patient Management
- Full CRUD operations for oncology patient records
- Track patient status, medical record numbers, and demographic data
- Search and filter by name, ID, area, and status

### ?? Donations Tracking
- Register and monitor donations linked to specific patients
- Track donation progress with visual indicators (Pending / Partial / Completed)
- Requested amount vs. received amount comparison with percentage tracking

### ?? Invoice Management (NCF)
- Generate invoices compliant with the **Dominican Republic NCF** (Número de Comprobante Fiscal) system
- Support for multiple NCF types: **B14** (Special Regimes) and **B15** (Government)
- Automatic NCF sequence management
- Tax calculation (ITBIS) with exempt and taxable amount breakdown
- Multiple payment methods: Cash, Check, and Credit

### ?? Receipt Generation
- Income receipt creation and management
- Support for Cash, Transfer, and Check payment methods
- Bank and check number tracking

### ?? Check Control
- Complete check registry with number, amount, date, and payee
- Concept tracking for each issued check

### ?? Client Management
- Client database with contact information, address, and RNC (tax ID)
- Client association with invoices

### ?? Reporting
- Financial reports with PDF export
- Invoice and receipt PDF generation optimized for **Epson LX-350** dot-matrix printers
- Half-letter (5.5" × 8.5") format support
- DevExpress Reporting integration

### ?? Authentication & Role-Based Access
- Secure login with **BCrypt** password hashing
- Role-based permissions system (**Admin** / **Moderator**)
- Automatic default admin user creation on first launch
- Moderator role restrictions (e.g., hidden check management)

### ?? Petty Cash (Caja Chica)
- Track small expenses with payee, amount, charge-to account, and concept

## ??? Tech Stack

| Layer | Technology |
|---|---|
| **Framework** | .NET 8.0 (Windows 10.0.19041+) |
| **UI** | WinUI 3 (Windows App SDK) |
| **ORM** | Entity Framework Core 8 |
| **Database** | SQL Server (with resilient retry policies) |
| **PDF Generation** | iText 7 |
| **Reporting** | DevExpress Reporting |
| **Security** | BCrypt.Net |
| **Configuration** | Microsoft.Extensions.Configuration |
| **DI Container** | Microsoft.Extensions.DependencyInjection |

## ?? Project Structure

```
RamaFemenina/
??? Assets/                     # Application icons and images
??? Common/
?   ??? BaseNavigationPage.cs   # Base class for navigation pages
?   ??? BasePaginatedPage.cs    # Base class with pagination support
??? Data/
?   ??? RamaFemeninaContext.cs  # EF Core DbContext with optimized configuration
??? Extensions/
?   ??? DispatcherQueueExtensions.cs
??? Models/
?   ??? Acceso.cs               # User authentication model
?   ??? CajaChica.cs            # Petty cash model
?   ??? Cheques.cs              # Checks model
?   ??? Clientes.cs             # Clients model
?   ??? Donaciones.cs           # Donations model
?   ??? Factura.cs              # Invoice model (NCF compliant)
?   ??? Paciente.cs             # Patient model
?   ??? Recibo.cs               # Receipt model
?   ??? ReportParameters.cs     # Report configuration model
??? Services/
?   ??? AuthenticationService.cs      # Login & user management with BCrypt
?   ??? ConfigurationService.cs       # App configuration
?   ??? CrystalReportService.cs       # Legacy report service
?   ??? DataCacheService.cs           # Data caching layer
?   ??? FacturaNcfPdfService.cs       # NCF invoice PDF generation
?   ??? FacturaService.cs             # Invoice business logic
?   ??? NcfSequenceService.cs         # NCF sequence management
?   ??? PaginatedCollection.cs        # Pagination utility
?   ??? PdfReportService.cs           # General PDF report generation
?   ??? ReciboPdfService.cs           # Receipt PDF generation
?   ??? ReportManager.cs              # Report orchestration
?   ??? SimpleReportService.cs        # Simplified reporting
??? Utilities/
?   ??? PasswordHashUtility.cs        # Password hashing helpers
??? Utils/
?   ??? NumeroALetras.cs              # Number-to-words converter (Spanish)
??? Scripts/                          # SQL migration scripts
??? App.xaml / App.xaml.cs            # Application entry point & DI setup
??? MainWindow.xaml                   # Login window
??? HomeWindow.xaml                   # Main navigation shell
??? PacientesPage.xaml                # Patients page
??? ClientesPage.xaml                 # Clients page
??? DonacionesPage.xaml               # Donations page
??? ChequesPage.xaml                  # Checks page
??? ReciboPage.xaml                   # Receipts page
??? ReportPage.xaml                   # Reports page
??? CreateUser.xaml                   # User creation page
??? appsettings.json                  # Connection string & configuration
```

## ?? Getting Started

### Prerequisites

- **Windows 10** (version 1903 / build 19041 or later)
- **Visual Studio 2022** (17.8+) with the following workloads:
  - .NET Desktop Development
  - Windows App SDK (WinUI 3)
- **SQL Server** (LocalDB, Express, or full edition)
- **.NET 8.0 SDK**

### Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/CarlosA21/RamaFemenina.git
   cd RamaFemenina
   ```

2. **Configure the database connection**

   Edit `appsettings.json` with your SQL Server connection string:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=YOUR_SERVER;Database=DonacionesDB2;Integrated Security=True;TrustServerCertificate=True;"
     }
   }
   ```

3. **Create the database**

   Run the SQL scripts located in the `Scripts/` folder in order against your SQL Server instance to set up the required schema.

4. **Build and run**
   ```bash
   dotnet build
   dotnet run
   ```
   Or open `RamaFemenina.csproj` in Visual Studio and press **F5**.

5. **Default credentials**

   On first launch with an empty user table, log in with:
   | Username | Password |
   |---|---|
   | `admin` | `admin123` |

   > ?? **Change the default password immediately after first login.**

## ??? PDF & Printing

The application generates PDFs optimized for **half-letter format** (5.5" × 8.5") to work seamlessly with **Epson LX-350** dot-matrix printers. PDF generation uses **iText 7** with:

- Zero-margin layouts for precise printer alignment
- Configurable viewer preferences (no scaling, simplex mode)
- Automatic logo detection and ICO-to-PNG conversion
- Spanish number-to-words conversion for receipt amounts

## ?? Security

- Passwords are hashed using **BCrypt** with a work factor of 12
- Automatic migration from plain-text passwords to BCrypt hashes
- Role-based UI restrictions enforced at the navigation level
- SQL injection prevention through Entity Framework parameterized queries
- Transient fault handling with automatic retry policies (up to 5 retries)

## ??? Database

The application uses **SQL Server** with **Entity Framework Core 8** and includes:

- Optimized indexes for frequently queried columns
- Value converters for NULL handling
- Change tracking optimization for read-heavy workloads
- Command timeout configuration (60 seconds)
- Lazy loading disabled for predictable query performance

## ?? Key Dependencies

| Package | Version | Purpose |
|---|---|---|
| Microsoft.WindowsAppSDK | 1.8.x | WinUI 3 framework |
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.11 | SQL Server ORM |
| itext7 | 8.0.2 | PDF generation |
| DevExpress.Reporting.Core | 25.1.7 | Advanced reporting |
| BCrypt.Net-Next | 4.0.3 | Password hashing |

## ?? Contributing

This is a private project for **Rama Femenina Contra el Cáncer, Inc.** Contributions are managed internally.

## ?? License

This project is proprietary software developed for Rama Femenina Contra el Cáncer, Inc. All rights reserved.

---

<p align="center">
  Made with ?? for cancer patients in Santiago, Dominican Republic ????
</p>
