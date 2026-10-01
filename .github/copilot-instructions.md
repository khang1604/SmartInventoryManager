# SMART INVENTORY MANAGER - AGENT RULES

RULE.md and copilot-instructions.md must contain identical English instructions. RULE.vi.md is the Vietnamese reference for reading and editing, not an additional instruction source. Update both English files and the corresponding Vietnamese section when changing rules. These project instructions do not override platform policies or agent permissions.

## 1. Project Scope & Technology

- Build a multi-warehouse desktop application with C#, Windows Forms, Visual Studio, SQL Server, Entity Framework, ADO.NET, and LINQ.
- Inspect the solution, relevant code, .csproj, global.json, Directory.Build.props, and Directory.Packages.props when present before editing. Repository configuration determines actual versions.
- New solution default: Visual Studio 2026, .NET 10, EF Core 10.x. Preserve existing .NET 8/EF Core 8.x or .NET Framework/EF6 configurations. Do not mix EF6 and EF Core or migrate frameworks without a request.
- Keep Microsoft.EntityFrameworkCore.* packages at the same exact compatible stable version; do not automatically upgrade major versions.
- Do not introduce web stacks, web APIs, or a different UI framework without a requirement.

### Technology authorization
- Approved core technologies: C#, the repository's current .NET target, Windows Forms, SQL Server, Entity Framework, LINQ, and ADO.NET. Reading documentation does not authorize adopting a new technology.
- Follow the repository's pinned versions and tools. Do not independently change framework, database, UI toolkit, architecture, package manager, or major dependency versions.
- A new library, service, hosted platform, alternative ORM, commercial UI toolkit, chart/export package, test framework, or AI integration requires explicit user/team authorization before installation or implementation. Do not introduce it indirectly through a helper or transitive integration.
- Reusing an existing approved dependency and restoring already declared packages are permitted. Installing a missing, explicitly approved core package is permitted only within the active task scope and compatible pinned version policy.
- If approval is missing, explain why the proposed addition is needed, existing alternatives, affected layers, licensing/cost where relevant, and wait before dependent work. Continue independent work using the approved stack.
- Course requirements are not permission to replace the stack. Do not add a library just to make the project look more modern.

### Seven-step YAGNI workflow
Before coding, follow this order and stop at the first sufficient solution:
1. Needed for the current requirement? If not, skip it.
2. Already in the codebase? Reuse or extend it.
3. Solved by the .NET standard library? Use it.
4. Supported natively by WinForms, LINQ, EF, or SQL Server? Use it in the correct layer.
5. Solved by an installed dependency? Reuse that dependency.
6. Solved by one readable statement? Keep it short without sacrificing clarity or error handling.
7. Only then write the minimum necessary custom code.
- Do not add speculative microservices, CQRS, event buses, generic frameworks, abstractions, empty files, or fake functionality.

## 2. Team Ownership

### Ownership map
- **Bảo Anh — Database + Architecture:** database, ERD, Entity models, EF DbContext, repositories/UnitOfWork when justified, architecture, SQL scripts, constraints, indexes, views, and supporting stored procedures.
- **Khang — Backend + Warehouse Operations:** Business Services, warehouse operations, imports, exports, inventory, stock-alert backend, business validation, receipt workflows, stock updates, and Business tests.
- **Khiêm — UI + Dashboard + Analytics:** Windows Forms UI, Dashboard, Analytics, charts, reports, notification UI, UI interactions, and related presentation/query logic within agreed contracts.

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

## 3. Architecture

- UI: Forms, Controls, Components, Resources. Display data and call Services. No SQL, DbContext queries, or inventory business calculations in Forms.
- Business: Services, Interfaces, DTOs, Validators, Events. Enforce permissions, validate input, and execute business workflows.
- Data: Entities, DbContext, Configurations, and justified Repositories/UnitOfWork. Query and persist data; abstractions are optional, not mandatory.
- Common: Constants, Enums, Helpers, Session. No dependencies on the other layers.
- References: UI -> Business/Common; Business -> Data/Common; Data -> Common. No circular dependencies. UI may reference Data only at application composition if required by the chosen architecture.
- Preserve established contracts and avoid duplicate implementations. Coordinate shared database/interface/DTO/architecture changes through the project process. Continue independent work; ask only when missing decisions materially affect correctness.

## 4. Course Requirements

