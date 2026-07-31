---
name: paradigm-evolve-guidance
description: Promote an observed Paradigm.Enterprise engineering practice into governed guidance or deterministic tooling. Use when a review, incident, repeated correction, or new framework lesson should become a fixture, validation rule, built-in semantic check, reviewed fixer, skill instruction, test, or documented convention.
---

# Evolve Paradigm guidance

Read and preserve [Paradigm Good Coding Practices](../../references/good-coding-practices.md) when changing guidance or tooling.

## Establish evidence

Capture the observed practice as a minimal reproducible fixture. Record expected behavior, false-positive boundaries, and why the existing guidance or checks did not prevent it. Do not promote preference or a one-off workaround as a universal rule.

## Choose the strongest safe control

Promote in this order:

1. Add a deterministic built-in metadata rule when restored assemblies contain sufficient evidence.
2. Add a built-in `paradigm checks` rule when semantic source analysis is required.
3. Add a reviewed standalone fixer/script only for repeatable transformations. Require `--dry-run`; never invoke it from validation or PR review.
4. Add concise skill guidance only when judgment is unavoidable.

Never add implicit extension discovery, downloads, execution, or runtime-framework reflection to enforce application policy. Give each diagnostic stable severity, ownership, message, location, suppression requirements, and deterministic ordering.

## Govern the change

- Add positive, negative, exception, suppression-expiry, and ordering fixtures.
- Document generated/persistence exceptions explicitly; require suppression code, symbol/location, reason, and expiry.
- Update affected implementation/review skills without duplicating long reference material.
- Update CLI/check documentation, configuration examples, and changelog.
- Do not add compatibility branches or parallel implementations for unreleased CLI designs. Package metadata may carry a release version without making runtime behavior version-dependent.
- Run restore, build, tests, skill/plugin validation, CLI commands, and Docfx with warnings as errors.

Report what evidence became deterministic, what remains judgment-based, and the smallest future signal that would justify promoting the remaining guidance.
