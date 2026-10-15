# SMART INVENTORY MANAGER - AGENT RULES

## General Guidelines
- This file contains shared project-wide instructions. `.agent/RULE.md` is a local machine and team-member-specific instruction file; it may contain additional personal delivery rules and does not need to be identical to this file. Keep this file limited to rules that apply across the project. RULE.vi.md, when present, is a Vietnamese reference for reading and editing, not an additional instruction source. These project instructions do not override platform policies or agent permissions.
- Last synchronized: 2026-10-15. Use the current `.agent/SystemContract.md` when available, explicit subsequent user/team decisions, README.md, and Documentation/Architecture/ProjectStatus.md. `.agent` is local-only; never assume another clone includes it. Unresolved contract requirements are not proof of implemented functionality.

## Project Scope & Technology
- Build a multi-warehouse desktop application with C#, Windows Forms, Visual Studio, SQL Server, Entity Framework, ADO.NET, and LINQ.
- Inspect the solution, relevant code, .csproj, global.json, Directory.Build.props, and Directory.Packages.props when present before editing. Repository configuration determines actual versions.
- New solution default: Visual Studio 2026, .NET 10, EF Core 10.x. Preserve existing .NET 8/EF Core 8.x or .NET Framework/EF6 configurations. Do not mix EF6 and EF Core or migrate frameworks without a request.
- Keep Microsoft.EntityFrameworkCore.\* packages at the same exact compatible stable version; do not automatically upgrade major versions.
- Do not introduce web stacks, web APIs, or a different UI framework without a requirement.

### Technology authorization
- Approved core technologies: C#, the repository's current .NET target, Windows Forms, SQL Server, Entity Framework, LINQ, and ADO.NET. Reading documentation does not authorize adopting a new technology.
- Follow the repository's pinned versions and tools. Do not independently change framework, database, UI toolkit, architecture, package manager, or major dependency versions.
- A new library, service, hosted platform, alternative ORM, commercial UI toolkit, chart/export package, test framework, or AI integration requires explicit user/team authorization before installation or implementation. Do not introduce it indirectly through a helper or transitive integration.
- Reusing an existing approved dependency and restoring already declared packages are permitted. Installing a missing, explicitly approved core package is permitted only within the active task scope and compatible pinned version policy.
- If approval is missing, explain why the proposed addition is needed, existing alternatives, affected layers, licensing/cost where relevant, and wait before dependent work. Continue independent work using the approved stack.
- Course requirements are not permission to replace the stack. Do not add a library just to make the project look more modern.

### Seven-step YAGNI workflow
- Before coding, follow this order and stop at the first sufficient solution:
  1. Needed for the current requirement? If not, skip it.
  2. Already in the codebase? Reuse or extend it.
  3. Solved by the .NET standard library? Use it.
  4. Supported natively by WinForms, LINQ, EF, or SQL Server? Use it in the correct layer.
  5. Solved by an installed dependency? Reuse that dependency.
  6. Solved by one readable statement? Keep it short without sacrificing clarity or error handling.
  7. Only then write the minimum necessary custom code.
- Do not add speculative microservices, CQRS, event buses, generic frameworks, abstractions, empty files, or fake functionality.

## Team Ownership

### Ownership map
- **Bảo Anh — Database + Architecture + Data/EF/ADO.NET Infrastructure:** database, ERD, Entity models, EF DbContext, EF Configurations, EF Core Migrations, justified repositories/UnitOfWork, ADO.NET infrastructure, architecture, SQL-managed artifacts, constraints, indexes, views, and supporting stored procedures.
- **Khang — Business/Backend + Warehouse Operations:** Business Services, warehouse operations, imports, exports, inventory workflows, stock-alert backend, business validation, receipt state machine, stock updates, transfer/adjustment workflows, business events, and Business tests.
- **Khiêm — UI + Dashboard + Analytics + Reports + Notification UI:** Windows Forms UI, Dashboard, Analytics, charts, reports, notification UI, UI interactions, and presentation/query integration within agreed contracts.

