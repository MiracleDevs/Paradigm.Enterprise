# EF Core Power Tools template provenance

These templates are derived from the official `Paradigm.Web.ApiTemplate` repository:

- Remote: `https://github.com/MiracleDevs/Paradigm.Web.ApiTemplate`
- Revision: `522906b9151d566a5dddd2609b66da36726368a7`
- Upstream `EntityType.t4` SHA-256: `0EE9962D4D4757EB4DD7C3234597577A511EFD8610341078ED4C3408251BB8BE`
- Upstream `DbContext.t4` SHA-256: `70E703CC0E043B0C8441A1B8B9E126FD961E69E352DCE31F27D2E3B2AAC78049`

The source clone is read-only. Beacon carries only reusable compatibility adaptations needed by EF Core Power Tools 10.1.1386 and Paradigm.Enterprise 1.1:

- database views and entity/view pairs are detected from EF relational metadata and the universal `View` suffix;
- entity identifier CLR types are inferred and supplied to current generic `EntityBase<TId>`, `EntityMapperBase<TId,...>`, and interface contracts;
- both audit actor columns are inspected, normalized, and required to have the same identifier type; the current `IAuditableEntity<TDate,TId>` contract is emitted only when actor and entity identifiers match, so a `long` entity with `int` actors does not receive an invalid interface;
- the context derives one unambiguous audit actor identifier type from audited entity metadata and supplies it to `DbContextBase<TActorId>` with the current service-provider constructor;
- output includes the EF Core Power Tools ownership header and compiled `GeneratedCode("EFCorePowerTools", "10.1.1386")` metadata;
- relational collection navigations remain ordinary persistence collections because aggregate ownership is not safely inferable from database metadata; application partials add domain trackers and behavior deliberately.

There are no Beacon entity names, relationship names, or public-view whitelists in either T4 file. Application-specific relationship corrections belong in partial context configuration, and domain behavior belongs in non-generated partial entity files.

To resynchronize, compare both official `src/Paradigm.Web.ApiTemplate.Data/CodeTemplates/EFCore` files at a reviewed revision, verify their hashes, reapply only compatibility items still required by the installed APIs, regenerate twice from a disposable SQL Server database, compare the complete owned output byte-for-byte, build, and review the generated diff. When upstream implements the same generic behavior, remove the matching local delta.
