# Paradigm.Enterprise Framework Overview

The Paradigm.Enterprise framework is a comprehensive set of .NET libraries designed to accelerate the development of enterprise web applications, particularly WebAPIs. The framework provides a solid foundation for implementing best practices, common design patterns, and standardized application paths.

## Core Concepts

The framework is built around several key concepts:

1. **Clean Architecture** - The framework follows a clean architecture approach with clear separation of concerns between layers.
2. **Domain-Driven Design** - Provides base classes for entities, repositories, and services.
3. **Repository Pattern** - Standardized data access through repository abstractions.
4. **Unit of Work** - Manages transactions and data persistence.
5. **Provider Pattern** - Simplifies business logic implementation and service orchestration.
6. **CQRS Principles** - Separate read and write operations for better scalability.

## Framework Structure

The framework is organized into several NuGet packages, each targeting specific functionality:

### Core Libraries
- **Interfaces** - Core interfaces used throughout the framework
- **Domain** - Base domain models, entities, and value objects
- **Data** - Database access abstractions and implementations
- **Providers** - Business logic implementation patterns

### Database Providers
- **Data.SqlServer** - Microsoft SQL Server implementation
- **Data.PostgreSql** - PostgreSQL implementation

### Service Libraries
- **Services.Core** - Base service interfaces and implementations
- **Services.BlobStorage** - File/blob storage abstractions
- **Services.Cache** - Caching mechanisms
- **Services.Email** - Email service abstractions

### API Libraries
- **WebApi** - API controllers, filters, and middleware

## Getting Started

To get started with the Paradigm.Enterprise framework, add the relevant NuGet packages to your project:

```csharp
Install-Package Paradigm.Enterprise.WebApi
```

This will bring in all the necessary dependencies for a typical WebAPI project.

## Versioning and Compatibility

The framework follows semantic versioning. The current version is 1.0.11.

## License

This project is licensed under the MIT License - see the LICENSE file for details. 