### Active scope enforcement
- Before edits, identify the active owner, requested feature, allowed components/files, and shared contracts. Use explicit task instructions or documented assignment; never infer ownership merely from a login name, file author, or branch name.
- If the task does not establish an owner/scope, perform read-only inspection and ask for the missing scope before implementation. When the user assigns a clear feature, map it to the ownership above without asking redundantly.
- Default write scope is limited to the active owner's requested feature. Read other layers as needed to understand contracts; do not modify another owner's code, tests, configuration, or dependencies just to complete your part.
- A cross-owner change requires explicit authorization naming the affected component and purpose. Existing authorization applies; do not ask again for the same approved scope. A request to review or diagnose is not authorization to implement in other owners' components.
- For blocked cross-layer work, report the required contract/change and responsible owner, then continue independent work. Do not create replacement repositories, duplicate Services, ad hoc SQL, fake data, or workarounds that bypass ownership.
- Shared solution/project configuration, Common, DTOs/interfaces, authentication, and database contracts require the agreed project process and task authorization. Ownership defines responsibility, not unconditional permission to change every file in a layer.
- When older assignment documents conflict, use this latest agreed ownership map unless the user provides an explicit updated assignment. Do not treat UI file placement as evidence that Backend owns it.
- When a feature needs both a backend endpoint/service and UI integration, split changes by owner and state which part is implemented. Do not claim the whole feature is finished if another part remains.
- New roles, thresholds, state transitions, and analytics formulas require agreed business rules; never decide them solely to avoid coordination.

### Session-start synchronization
- At the beginning of every work session, before editing or implementing code, read the newly added or changed features from other team members that affect the current task.
- Inspect the related contracts, data models, DTOs, interfaces, service methods, and variable/property names so the work uses the names and shapes already established by the team.
- Use the current `develop` history and relevant task branches or diffs as the source of truth when checking recent cross-owner changes; do not rely on memory or recreate equivalent contracts.
- If a recent change is incomplete, conflicting, or missing a required decision, stop implementation of the dependent part and report the exact contract or naming issue to the responsible owner.

## Architecture
- UI: Forms, Controls, Components, Resources. Display data and call Services. No SQL, DbContext queries, or inventory business calculations in Forms.
- Business: Services, Interfaces, DTOs, Validators, Events. Enforce permissions, validate input, and execute business workflows.
- Data: Entities, DbContext, Configurations, and justified Repositories/UnitOfWork. Query and persist data; abstractions are optional, not mandatory.
- Common: Constants, Enums, Helpers, Session. No dependencies on the other layers.
- References: UI -> Business/Common; Business -> Data/Common; Data -> Common. No circular dependencies. UI may reference Data only at application composition if required by the chosen architecture.
- Preserve established contracts and avoid duplicate implementations. Coordinate shared database/interface/DTO/architecture changes through the project process. Continue independent work; ask only when missing decisions materially affect correctness.

## Course Requirements
- Demonstrate C#, OOP, interfaces, delegates/events, Windows Forms, LINQ, ADO.NET, Entity Framework, and SQL Server through real, necessary features.
- EF handles ordinary CRUD. LINQ handles queries, filtering, projections, and analysis. ADO.NET handles approved stored procedures, report queries, or complex SQL when it adds clear value.
- Query/data-access implementations remain in Data; Business orchestrates business workflows; UI/Analytics consumes agreed contracts. Do not put direct SQL/ADO.NET in Forms.
- Do not reimplement the same CRUD with both EF and ADO.NET merely to demonstrate technologies. Document where each required technology is used.
- Use interfaces for meaningful boundaries and delegates/events for real notifications, including post-commit stock/UI updates. Do not add unused interfaces/events just to satisfy a checklist.
- All implementations must stay within the active ownership scope and technology authorization policy.

## Inventory Business Rules
- Track stock per product + warehouse, and location when modeled. Never use one global quantity for all warehouses.
- Import/export quantities must be positive; prices nonnegative; money uses decimal. Quantity types follow agreed units.
- Validate referenced products, warehouses, suppliers, and relationships before writing.
- Only the agreed posted/completed state affects stock. Drafts must not change stock when a draft/approval workflow exists.
- Each receipt affects stock exactly once. Protect repeated submissions/double-clicks at Service and database levels as appropriate. The goal is mandatory; a specific duplicate-posting unique index is not locked until all transaction/reference cases are reviewed.
- Atomically save the receipt, lines, stock movement, history, and related alerts when consistency requires it. Roll back the entire operation on failure.
- Validate/decrement exports safely under concurrency; reading then updating without concurrency protection is insufficient. Reject negative stock unless explicitly allowed; use suitable constraints/concurrency/locking.
- Transfer source decrease and destination increase in one transaction; reject identical warehouses and record both sides.
- Adjustments record before/after quantities, difference, reason, actor, and timestamp. Posted receipts use an agreed cancellation/reversal flow; never silently edit/delete them.
- Business Services are the authoritative stock-update path. Never use triggers to automatically increase/decrease Inventory. A trigger that supports `TotalAmount` is optional and requires explicit approval; it must not perform stock posting or bypass Business validation. SQL constraints/indexes/FKs/checks/views/stored procedures support the Service workflow; they must not perform an independent or duplicate stock posting.
- Import flow: Form -> ImportService -> transaction -> update Inventory -> write InventoryTransaction -> commit. Export/adjustment/transfer follow the same Business-controlled principle.
- Do not share DbContext concurrently. Manage context/connection/transaction lifetimes and disposal correctly.
- Publish InventoryChangedEvent/StockAlertEvent only after commit. UI refresh failure must not replay a committed transaction.
- `UserWarehouse` is the approved and required persistent assignment model for STAFF warehouse access under SystemContract.md; do not reintroduce `Users.WarehouseId` or infer STAFF scope from unrelated user/warehouse fields.
- SystemContract.md is the current source of truth for official event contracts, receipt states, cancellation requirements, stable Business errors, and shared transaction boundaries. Treat older SystemContract versions and audit snapshots as historical unless explicitly revalidated.
- Use agreed stock valuation/statistical formulas; do not assume selling price represents inventory value.