- Demonstrate C#, OOP, interfaces, delegates/events, Windows Forms, LINQ, ADO.NET, Entity Framework, and SQL Server through real, necessary features.
- EF handles ordinary CRUD. LINQ handles queries, filtering, projections, and analysis. ADO.NET handles approved stored procedures, report queries, or complex SQL when it adds clear value.
- Query/data-access implementations remain in Data; Business orchestrates business workflows; UI/Analytics consumes agreed contracts. Do not put direct SQL/ADO.NET in Forms.
- Do not reimplement the same CRUD with both EF and ADO.NET merely to demonstrate technologies. Document where each required technology is used.
- Use interfaces for meaningful boundaries and delegates/events for real notifications, including post-commit stock/UI updates. Do not add unused interfaces/events just to satisfy a checklist.
- All implementations must stay within the active ownership scope and technology authorization policy.

## 5. Inventory Business Rules

- Track stock per product + warehouse, and location when modeled. Never use one global quantity for all warehouses.
- Import/export quantities must be positive; prices nonnegative; money uses decimal. Quantity types follow agreed units.
- Validate referenced products, warehouses, suppliers, and relationships before writing.
- Only the agreed posted/completed state affects stock. Drafts must not change stock when a draft/approval workflow exists.
- Each receipt affects stock exactly once. Protect repeated submissions/double-clicks at Service and database levels as appropriate.
- Atomically save the receipt, lines, stock movement, history, and related alerts when consistency requires it. Roll back the entire operation on failure.
- Validate/decrement exports safely under concurrency; reading then updating without concurrency protection is insufficient. Reject negative stock unless explicitly allowed; use suitable constraints/concurrency/locking.
- Transfer source decrease and destination increase in one transaction; reject identical warehouses and record both sides.
- Adjustments record before/after quantities, difference, reason, actor, and timestamp. Posted receipts use an agreed cancellation/reversal flow; never silently edit/delete them.
- Business Services are the authoritative stock-update path. Never use triggers to automatically increase/decrease Inventory. SQL constraints/indexes/FKs/checks/views/stored procedures support the Service workflow; they must not perform an independent or duplicate stock posting.
- Import flow: Form -> ImportService -> transaction -> update Inventory -> write InventoryTransaction -> commit. Export/adjustment/transfer follow the same Business-controlled principle.
- Do not share DbContext concurrently. Manage context/connection/transaction lifetimes and disposal correctly.
- Publish InventoryChangedEvent/StockAlertEvent only after commit. UI refresh failure must not replay a committed transaction.
- Use agreed stock valuation/statistical formulas; do not assume selling price represents inventory value.

## 6. Stock Alert Rules

- CurrentStock (Quantity) is stock for the relevant product/warehouse. ReorderPoint is the reorder-alert threshold. MaximumStock is the intended upper limit.
- Require configured thresholds with 0 <= ReorderPoint < MaximumStock. Reject invalid configuration; do not invent missing thresholds. Reject negative quantities rather than assigning a normal status.
- Apply these mutually exclusive rules, including the boundary values:
  - Quantity = 0 -> OUT_OF_STOCK.
  - 0 < Quantity <= ReorderPoint -> LOW_STOCK.
  - ReorderPoint < Quantity <= MaximumStock -> NORMAL.
  - Quantity > MaximumStock -> OVERSTOCK.
- Threshold scope follows the agreed product/warehouse model; never hard-code demo values in Services.
- Prevent duplicate open alerts for the same product/warehouse/type. Resolve/update alerts when stock returns to NORMAL according to the agreed lifecycle; switching alert types must not leave stale active alerts.
- Stock-status calculation belongs to Business, not duplicated in Forms, charts, or database triggers. UI displays the returned status with clear Vietnamese labels.
- Test zero, exactly ReorderPoint, exactly MaximumStock, values immediately below/above both thresholds, and invalid thresholds.

## 7. Analytics Rules

- Dashboard, Analytics, charts, reports, and notification presentation belong to the UI/Analytics scope. Business-owned stock calculation, validation, and updates remain in Business; Data owns database/query implementations.
- Agree input/output contracts before connecting Analytics to Services. Request authorization for cross-owner Service/Data changes rather than implementing them independently.
- Define metrics explicitly: warehouse/time scope, units, stock valuation method, Top Products ranking criterion, Dead Stock time window, ABC basis/cutoffs, and reorder formula.
- Do not invent formulas or data. Reorder recommendations must explain their criteria and never automatically create/post imports without a separate requirement.
- Reuse approved EF/LINQ/ADO.NET query paths. New chart/export/AI libraries require permission; presentation responsibilities do not authorize new technologies.

