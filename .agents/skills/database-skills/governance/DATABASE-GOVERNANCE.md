## Rules Governance

Before performing any analysis, design, optimization, or modification related to databases, the agent must determine which governance rules apply to the project.

### Database-specific rules

The agent must look for:

`.\docs\rules\database`

If the folder exists, the agent must read the relevant rules before making decisions and apply them throughout the analysis.

The existence of this folder is **not mandatory**.

If the folder does not exist:

* The agent must continue with the task.
* The agent must not consider its absence an error.
* The agent must apply the principles and rules established by the project's general documentation governance.
* The agent must not invent undocumented specific rules.
* When relevant, the agent must state that no database-specific rules exist and that decisions were made using the general governance and the technical principles defined by this SKILL.

### Hierarchy

When database-specific rules exist, they must be interpreted within the documentation hierarchy defined by the project's governance.

The agent must not assume a specific rule is absolute when the documentation governance establishes priority mechanisms, exceptions, or context.

In case of conflict, the agent must follow the hierarchy defined by the documentation governance and document both the conflict and the decision made.

### Continuity principle

The absence of specific documentation must not prevent a technically valid task.

At a minimum, the agent must use the technical principles defined in this SKILL and in the other applicable database SKILLS.

The absence of a specific rule does not authorize arbitrary decisions.

### Traceability

When a relevant decision depends on a documented rule, the agent must state:

* which rule was used;
* where it comes from;
* how it affects the decision.

When no specific rule exists, the agent must distinguish between:

* **Governance rule:** an explicit project requirement.
* **SKILL principle:** a technical criterion established by this documentation.
* **Reasoned decision:** a decision made through technical analysis in the absence of a specific rule.

This makes it possible to distinguish the project's actual constraints from the agent's technical recommendations.