## Stock Alert Rules
- CurrentStock (Quantity) is stock for the relevant product/warehouse. ReorderPoint is the reorder-alert threshold. MaximumStock is the intended upper limit.
- Require configured thresholds with 0 < ReorderPoint < MaximumStock. Reject invalid configuration; do not invent missing thresholds. Reject negative quantities rather than assigning a normal status.
- Apply these mutually exclusive rules, including the boundary values:
  - Quantity = 0 -> OUT_OF_STOCK.
  - 0 < Quantity <= ReorderPoint -> LOW_STOCK.
  - ReorderPoint < Quantity <= MaximumStock -> NORMAL.
  - Quantity > MaximumStock -> OVERSTOCK.
- Threshold scope follows the agreed product/warehouse model; never hard-code demo values in Services.
- Prevent duplicate open alerts for the same product/warehouse/type. Resolve/update alerts when stock returns to NORMAL according to the agreed lifecycle; switching alert types must not leave stale active alerts.
- Stock-status calculation belongs to Business, not duplicated in Forms, charts, or database triggers. UI displays the returned status with clear Vietnamese labels.
- Test zero, exactly ReorderPoint, exactly MaximumStock, values immediately below/above both thresholds, and invalid thresholds.

## Analytics Rules
- Dashboard, Analytics, charts, reports, and notification presentation belong to the UI/Analytics scope. Business-owned stock calculation, validation, and updates remain in Business; Data owns database/query implementations.
- Agree input/output contracts before connecting Analytics to Services. Request authorization for cross-owner Service/Data changes rather than implementing them independently.
- Define metrics explicitly through the agreed Analytics/Report contract: warehouse/time scope, units, stock valuation method, Top Products ranking criterion, Dead Stock time window, ABC basis/cutoffs, and reorder formula. Bảo Anh does not unilaterally decide report metrics or business formulas for Khiêm.
- Do not invent formulas or data. Reorder recommendations must explain their criteria and never automatically create/post imports without a separate requirement.
- Reuse approved EF/LINQ/ADO.NET query paths. New chart/export/AI libraries require permission; presentation responsibilities do not authorize new technologies.

## C# & Security
- Use domain types, generics, enums, and DTOs. No dynamic/object to bypass type safety; standard WinForms object event parameters remain valid.
- Use var when types are clear. PascalCase for types/methods/properties; camelCase for locals/parameters; follow existing private-field conventions.
- Use nullable reference types in modern .NET. Address nullability; do not overuse ! or suppress warnings to hide errors.
- Filter/paginate queries when needed; do not load entire tables to find one record or aggregate.
- Parameterize SQL/ADO.NET. Never concatenate user input into SQL.
- Store salted password hashes using an established solution, never plaintext or plain MD5/SHA. Enforce permissions in Services and resolve actors from a trusted authenticated session.
- Never hard-code, log, or commit passwords, tokens, or connection strings with secrets.
- Do not swallow exceptions. Log appropriate diagnostic details, preserve errors for diagnosis, and show understandable user messages.
- Avoid async void except WinForms event handlers. Do not block UI with .Result/.Wait(), wrap already asynchronous EF queries in Task.Run, or access a context concurrently. Update controls on the UI thread using supported APIs.

## WinForms UI/UX

### Vietnamese UI and WinForms implementation
- User-facing labels, notifications, dialogs, alerts, and instructions must use natural, clear Vietnamese.
- Never expose SQL, DbContext, schemas, stack traces, raw exceptions, or internal implementation details to end users. Developer discussions may use technical terms when useful; summaries should remain understandable.
- Keep long operations responsive, show busy state, and prevent repeated Save actions.
- Use logical TabIndex, clear labels, and appropriate Dock/Anchor/TableLayoutPanel layouts.
- Keep business logic and hand-written styling out of generated .Designer.cs/.resx when Designer, code-behind, or themes can handle it. Preserve Form Designer compatibility.
- Unsubscribe disposed Forms from events whose publishers outlive them.

