# RFC: Standardizing MIT License Information and Central Package Management in NuGet Packages

- **RFC ID**: 2025-05-28-license-info-nuget
- **Status**: Draft
- **Author(s)**: Iván Falletti <ivan@miracledevs.com>
- **Created**: 2025-05-28
- **Last Updated**: 2025-05-28

## Summary

This RFC proposes two standardization approaches for our NuGet packages:
1. Adding MIT license information to ensure proper license recognition
2. Implementing Central Package Management (CPM) to standardize package versions across all projects

While the repository already includes a LICENSE file at the root, the first change ensures that the license information is properly recognized by NuGet package consumers. The second change provides a centralized approach to managing package versions, reducing inconsistencies and simplifying updates.

## Motivation

### License Information

Currently, our NuGet packages do not explicitly include license information in their package metadata. This creates several issues:

1. Package consumers cannot easily determine the license terms without examining the repository
2. NuGet package listings do not display license information properly
3. Automated license compliance tools may flag our packages as "unknown license"
4. Potential users may be hesitant to use packages without clear license information

### Central Package Management

Additionally, our current approach to package versioning has several drawbacks:

1. Package versions are defined individually in each project file
2. Version inconsistencies can occur across projects
3. Updating packages requires changes to multiple files
4. There's no single source of truth for package versions

By standardizing both license information and package version management, we can improve discoverability, consistency, and maintainability of our packages.

## Detailed Design

### Part 1: License Information

We propose adding standardized license information to all Paradigm Enterprise NuGet packages by:

1. Adding the `PackageLicenseExpression` property to a central `Directory.Build.props` file in the `src` directory
2. Ensuring this property is inherited by all project files

#### Directory.Build.props Implementation

Create a `Directory.Build.props` file in the `src` directory with the following content:

```xml
<Project>
  <PropertyGroup>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
  </PropertyGroup>
</Project>
```

This approach uses MSBuild's directory-based build customization to apply the license property to all projects in the directory tree without modifying individual project files.

#### License Expression

We will use the SPDX identifier "MIT" as the license expression, which is recognized by NuGet and complies with the SPDX standard for license identification.

### Part 2: Central Package Management

We propose implementing Central Package Management to standardize package versions by:

1. Creating a `Directory.Packages.props` file in the `src` directory
2. Moving all package version information to this central file
3. Removing version attributes from individual project files

#### Directory.Packages.props Implementation

Create a `Directory.Packages.props` file in the `src` directory with the following content:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>

  <ItemGroup>
    <!-- Entity Framework -->
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="9.0.3" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Relational" Version="9.0.3" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.3" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.0.3" />
    <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.3" />

    <!-- Microsoft Extensions -->
    <PackageVersion Include="Microsoft.Extensions.Configuration" Version="9.0.3" />
    <PackageVersion Include="Microsoft.Extensions.Configuration.Binder" Version="9.0.3" />
    <PackageVersion Include="Microsoft.Extensions.Configuration.Json" Version="9.0.3" />
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection" Version="9.0.3" />
    <PackageVersion Include="Microsoft.Extensions.Logging" Version="9.0.3" />
    <PackageVersion Include="Microsoft.Extensions.Options" Version="9.0.3" />
    <PackageVersion Include="Microsoft.Extensions.Caching.Memory" Version="9.0.3" />

    <!-- ASP.NET Core -->
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Core" Version="2.2.5" />
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Abstractions" Version="2.2.0" />
  </ItemGroup>
</Project>
```

#### Project File Updates

Remove the `Version` attribute from all `PackageReference` elements in project files. For example:

Before:
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.3" />
```

After:
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" />
```

### Package Metadata Impact

After implementing these changes:

- All packages will include a `license` field with the SPDX expression "MIT"
- Package dependencies will use versions defined in the central file
- NuGet gallery and package manager interfaces will properly display license information
- Package updates can be managed from a single source

## Alternatives Considered

### Individual Project File Updates for License

We considered adding the `PackageLicenseExpression` property to each individual project file.

Advantages:
- More explicit, with license information visible in each project file
- Allows for different licenses per project if needed in the future

Disadvantages:
- Requires modifying multiple files
- Creates maintenance overhead for future changes
- More prone to inconsistencies across projects

### Using PackageLicenseFile Instead

We considered using `PackageLicenseFile` to include the actual LICENSE file in each package.

Advantages:
- Includes the full license text in the package
- Ensures the exact license wording is always available

Disadvantages:
- Requires including a copy of the license in each package
- Increases package size slightly
- More complex to maintain than using a standard expression

### No Central Package Management

We considered continuing with the current approach of specifying versions in individual project files.

Advantages:
- More flexibility for project-specific version requirements
- No changes required to existing workflow

Disadvantages:
- Difficult to maintain consistency across projects
- Updates require changes to multiple files
- No single source of truth for package versions

## Testing Strategy

1. Build sample NuGet packages locally and verify the license information is included in the package metadata
2. Verify the license information appears correctly in NuGet Package Explorer
3. Test package builds with CPM to ensure correct versions are applied
4. Validate that the license information is displayed correctly when packages are installed
5. Ensure all packages build correctly with the new CPM approach

## Rollout Plan

1. Create the `Directory.Build.props` file in the `src` directory
2. Create the `Directory.Packages.props` file in the `src` directory
3. Update all project files to remove version attributes from PackageReferences
4. Build and publish updated packages with the next version increment
5. Document the changes in release notes

This change is backward compatible and requires no special migration steps for consumers.

## Dependencies

This RFC depends on:
- Existing MIT license file in the repository root
- NuGet packaging infrastructure
- MSBuild 16.3 or higher (for Central Package Management)

## Open Questions

- Should we also update the license copyright year in the LICENSE file to reflect the current year?
- Should we consider adding a license notice to the source code files as well?
- Are there any packages that require specific version constraints that should not be centrally managed?

## References

- [NuGet PackageLicenseExpression documentation](https://learn.microsoft.com/en-us/nuget/reference/nuspec#license)
- [SPDX License List](https://spdx.org/licenses/)
- [MSBuild Directory.Build.props documentation](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-your-build)
- [Central Package Management documentation](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management)