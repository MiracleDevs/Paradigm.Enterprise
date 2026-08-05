# Paradigm pull-request review

When reviewing Paradigm.Enterprise code, use `paradigm-review-change`. Apply its checks only to files and capabilities affected by the change.

Run or verify evidence from:

```text
paradigm packages check --project <solution>
paradigm validate --project <solution>
paradigm checks run --project <solution>
```

For dependency changes also run `paradigm packages audit --project <solution>`. Treat vulnerabilities, version misalignment, below-minimum packages, incomplete audits, exposed `IQueryable`, and external entity-state mutation as errors. Treat public setters on handwritten entities, EF pagination outside a stored-procedure boundary, deprecations, and newer stable direct packages according to their reported severity.

Report only evidence-backed findings, ordered by severity, with file/line, consequence, and the smallest safe correction. State remaining verification gaps when no defect is found.