### Colors and annotations
- Palette: primary #228be6; confirm/approve #40c057; cancel/delete/danger #ff6b6b; primary action/download #1c7ed6.
- Color never conveys meaning alone; add labels/icons. Check actual contrast and adjust text colors/shades as needed.
- Every hand-written foreground/background/border color configuration needs an adjacent comment: // [COLOR CONFIG]: description, color, purpose. This includes direct BackColor/ForeColor/custom border assignments.
- Prefer shared theme/helper values. Preserve [COLOR CONFIG] comments when refactoring. Put custom styling/comments outside generated Designer files rather than modifying generated code just for comments.
- Reuse Resources or installed icon libraries. Do not introduce web icon packages into WinForms.

These are project choices informed by current Windows/Fluent guidance, not a formal standard called UI/UX 2026. Apply them in WinForms; do not migrate frameworks or buy controls without a requirement.

### 9.1. Principles and layout
- Prioritize fast data reading, few steps, and error prevention. Modern appearance must serve inventory work.
- Use consistent navigation, titles, action/search areas, content, and necessary status information; one prominent primary action per screen.
- Suggested sections: Overview, Products, Categories, Suppliers, Warehouses, Imports, Exports, Inventory, Alerts, Reports; actual labels are Vietnamese and respect permissions.
- Show current page/warehouse clearly, especially near receipt information. Use operational tables rather than oversized cards and excessive scrolling.
- Use surfaces/subtle borders for hierarchy; limit shadows, gradients, glass, bright colors, and decorative motion. Reuse existing layouts/components without an oversized design system.

### 9.2. Typography, spacing, and sizing
- Use system fonts or Segoe UI with suitable fallback/full Vietnamese support; no decorative fonts for data/forms.
- Suggested 100% baseline: body 10-11 pt, section titles 12-14 pt, page titles 16-20 pt; test and adjust. Points are not pixels. Avoid excessive uppercase/bold text.
- Suggested 96-DPI spacing: 4/8/12/16/24 units; content padding 16-24; typical button height 32-40. Scale with DPI; enlarge touch targets if needed; never force dimensions that clip text.
- Use consistent Vietnamese number/date formatting and explicit units/currency; avoid machine-dependent parsing.

### 9.3. Color and accessibility
- Stock states need text and optionally icons, not just color. Target contrast of 4.5:1 for normal text, 3:1 for large text; distinguish focus and interactive components from backgrounds.
- Bright red/green may not support white text: verify actual pairs and use dark text/adjusted shades.
- Respect SystemInformation.HighContrast using SystemColors/recognizable standard controls. Brand colors must not hide text, focus, or selection.
- Preserve visible focus indicators. Use AccessibleName/AccessibleDescription where labels are insufficient; test essential tasks with Narrator when possible.
- Dark mode is optional and requirement-driven; if implemented, test inputs, tables, menus, dialogs, disabled states, and focus, not only Form backgrounds.

### 9.4. Keyboard and interaction
- Essential flows support logical Tab/Shift+Tab, sensible Enter, and Escape for cancellable interactions.
- Configure AcceptButton/CancelButton by context. Enter while editing a cell must not unexpectedly post/delete receipts.
- Follow/show Windows shortcuts without hijacking standard shortcuts. Icon-only buttons need tooltips/accessibility names; important actions should have text.
- Separate destructive actions from primary actions; no default focus on destructive confirmations.

### 9.5. Tables and search
- Clear DataGridView headers/widths; codes/names left-aligned, quantities/money right-aligned, explicit units.
- Lists are read-only unless inline editing is required with clear validation/save/cancel. Show sort column/direction; do not make row numbers look like identifiers.
- Consistent search/filter placement, Clear filters, and result counts. Add debounce/cancellation only when needed.
- Preserve filters, search, selection, and scroll on return/refresh where practical. Paginate/virtualize when volume requires it; do not fetch large tables to filter on UI or paginate tiny lists unnecessarily.
- Distinguish no data from no matches and offer a relevant next action.

