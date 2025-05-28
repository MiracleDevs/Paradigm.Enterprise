# RFC: Standardizing MIT License Information in NuGet Packages

- **RFC ID**: 2025-05-28-license-info-nuget
- **Status**: Draft
- **Author(s)**: Iván Falletti <ivan@miracledevs.com>
- **Created**: 2025-05-28
- **Last Updated**: 2025-05-28

## Summary

This RFC proposes standardizing the inclusion of MIT license information in all Paradigm Enterprise NuGet packages. While the repository already includes a LICENSE file at the root, this change ensures that the license information is properly recognized by NuGet package consumers and displayed correctly in package metadata.

## Motivation

Currently, our NuGet packages do not explicitly include license information in their package metadata. This creates several issues:

1. Package consumers cannot easily determine the license terms without examining the repository
2. NuGet package listings do not display license information properly
3. Automated license compliance tools may flag our packages as "unknown license"
4. Potential users may be hesitant to use packages without clear license information

By standardizing the inclusion of license information in our package metadata, we can improve the discoverability and adoption of our packages while ensuring compliance with licensing best practices.

## Detailed Design

We propose adding standardized license information to all Paradigm Enterprise NuGet packages by:

1. Adding the `PackageLicenseExpression` property to a central `Directory.Build.props` file in the `src` directory
2. Ensuring this property is inherited by all project files

### Directory.Build.props Implementation

Create a `Directory.Build.props` file in the `src` directory with the following content:

```xml
<Project>
  <PropertyGroup>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
  </PropertyGroup>
</Project>
```

This approach uses MSBuild's directory-based build customization to apply the license property to all projects in the directory tree without modifying individual project files.

### License Expression

We will use the SPDX identifier "MIT" as the license expression, which is recognized by NuGet and complies with the SPDX standard for license identification.

### Package Metadata Impact

After implementing this change, the generated NuGet packages will include:

- A `license` field in the package metadata
- The SPDX expression "MIT" as the license value
- Proper license display in NuGet gallery and package manager interfaces

## Alternatives Considered

### Individual Project File Updates

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

## Testing Strategy

1. Build sample NuGet packages locally and verify the license information is included in the package metadata
2. Verify the license information appears correctly in NuGet Package Explorer
3. Validate that the license information is displayed correctly when packages are installed

## Rollout Plan

1. Create the `Directory.Build.props` file in the `src` directory
2. Build and publish updated packages with the next version increment
3. Document the change in release notes

This change is backward compatible and requires no special migration steps for consumers.

## Dependencies

This RFC depends on:
- Existing MIT license file in the repository root
- NuGet packaging infrastructure

## Open Questions

- Should we also update the license copyright year in the LICENSE file to reflect the current year?
- Should we consider adding a license notice to the source code files as well?

## References

- [NuGet PackageLicenseExpression documentation](https://learn.microsoft.com/en-us/nuget/reference/nuspec#license)
- [SPDX License List](https://spdx.org/licenses/)
- [MSBuild Directory.Build.props documentation](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-your-build) 