# VRLCRM Project Analysis

## Overview
VRLCRM is a comprehensive CRM (Customer Relationship Management) and Stock Management system built using **.NET Core 8.0 MVC**. It utilizes a modern administrative template (Materio Bootstrap 5) for its user interface.

## Architectural Structure
The project follows **Clean Architecture** principles, ensuring separation of concerns and maintainability.

### 1. VRLCRM.Domain (Core Layer)
Contains the enterprise logic and entities.
- **Entities:** `Customer`, `Supplier`, `StockItem`, `Category`, `Order`, `OrderLine`, `Invoice`, `InvoiceLine`, `StockMovement`, `ApplicationUser`, `ApplicationRole`.
- **Enums:** `OrderStatus`, `InvoiceType`, `StockMovementType`, `StockMovementReferenceType`.
- **Common:** Base entity definitions (`BaseEntity`).

### 2. VRLCRM.Application (Service Interfaces)
Defines the business logic interfaces and contracts.
- **Services:** `ICustomerService`, `ISupplierService`, `IStockService`, `ICategoryService`, `IOrderService`, `IInvoiceService`, `IStockMovementService`.
- **Exceptions:** Custom application-level exceptions.

### 3. VRLCRM.Infrastructure (Data & External Services)
Handles data persistence and implementation of application services.
- **ORM:** Entity Framework Core with `ApplicationDbContext`.
- **Database:** Support for migrations and seed data (`SeedData.cs`).
- **Implementations:** Concrete implementations of services defined in the Application layer.

### 4. VRLCRM (Web/Presentation Layer)
The main entry point and UI.
- **Controllers:** Manage web requests for each module (e.g., `CustomersController`, `StocksController`, `OrdersController`).
- **Views:** Razor views using the Materio Bootstrap 5 template.
- **Models:** ViewModels for data transfer between controllers and views.
- **Services:** Web-specific services like `CustomerCartService`, `InvoiceDocumentService`, and `StockImageStorage`.

## Technology Stack
- **Backend:** C# / .NET Core 8.0 MVC
- **Database:** Entity Framework Core
- **Frontend:** Bootstrap 5, SCSS, JavaScript (ES6+), jQuery
- **Build Tools:** Gulp, Webpack (for asset management)
- **Logging:** Serilog
- **Authentication/Authorization:** ASP.NET Core Identity

## Key Modules
- **Customer Management:** Basic CRM features to manage customers and their details.
- **Stock Management:** Tracking stock items, categories, and images.
- **Commercial Module:** Management of Orders, Invoices (Purchase/Sales), and Suppliers.
- **Stock Movements:** Automated tracking of stock changes based on orders and invoices.

## Project Configuration
- `Program.cs`: Configures services, middleware, Serilog, and routing.
- `appsettings.json`: Contains environment-specific configurations.
- `package.json`: Manages frontend dependencies and build scripts.