### 9.6. Forms and error prevention
- Visible labels, required indicators, and units; placeholders/tooltips are not label replacements. Use appropriate standard numeric/date/selection controls.
- Field-level ErrorProvider/clear text, not color alone; focus first invalid field. Preserve values/receipt lines after failures.
- Show warehouse, products, quantities, units, relevant totals, receipt state, and available stock where useful.
- Warn before losing unsaved edits, not for every ordinary action. Confirmations name the action, receipt, and consequences instead of ambiguous Yes/No.
- Never present unimplemented controls as working or report success before saving.

### 9.7. System states and feedback
- Provide loading, empty, success, error, and permission states; never leave a blank screen during slow queries.
- Show busy feedback, prevent duplicate actions, remain responsive, and never fabricate progress percentages.
- Success only after commit. If refresh fails afterward, explain saving succeeded and offer refresh, not duplicate save.
- Vietnamese errors suggest recovery without exposing internals/secrets. Prefer lightweight routine-success feedback; reserve blocking dialogs for decisions.
- Important errors/results must not exist only in disappearing notifications; stock alerts remain accessible in their list.

### 9.8. Dashboard and charts
- KPIs show scope/warehouse/time period, units, and update time when useful. Prioritize quantity/value/low-stock/out-of-stock metrics; link to relevant lists when implemented.
- Charts have titles, axes, units, necessary legends; avoid 3D, excessive colors, and misleading scales. Quantitative bars normally start at zero; clearly indicate alternatives.
- Offer exact values/tables when needed. Explain reorder criteria/data; do not label simple formulas as AI.

### 9.9. DPI, performance, and UI acceptance
- Use appropriate Dock/Anchor/TableLayoutPanel rather than fixed coordinates everywhere; keep AutoScaleMode consistent with framework guidance.
- Configure DPI awareness for the target framework; do not blindly copy .NET Framework app.config into modern .NET.
- Test 100/125/150/200% scaling, resize, and mixed-DPI monitors when available; no clipped text, overlapping controls, or inaccessible primary actions.
- Test target window sizes, including 1366x768 laptops if relevant; choose realistic MinimumSize, not an oversized minimum to hide layout problems.
- No expensive queries inside paint/resize loops or unnecessary continuous refresh timers.
- Test mouse/keyboard workflows with long Vietnamese text, large numbers, and error states. Build/check Designer/runtime; report unverified visuals/DPI. Passing build alone does not prove UI quality.

## Database & Dependencies

### Database responsibilities
- EF Core Migration is the authoritative mechanism for EF-managed schema evolution (tables, columns, types, PK/FK, UNIQUE, CHECK, NOT NULL, defaults, indexes, and EF relationships). Existing table/constraint/index SQL scripts must not compete with Migration as a second schema authority.
- Preserve multi-warehouse keys, foreign keys, constraints, indexes, delete behavior, and transaction/concurrency requirements. Use views/stored procedures where justified; database ownership does not justify unnecessary abstractions.
- `UserWarehouse` is approved and is part of the 17-entity model. Its primary key is `(UserId, WarehouseId)`; both foreign keys use `DeleteBehavior.Restrict`. Do not add assignment audit fields without an approved contract change. `Users.WarehouseId` remains forbidden.
- Repository and UnitOfWork are optional: use them only when they add clear value or are established project contracts. Do not build wrappers just to fill folders; EF already provides related capabilities. Do not remove established contracts without authorization.
- Business Services are the authoritative stock-update path. Never use triggers to automatically increase/decrease Inventory. Supporting stored procedures must not independently post receipts, duplicate stock updates, or bypass Business validation.
- EF and ADO.NET participating in one workflow must share the intended transaction/context consistency strategy; do not commit partial steps separately or retain stale tracked quantities.
- Schema changes, SQL scripts, Entity/DbContext mappings, and supporting query contracts remain in the database owner's scope; cross-owner edits require authorization.
- Keep schema changes in the authorized task scope and verify them locally. Migration source and snapshot belong in Git when the task authorizes commits; do not commit/push or apply schema to an unapproved target. Preserve existing authorization for cross-layer work and Git actions instead of asking again.

### Dependencies and cleanup
- Use NuGet through dotnet CLI or Visual Studio, not yarn/npm. Check standard libraries and installed packages before adding explicitly authorized compatible stable dependencies to the correct project. New packages require approval under Section 1; restoring declared approved packages is permitted.
- EF Core SQL Server/SqlClient belong in Data, UI packages in UI, test packages in test projects. Install Tools/Design only for the chosen tooling workflow; Design may also be needed in the startup project.
- Before removing packages, check source, .resx, Designer, configuration, reflection, and transitive dependencies. Missing using directives do not prove a package is unused.
- Do not remove/upgrade unrelated packages or replace UI libraries outside scope. Clean touched code, but preserve business explanations and color annotations.

