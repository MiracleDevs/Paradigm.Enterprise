# 1. Paradigm.Enterprise.CodeGenerator

The CodeGenerator project is a utility library that provides code generation capabilities for the Paradigm.Enterprise framework. It automates the creation of boilerplate code for entities, repositories, providers, controllers, and other components, increasing developer productivity and ensuring consistency across the codebase.

## 1.1. Key Components

### 1.1.1. Code Generators

The project includes various code generators for different aspects of the application:

- **EntityGenerator** - Generates entity classes from database schema or model definitions
- **RepositoryGenerator** - Creates repository implementations for entities
- **ProviderGenerator** - Generates provider classes with standard CRUD operations
- **ControllerGenerator** - Creates API controllers with standard endpoints
- **DtoGenerator** - Generates Data Transfer Objects for view and edit operations

### 1.1.2. Templates

The CodeGenerator uses a templating system to generate code:

- **T4 Templates** - Text template transformation toolkit templates
- **Handlebars Templates** - Logic-less templates for code generation
- **Razor Templates** - Syntax for embedding C# in templates

### 1.1.3. Model Discovery

The project includes utilities for discovering and analyzing existing code:

- **SchemaAnalyzer** - Analyzes database schema for entity generation
- **CodeAnalyzer** - Parses existing code for relationship discovery
- **TypeScanner** - Scans assemblies for relevant types

### 1.1.4. Configuration

Configuration options for code generation:

- **CodeGenOptions** - Configuration for code generation behavior
- **NamingConventions** - Rules for naming generated artifacts
- **TemplateSettings** - Customization options for templates

## 1.2. Usage Example

### 1.2.1. Command Line Interface

```shell
# Generate a complete set of components for an entity
dotnet run --project src/Paradigm.Enterprise.CodeGenerator -- generate entity Product --properties "Name:string,Description:string,Price:decimal,CategoryId:int" --output ./src/MyApp

# Generate a controller for an existing entity
dotnet run --project src/Paradigm.Enterprise.CodeGenerator -- generate controller Product --namespace MyApp.Controllers --output ./src/MyApp.Api
```

### 1.2.2. Programmatic Usage

```csharp
// Example of programmatic usage
var generator = new EntityGenerator
{
    EntityName = "Customer",
    Namespace = "MyApp.Domain.Entities",
    Properties = new[]
    {
        new PropertyDefinition { Name = "FirstName", Type = "string" },
        new PropertyDefinition { Name = "LastName", Type = "string" },
        new PropertyDefinition { Name = "Email", Type = "string" },
        new PropertyDefinition { Name = "IsActive", Type = "bool", DefaultValue = "true" }
    },
    OutputDirectory = "./src/MyApp.Domain"
};

generator.Generate();

// Generate a complete set of components
var projectGenerator = new ProjectGenerator
{
    EntityName = "Order",
    BaseNamespace = "MyApp",
    Properties = new[]
    {
        new PropertyDefinition { Name = "OrderDate", Type = "DateTime" },
        new PropertyDefinition { Name = "CustomerId", Type = "int" },
        new PropertyDefinition { Name = "TotalAmount", Type = "decimal" },
        new PropertyDefinition { Name = "Status", Type = "string" }
    },
    OutputDirectory = "./src/MyApp",
    GenerateEntity = true,
    GenerateRepository = true,
    GenerateProvider = true,
    GenerateController = true,
    GenerateDtos = true
};

projectGenerator.Generate();
```

## 1.3. Generated Code Structure

The CodeGenerator produces a consistent structure for generated code:

```shell
MyApp/
├── Domain/
│   └── Entities/
│       └── Product.cs
├── Data/
│   └── Repositories/
│       └── ProductRepository.cs
├── Api/
│   ├── Dtos/
│   │   ├── ProductViewDto.cs
│   │   └── ProductEditDto.cs
│   ├── Providers/
│   │   ├── IProductProvider.cs
│   │   └── ProductProvider.cs
│   └── Controllers/
│       └── ProductsController.cs
```

## 1.4. Customization

The CodeGenerator provides several ways to customize the generated code:

1. **Template Overrides** - Custom templates for specific components
2. **Configuration Files** - JSON configuration for generation rules
3. **Extension Points** - Hooks for custom code generation logic
4. **Post-Processors** - Custom processing of generated code

## 1.5. NuGet Package (Development Tool)

The CodeGenerator is typically used as a development tool rather than a runtime dependency:

```shell
dotnet tool install --global Paradigm.Enterprise.CodeGenerator
```

## 1.6. Benefits

1. **Consistency** - Ensures consistent implementation of design patterns
2. **Productivity** - Reduces boilerplate code writing
3. **Best Practices** - Enforces framework conventions and best practices
4. **Maintainability** - Generated code follows consistent patterns
5. **Flexibility** - Customizable templates for specific project needs