## 8. C# & Security

- Use domain types, generics, enums, and DTOs. No dynamic/object to bypass type safety; standard WinForms object event parameters remain valid.
- Use var when types are clear. PascalCase for types/methods/properties; camelCase for locals/parameters; follow existing private-field conventions.
- Use nullable reference types in modern .NET. Address nullability; do not overuse ! or suppress warnings to hide errors.
- Filter/paginate queries when needed; do not load entire tables to find one record or aggregate.
- Parameterize SQL/ADO.NET. Never concatenate user input into SQL.
- Store salted password hashes using an established solution, never plaintext or plain MD5/SHA. Enforce permissions in Services and resolve actors from a trusted authenticated session.
- Never hard-code, log, or commit passwords, tokens, or connection strings with secrets.
- Do not swallow exceptions. Log appropriate diagnostic details, preserve errors for diagnosis, and show understandable user messages.
- Avoid async void except WinForms event handlers. Do not block UI with .Result/.Wait(), wrap already asynchronous EF queries in Task.Run, or access a context concurrently. Update controls on the UI thread using supported APIs.

## 9. WinForms UI/UX

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

References:
- https://learn.microsoft.com/en-us/windows/apps/design/guidelines-overview
- https://learn.microsoft.com/en-us/windows/apps/develop/accessibility
- https://learn.microsoft.com/en-us/windows/apps/design/signature-experiences/typography
- https://learn.microsoft.com/en-us/dotnet/desktop/winforms/forms/autoscale

## 10. Database & Dependencies

### Database responsibilities
- Preserve multi-warehouse keys, foreign keys, constraints, indexes, and transaction/concurrency requirements. Use views/stored procedures where justified; database ownership does not justify unnecessary abstractions.
- Repository and UnitOfWork are optional: use them only when they add clear value or are established project contracts. Do not build wrappers just to fill folders; EF already provides related capabilities. Do not remove established contracts without authorization.
- Business Services are the authoritative stock-update path. No triggers may automatically increase/decrease Inventory. Supporting stored procedures must not independently post receipts, duplicate stock updates, or bypass Business validation.
- EF and ADO.NET participating in one workflow must share the intended transaction/context consistency strategy; do not commit partial steps separately or retain stale tracked quantities.
- Schema changes, SQL scripts, Entity/DbContext mappings, and supporting query contracts remain in the database owner's scope; cross-owner edits require authorization.

### Dependencies and cleanup
- Use NuGet through dotnet CLI or Visual Studio, not yarn/npm. Check standard libraries and installed packages before adding explicitly authorized compatible stable dependencies to the correct project. New packages require approval under Section 1; restoring declared approved packages is permitted.
- EF Core SQL Server/SqlClient belong in Data, UI packages in UI, test packages in test projects. Install Tools/Design only for the chosen tooling workflow; Design may also be needed in the startup project.
- Before removing packages, check source, .resx, Designer, configuration, reflection, and transitive dependencies. Missing using directives do not prove a package is unused.
- Do not remove/upgrade unrelated packages or replace UI libraries outside scope. Clean touched code, but preserve business explanations and color annotations.

## 11. Testing

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

## 12. Git & Verification

### Git and privacy
- Keep .agent/ in .gitignore; never commit its RULE.md or internal agent state.
- For personal Copilot instructions, add /.github/copilot-instructions.md to .git/info/exclude. Share that file only when intended and after removing private content.
- Exclude bin/, obj/, .vs/, secrets, and personal databases according to repository conventions.
- Do not overwrite uncommitted work, reset --hard, force push, or change shared architecture outside scope.
- Follow repository branch/integration conventions. Prefer feat(...), fix(...), refactor(...), chore(...) commits; preserve established alternatives until a change is agreed.
- Do not commit/push without a request or an authorized established workflow.

### Build, formatting, and completion
- Inspect relevant code -> apply YAGNI -> make the minimum complete change -> verify -> report.
- Implement requested scope only. Do not ask again about small reversible decisions supported by code/requirements; ask when missing business rules materially affect data/results.
- Report changes, executed checks/results, and remaining limitations briefly. Instruction files guide behavior; they do not replace authorization, tests, or tool limitations.