## Testing
- Build the entire solution after each coherent logic/UI change; recheck after the final fix.
- From the solution directory, run as applicable:
  - dotnet restore when dependencies changed or are not restored.
  - dotnet build --no-restore
  - dotnet test --no-build for built solutions/test projects with tests.
  - dotnet format whitespace --verify-no-changes and dotnet format style --verify-no-changes when supported by the SDK/project conventions.
- Use existing analyzers/.editorconfig. Do not reformat the whole repository or weaken checks just to pass.
- Introduce no compile errors/new warnings. Report pre-existing failures accurately; never claim checks passed without running them.
- Test imports/exports, insufficient stock, multiple warehouses, rollback, duplicates, concurrent exports, adjustments, transfers, alerts, and permissions.
- EF InMemory does not prove SQL Server transaction/constraint/concurrency behavior; use appropriate integration tests.
- After UI changes, verify Designer, launch the app, and test affected interactions on Windows. Report unavailable Windows/SDK/SQL Server/permissions and checks not run.
- Integration tests involving a database must use dedicated test databases. Unit tests should test Business logic independently where practical, including Services, validation, stock/status calculations, alerts, and permissions.
- Example: an export of 50 with available stock of 20 must be rejected without changing stock. Use real SQL Server integration tests separately for persistence/concurrency/transactions.
- Never reset/drop real databases. Follow the agreed schema source of truth for scripts/migrations.

## Git & Verification

### Branch rules
- `develop` is the main shared branch and the base and target of all regular team work.
- Work on a task branch: `feature/<task>` for features, `fix/<issue>` for fixes, or `chore/<task>` for maintenance. Use short names without spaces or Vietnamese diacritics, such as `feature/import-receipt`.
- One branch covers one task within the assigned scope. Branch names do not grant ownership or permission to edit another member's work.
- Do not commit or push directly to `develop`. Push the task branch and open a Pull Request with **base = develop** and **compare = the task branch**.
- Before requesting review, merge the latest `origin/develop` into the task branch, resolve conflicts without discarding teammates' changes, and build/test the result.
- At least one other member reviews and approves the Pull Request before merging into `develop`. Delete the task branch after merging when it is no longer needed.
- Never force-push shared branches or rewrite history during regular work. A separately authorized repository migration is an exception: back up the old remote commit first and use `--force-with-lease` tied to the observed commit when rewriting is necessary. Do not bypass branch protection.
- Agents may commit, push, or open Pull Requests only when the current task or an explicitly authorized workflow permits those actions. Do not ask again for permission already granted for the same action and scope.

### Team commands
Run from the repository root. Start with a clean working tree; commit or preserve existing work before switching branches. Replace the example branch and file names with the actual task.

Start a task:
```powershell
git switch develop
git pull --ff-only origin develop
git switch -c feature/import-receipt
```
If `develop` exists only on the remote, run `git fetch origin` followed by `git switch --track origin/develop` instead of the first command. If a pull cannot fast-forward, inspect the divergence; do not reset or force-push to hide it.

Save, update, verify, and push the task:
```powershell
git add <changed-file-paths>
git commit -m "feat(import): add import receipt"
git fetch origin
git merge origin/develop
dotnet restore
dotnet build --no-restore
git push -u origin feature/import-receipt
```
Resolve and commit any merge conflicts, then rerun verification before pushing. Run existing relevant tests as described in Section 11. Open the Pull Request targeting `develop` only after checks pass.

After the Pull Request is merged, with a clean working tree:
```powershell
git switch develop
git pull --ff-only origin develop
git branch -d feature/import-receipt
```
If Git refuses deletion after a squash merge, leave the local branch until its work is verified on `develop`; do not automatically use `-D`.

### Git and privacy
- Keep `.agent/` in `.gitignore`; never commit its RULE.md or internal agent state.
- Track `.github/copilot-instructions.md` as the shared team instruction file. Do not exclude it in `.gitignore` or `.git/info/exclude`. Remove secrets and personal content before committing it.
- Exclude bin/, obj/, .vs/, secrets, and personal databases according to repository conventions.
- Never overwrite uncommitted work or use `reset --hard`/`clean -fd` to discard it.
- Use Conventional Commits: `feat(...)`, `fix(...)`, `docs(...)`, `refactor(...)`, `test(...)`, or `chore(...)`, followed by a short description.

### Completion
- Inspect relevant code -> apply YAGNI -> make the minimum complete change -> verify -> report.
- Implement requested scope only. Ask only when missing decisions materially affect correctness or scope.
- Report changes, actual check results, the working branch, and commit/push/PR status when applicable. Never claim a Git action or check succeeded without verification.

