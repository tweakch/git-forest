# git-forest CLI v0.2 (improved)

Design goals: **idempotent**, **deterministic IDs**, **reconcile desired state**, **automation-friendly output**, **clear ownership**, **safe concurrency**.

---

# 1. Core model

## Entities

**Plan**

* Versioned package defining desired forest intent: planners, planters, plant templates, scopes, policies.
* Source: GitHub slug/URL, local path.

**Planner**

* Deterministic generator that produces a **desired set** of Plants from a Plan + repo context.

**Plant**

* Concrete work item with a stable **key** and lifecycle facts (assignments, branches, candidates, harvest results).

**Planter**

* Executor persona (agent) that proposes diffs/PRs for a plant, under policies.

**Forest**

* Repo-local state under `.git-forest/` + optional user config.

## Identity

* **Stable Plant Key**: `planId:plantSlug` (e.g. `sample:backend-memory-hygiene`)
* Optional display **Seq** per plan for humans (`P01`, `P02`), derived at render time (not stored as identity).

## Ownership rules (source of truth)

Each plant field is either:

* **Plan-owned** (updated by `gf plan --reconcile`)
* **User-owned** (never overwritten)
* **Planter-owned** (written by planters: candidates, analyses, health signals)

Drift is tracked (`plan_owned_hash`, `user_overrides`).

---

# 2. Layout

```text
gf init
gf status
gf config ...

gf plans ...
gf plan <plan-id> ...

gf plants ...
gf plant <plant-key|selector> ...

gf planners ...
gf planner <planner-id> ...

gf planters ...
gf planter <planter-id> ...
```

Conventions:

* Every read/list command supports `--json`.
* Mutating commands support `--dry-run` and `--yes`.
* Exit codes are consistent (see §11).

---

# 3. Init & status

## 3.1 `gf init`

Initialize in current git repo.

```bash
gf init [--force] [--dir .git-forest]
```

Behavior:

* Creates `.git-forest/` with minimal config + lock file.
* Detects default branch + remotes.
* Writes `.git-forest/forest.yaml`.

Output (human):

```text
initialized (.git-forest)
```

Output (json):

```bash
gf init --json
```

## 3.2 `gf status`

```bash
gf status [--json]
```

Human output example:

```text
Forest: initialized  Repo: origin/main
Plans: 1 installed (sample@v1.2.0)
Plants: planned 15 | planted 1 | growing 0 | harvestable 0 | harvested 0
Planters: 24 available | 2 active
Lock: free
Hints: gf plants list --status planned
```

---

# 4. Config

Config layers:

1. user-level (optional): `~/.git-forest/config.yaml`
2. repo-level: `.git-forest/config.yaml`
3. plan defaults (read-only, from plan package)

## 4.1 `gf config show`

```bash
gf config show [--effective] [--json]
```

## 4.2 `gf config get|set|unset`

```bash
gf config get <path> [--json]
gf config set <path> <value> [--scope user|repo]
gf config unset <path> [--scope user|repo]
```

Example paths:

* `git.remote=origin`
* `git.baseBranch=main`
* `branches.template="{planter}/{plantSlug}"`
* `llm.profile.default=gpt-forest-backend-v1`
* `execution.mode=manual|auto`
* `locks.timeoutSeconds=15`

---

# 5. Plans

## 5.1 `gf plans list`

```bash
gf plans list [--json]
```

## 5.2 `gf plans install`

```bash
gf plans install <source> [--id <plan-id>] [--ref <tag|branch|sha>] [--force] [--json]
```

Sources:

* `tweakch/git-forest-plans/sample`
* `https://github.com/...`
* `./local/plan`

Stores under: `.git-forest/plans/<plan-id>/`

## 5.3 `gf plans show`

```bash
gf plans show <plan-id> [--json]
```

## 5.4 `gf plans remove`

```bash
gf plans remove <plan-id> [--purge-plants] [--yes]
```

---

# 6. Planning (reconcile)

## 6.1 `gf plan <plan-id> reconcile`

Default action is reconcile.

```bash
gf plan <plan-id> [reconcile] [--update] [--only <scope>] [--dry-run] [--json]
```

**Contract (important):**

* Deterministic generation: same plan+repo context → same plant keys.
* Reconcile computes **desired state** then applies:

  * create missing plants
  * update plan-owned fields
  * mark removed plants as `archived` (never delete unless `--purge`)
* Never overwrites user-owned fields.

Human output:

```text
Reconciling plan 'sample@v1.2.0'...
Planners: +2 ~0 -0
Planters: +3 ~1 -0
Plants:   +4 ~6 -0 (archived 0)
done
```

## 6.2 `gf plan <plan-id> diff`

