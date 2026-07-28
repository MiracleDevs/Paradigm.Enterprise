# Framework overview

Paradigm.Enterprise exists to make a familiar backend architecture consistent across projects. It is not a general-purpose application framework and it does not hide ASP.NET Core or Entity Framework Core. Instead, it gives teams shared contracts and base implementations for the parts of a layered API that otherwise drift between solutions.

The central design choice is to separate protocol, orchestration, domain behavior, and persistence. Controllers translate HTTP concerns. Providers coordinate application use cases and can be reused from a controller, worker, or message handler. Entities protect business invariants. Repositories express persistence operations. A Unit of Work coordinates commits without giving providers direct access to a `DbContext`.

This separation is useful only when the boundaries remain explicit. A thin controller is not a goal by itself; it is evidence that HTTP details have not leaked into the use case. A small repository is not wasted ceremony; it keeps query and storage choices out of domain code. A provider is not a second place for entity rules; it coordinates work that spans collaborators.

## Read and write models

The libraries support separate table-backed entities and read-oriented views. A write provider receives a view, maps it to an entity, asks the entity to validate itself, persists through an edit repository, commits, and then reads the result through the view repository. A query can use a database view, projection, or stored procedure without changing the entity used for writes.

This split is a library convention, not a claim that every endpoint needs a database view. A simple application may use similar shapes for both paths. The distinction becomes valuable when queries need joins, calculated fields, or performance characteristics that do not belong in the aggregate.

## Packages as building blocks

The core dependency chain begins with `Interfaces`, continues through `Domain`, `Data`, and `Providers`, and ends at `WebApi`. SQL Server and PostgreSQL packages supply database-specific connection and stored-procedure support. Service packages for caching, email, blob storage, and tabular files can be adopted independently.

Installing `Paradigm.Enterprise.WebApi` brings the main dependency chain transitively, but an application should still reference packages in the projects that use their types. See the [package matrix](reference/packages.md) for target frameworks and responsibilities.

## A deliberate amount of convention

Several registration helpers discover types by reflection. A public `OrderProvider` is expected to implement `IOrderProvider`; the same naming rule applies to repositories. This is executable behavior, not a cosmetic naming preference. Generated interfaces, partial entity classes, JSON serializer contexts, and mapper registration add other conventions that must be understood before regeneration.

The [conventions reference](reference/conventions.md) collects these rules. The tutorials introduce each convention when it first matters.