## File Safety & Destructive Operations

### Workspace boundary

- Before any file modification, resolve and record the absolute repository root.
- Create, edit, move, or delete files only inside that repository.
- Never modify or clean drive roots, Windows directories, installed applications, user profiles, Desktop, Documents, Downloads, or shared folders outside the repository.
- A request to fix, build, install packages, refactor, or clean this project does not authorize modifying unrelated files or system settings.
- If the repository root cannot be verified, perform read-only inspection only.

### Deletion requires specific approval

- Before deleting any existing file or directory, show its exact absolute path, why deletion is necessary, what it contains, and the recovery method.
- Obtain explicit approval for those exact targets. General instructions such as “clean up,” “remove unused code,” or “fix everything” are not deletion approval.
- Do not combine approved deletion with additional targets.
- Prefer a reversible move to an agreed backup location over permanent deletion.
- Preserve original contents before overwriting substantial files. Git does not protect untracked or uncommitted work; preserve those separately.
- Never automatically delete backups.

### Validate every destructive target

- Resolve the final absolute path, including symbolic links, junctions, and reparse points, before acting.
- Verify that the target is strictly inside the repository and is not the repository root, a drive root, or an ancestor directory.
- Reject empty paths, unresolved variables, wildcards, ambiguous relative paths, or links that lead outside the repository.
- Never construct destructive commands from unchecked variables or user-controlled text.
- Recursive deletion requires an enumerated target list and explicit approval.
- Never search an entire drive and delete matching files or folders.

### Forbidden automatic operations

- Do not run disk cleanup, storage optimization, application uninstallers, registry cleanup, disk formatting, partition changes, or broad permission changes.
- Do not delete system folders, package caches, SDKs, runtimes, global NuGet caches, or other projects to fix a build.
- Do not use destructive commands such as `Remove-Item -Recurse -Force`, `rd /s /q`, `rm -rf`, `git clean`, or `git reset --hard` without exact-target approval and a verified recovery plan.
- Do not use administrator privileges or bypass permission errors to perform cleanup.
- Do not hide deletion inside scripts, build hooks, package scripts, or chained commands. Inspect unfamiliar cleanup scripts before running them.
- Standard restore/build commands are permitted when their configuration has been checked for unexpected destructive hooks. Explicit clean commands still require deletion approval.

### If a mistake occurs

- Stop all write, cleanup, installation, and deletion operations immediately.
- Report the exact command, affected paths, and known results.
- Preserve logs and available backups.
- Do not attempt recovery, reinstall software, or write recovered files onto the affected drive without a separately agreed recovery plan.
- Never claim that deleted data is recoverable without verification.

## Staff Permissions
- STAFF assignments are persisted in `UserWarehouse` with composite key `(UserId, WarehouseId)` and Restrict FKs. Active assignment, active user and active warehouse are required; warehouse-scoped access is limited to the validated current warehouse. `Location` is not the authorization source, and selecting a warehouse never grants access by itself.
- Only ADMIN manages STAFF assignments. Deactivation retains the row with `IsActive=false`; reactivation must validate the STAFF role and active account/warehouse. Do not grant STAFF assignments to ADMIN or WAREHOUSE_MANAGER.
- WAREHOUSE_MANAGER scope uses `Warehouse.ManagerId`, referencing an active WAREHOUSE_MANAGER; ADMIN is not restricted to one warehouse. UI menu/filter restrictions do not replace Business checks.

## Confirmed Operational Decisions — 2026-10-14