```bash
gf plan <plan-id> diff [--only <scope>] [--json]
```

Shows what reconcile would change, without applying.

---

# 7. Plants

## 7.1 Plant selectors

A plant may be addressed by:

* full key: `sample:backend-memory-hygiene`
* short forms:

  * `P01` (rendered index for current plan context)
  * slug `backend-memory-hygiene` if unambiguous

Resolution rules are defined and errors are explicit.

## 7.2 `gf plants list`

```bash
gf plants list
  [--status planned|planted|growing|harvestable|harvested|archived]
  [--plan <plan-id>]
  [--planter <planter-id>]
  [--scope <scope>]
  [--search <text>]
  [--json]
```

Human example:

```text
Key                             Status   Title                         Plan   Planter
sample:reduce-tech-debt          planned  Reduce technical debt          sample -
sample:backend-memory-hygiene    planted  Backend memory hygiene         sample backend
```

## 7.3 `gf plant show`

```bash
gf plant <selector> [show] [--json]
```

Shows:

* identity (key, slug, plan)
* ownership / drift markers
* assigned planters
* branch(es)
* candidates count
* last activity

## 7.4 `gf plant set` (user overrides)

User-owned updates only.

```bash
gf plant <selector> set <path> <value>
gf plant <selector> unset <path>
```

Example:

```bash
gf plant sample:backend-memory-hygiene set priority high
gf plant sample:backend-memory-hygiene set notes "Focus on pooling + Span usage"
```

## 7.5 `gf plant history|logs|candidates|branches`

```bash
gf plant <selector> history [--json]
gf plant <selector> logs [--tail 200]
gf plant <selector> candidates list [--json]
gf plant <selector> branches list [--json]
```

---

# 8. Planters (agents)

## 8.1 `gf planters list`

```bash
gf planters list [--builtin|--custom] [--origin plan|user] [--json]
```

## 8.2 `gf planter <id> show`

```bash
gf planter <planter-id> show [--json]
```

## 8.3 Assignment (plant-centric + planter-centric)

Both forms supported:

```bash
gf plant <selector> assign <planter-id> [--capacity 1]
gf planter <planter-id> assign <selector> [--capacity 1]
```

Idempotent:

* assigning same planter twice is a no-op.

## 8.4 `gf planter <id> plant` (alias)

Alias for `assign` + optional branch creation.

```bash
gf planter <planter-id> plant <selector> [--branch auto|<name>] [--yes] [--dry-run]
```

Branch policy:

* `auto` uses `branches.template` from config.
* Branch creation is skipped if already exists unless `--force-branch`.

## 8.5 `gf planter <id> grow`

```bash
gf planter <planter-id> grow <selector>
  [--mode propose|apply]
  [--max-diffs 3]
  [--risk low|medium|high]
  [--dry-run]
  [--json]
```

Contract:

* `propose` creates candidate diffs only.
* `apply` may open PRs / apply patches if configured and allowed.

## 8.6 Unassign

```bash
gf plant <selector> unassign <planter-id>
gf planter <planter-id> unassign <selector>
```

---

# 9. Planners

## 9.1 `gf planners list`

```bash
gf planners list [--plan <plan-id>] [--json]
```

## 9.2 `gf planner run`

```bash
gf planner <planner-id> run --plan <plan-id> [--only <scope>] [--dry-run] [--json]
```

Notes:

* Planner output is always reconciled through the same engine as `gf plan reconcile`.

---

# 10. Concurrency & locking

All mutating commands acquire a repo lock:

* lock file: `.git-forest/lock`
* timeout: `locks.timeoutSeconds`

If lock cannot be acquired:

* exit code `23`
* message includes owner + held duration.

---

# 11. Exit codes (stable for automation)

* `0` success
* `2` invalid arguments / parse error
* `10` forest not initialized
* `11` plan not found
* `12` plant not found / ambiguous selector
* `13` planter not found
* `20` schema validation failed
* `23` lock timeout / busy
* `30` git operation failed
* `40` execution not permitted by policy (e.g. apply PRs disabled)

---

# 12. Minimal on-disk contract (v0)

```text
.git-forest/
  forest.yaml
  config.yaml
  lock
  plans/<plan-id>/...
  plants/<planId__slug>/
    plant.yaml
    history.log
    branches.yaml
    candidates/
  planters/<planter-id>/
    planter.yaml
    state/<planId__slug>.json
  planners/<planner-id>/planner.yaml
  logs/
```

---

If you want, I can also produce:

* a **sample `plan.yaml`** that generates your “15 planned plants”
* the **JSON schemas** (plan/plant/planter) so `install`/`reconcile` can validate deterministically.
