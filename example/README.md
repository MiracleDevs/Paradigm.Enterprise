# 🚀 Paradigm.Enterprise Example Project

Welcome to the coolest example of how to use the Paradigm.Enterprise framework! This sample project shows you how to build a slick, modern web API using clean architecture principles without all the usual headaches.

## 🛠️ What's Inside

This example app demonstrates a product catalog system with:

- **Domain entities** with built-in validation
- **Repository pattern** for data access
- **Provider pattern** for business logic
- **RESTful API** for client applications

## 🏗️ Project Structure

- **ExampleApp.Interfaces** - Contracts and DTOs
- **ExampleApp.Domain** - Domain entities and business rules
- **ExampleApp.Data** - Data access and repositories
- **ExampleApp.Providers** - Business logic providers
- **ExampleApp.WebApi** - API controllers and endpoints

## 🔍 How It Works

1. **Entities** enforce business rules through validation
2. **Repositories** handle data access operations
3. **Providers** orchestrate business processes
4. **Controllers** expose functionality via REST endpoints

## 🏃‍♂️ Running the Example

```bash
# Navigate to the WebApi project
cd ExampleApp.WebApi

# Run the application
dotnet run

# API will be available at:
# https://localhost:7050/swagger
```

## 📱 API Endpoints

| Method | Endpoint                   | Description                      |
|--------|----------------------------|----------------------------------|
| GET    | /api/products              | Get all products                 |
| GET    | /api/products/{id}         | Get product by ID                |
| GET    | /api/products/category/{c} | Get products by category         |
| GET    | /api/products/available    | Get available products           |
| POST   | /api/products              | Create a new product             |
| PUT    | /api/products/{id}         | Update an existing product       |
| PATCH  | /api/products/{id}/stock/{qty} | Update product stock level   |
| DELETE | /api/products/{id}         | Delete a product                 |

## 💡 Key Takeaways

- **Clean Architecture**: Separation of concerns makes code more maintainable
- **DI Container**: Services are registered and injected automatically
- **Entity Framework**: Simplified data access with InMemory provider
- **Validation**: Domain-driven validation ensures data integrity
- **RESTful API**: Standard HTTP methods and status codes 