- Preserve exactly ADMIN, WAREHOUSE_MANAGER, STAFF and the six receipt states DRAFT, PENDING_APPROVAL, APPROVED, REJECTED, COMPLETED, CANCELLED. Only completion posts receipt stock, once, through internal InventoryService posting with the caller's context/transaction.
- SQL deadlock 1205 may retry at most twice (three attempts total) only while no commit succeeded and SQL has rolled back the victim transaction; start a new transaction and clear tracking. Never automatically retry timeout, lost connection or an uncertain commit. Exhausted retries use `BusinessConflictException.Code = CONCURRENCY_RETRY_EXHAUSTED`; duplicate completion must never replay stock.
- Business logs for the approved stock workflows persist in the same transaction. Shared InventoryChanged/StockAlert publishing and compatible ReceiptCompleted notifications occur after commit; isolate observer failures. Abnormal-stock notifications are best-effort after commit to active ADMINs and the active WAREHOUSE_MANAGER responsible for that warehouse. Failure must not replay stock. Do not claim every CRUD action is logged or add an outbox without approval.
- Dashboard defaults to 30 calendar days including today, using InventoryTransaction.CreatedAt for daily IMPORT/EXPORT quantities, without also counting receipt details. Lowest-stock top10 ranks product–warehouse inventory lines by quantity, then ProductCode and WarehouseCode; distribution has four approved stock states; recent movements show20 rows with actor and available reference. STOCK totals are current stock, not historical valuation.
- Report totals and exports use the entire authorized dataset matching the applicable warehouse/date/search/type/status filters, not only the visible page and not a cap5000. STOCK reports have no movement-date filter. Do not assume multi-query snapshot consistency or invent valuation.
- Reuse existing ClosedXML and QuestPDF for exports. The approved per-receipt template is basic A4 with code/date/status/warehouse/creator and product/quantity/unit price/line total. Printing uses the exported PDF; a direct print dialog is not implemented. Include the approved completion/cancel/reject audit; show unknown legacy values as unavailable.
- EF Migrations are schema authority; keep legacy SQL as requested, never run schema scripts over migrations, and never change an applied baseline. App startup does not migrate or seed. Separate interactive Setup creates exactly three roles and only the first active ADMIN, using PBKDF2; it must not overwrite an existing active ADMIN or log credentials.
- Desktop UI and EF CLI use SMART_INVENTORY_CONNECTION_STRING; UI supports the existing local App.config fallback, while Setup requires the environment variable. Confirm the effective target for each tool rather than assuming Visual Studio launchSettings configures EF/Setup CLI. Never commit local credentials.
- The currently authorized SQL TEST target is localhost/SmartInventoryManagerDB; production is separate and unspecified. SQL integration tests are opt-in via SMART_INVENTORY_SQL_TEST_CONNECTION_STRING and SMART_INVENTORY_SQL_TESTS_ALLOW_MUTATION=1, guard that exact target, and clean only their own fixtures. No drop/reset is authorized.
- User-approved TeamChot implementation supersedes earlier deferrals: receipt CompletedBy/CompletedAt, required separate CancelReason/RejectReason, full paged ReceiptRejection history (no detail snapshots or automatic purge), and movement BeforeQuantity/AfterQuantity/SignedDifference. Preserve unknown legacy audit as NULL; save workflow audit atomically and prevent deletion of receipts with rejection history.
- Transfers use a shared ReferenceId from SQL TransferReferenceSequence, distinct movement TransactionIds and TRANSFER_OUT/IN types, with the approved filtered uniqueness constraint. Never allocate MAX+1; gaps are valid.
- Phase-one valuation is explicitly estimated current Quantity × current PurchasePrice, not accounting cost. ABC uses COMPLETED export detail Quantity × UnitPrice per product–warehouse over 12 months through today; sort descending then product/warehouse codes, classify crossing rows in the accumulating group (default A80/B95, configurable), and classify zero total as insufficient value. Dead stock uses positive current stock, no COMPLETED export in 90 calendar days including today, and earliest movement as tracking start; absent/short history is insufficient data. Reorder suggestions use MaximumStock−Quantity when Quantity<=ReorderPoint, never auto-post.
- Hide horizontal/vertical scrollbar chrome while preserving content access, wheel/keyboard scrolling and paging. Details cells must preserve selected-row colors with legible text/icons. Wrapped filters must remain reachable.
- Weighted-average accounting valuation, approved opening balances/transfer/adjustment costs, retention/archive policy and production deployment remain open. Do not invent these rules or treat limited native UI checks as full Designer/DPI/physical-print acceptance.
- Update checklist boxes only with evidence and label TEST versus production. Build/unit/SQL tests, native UI acceptance, Designer/DPI and export/printing checks are distinct; list unverified coverage instead of declaring the entire project complete.

- Confirmed 2026-10-15: receipt lifecycle audit additionally stores latest SubmittedBy/At, ApprovedBy/At, CancelledBy/At and append-only ReceiptStatusChanges for every actual transition, including REJECTED→DRAFT through edit/resubmit, in the workflow transaction. Initial draft creation uses existing creator/date; do not invent old transitions or backfill unknown actors/times. Preserve rejection history; prohibit hard deletion when either history exists. Paging20, no detail snapshots/purge. Keep best-effort notifications and phase-one estimated value; WAC remains phase2. User requested business implementation first and comprehensive testing last; no further data cleanup.

- Temporary scripts, QA harnesses, generated review files and backups belong in repository-root .temp/, never scattered under .agent. Do not commit .temp or local credentials. The sole current progress/acceptance report is Documentation/Architecture/ProjectStatus.md; architectural references, contracts and the DB checklist remain separate purpose-specific documents.
