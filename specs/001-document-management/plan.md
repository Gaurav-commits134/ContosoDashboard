# Implementation Plan: Document Management

**Branch**: `001-document-management` | **Date**: 2026-11-23 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-document-management/spec.md`

## Summary

Complete the secure, project-aware document-management feature in the existing Blazor Server + EF Core application. The implementation plan uses the repository's current models and services, adds the missing retrieval, sharing, replacement, audit, reporting, and task/dashboard capabilities, and preserves the clarified rule that direct shares may target only users or teams that already belong to the related project.

## Technical Context

**Language/Version**: C# / .NET 8.0  
**Primary Dependencies**: ASP.NET Core 8, Blazor Server, Entity Framework Core 8, Bootstrap 5, SQLite
**Storage**: Local file system under `AppContext.BaseDirectory/AppData/uploads` with EF Core metadata persistence
**Testing**: xUnit, EF Core SQLite/in-memory integration tests, and Blazor component tests where practical
**Target Platform**: Linux development environment; Blazor Server app deployed locally with offline support  
**Project Type**: Web application  
**Performance Goals**: Average authorized-document retrieval under 30 seconds; upload up to 25 MB within 30 seconds on a typical network; list and search within 2 seconds for up to 500 documents; PDF/image preview within 3 seconds; no more than three clicks for upload; zero confirmed document-access security incidents during the first three months after launch
**Constraints**: Offline-capable; no production authentication claims; integer `DocumentId`; text categories; 25 MB maximum; server-side authorization on every resource operation; Team Lead access scoped by the authenticated Department claim; administrator access to all documents; malware scanning fail closed through a functioning, replaceable local scanner
**Scale/Scope**: Single dashboard application used by a small internal team with project-scoped document access

## Constitution Check

### Initial constitution check

| Principle / gate | Assessment |
|---|---|
| I. Training-First Purpose | Pass. The existing mock authentication is retained and the feature is documented as training-only. |
| II. Security and Authorization by Design | Pending implementation and tests. Every document read/write/delete/share operation will be authorized on the server. Team Leads may manage same-Department documents; administrators may access and manage all documents. Direct shares remain limited to recipients already authorized for the related project; these elevated roles do not bypass that rule. |
| III. Offline-First and Replaceable Infrastructure | Conditional pass. SQLite and local filesystem remain the defaults. Uploads require a functioning local scanner through the replaceable `IMalwareScanService`; no-op or test-only hooks do not satisfy the requirement, and unavailable/failed scans block storage. |
| IV. Simple, Layered Architecture | Pass. Existing Models, Services, Pages, and DbContext boundaries are retained; no new host or broad rewrite. |
| V. Verifiable Quality | Pending. Existing MVP tests are insufficient. The plan requires role-matrix tests, at least 100 negative authorization cases, real-scanner verification, and the specified lifecycle/integration tests before release. |

### Gate result

All six approved clarifications remain resolved. Implementation is not release-ready: the current MVP has no functioning malware scanner, and the new role and feature requirements remain pending. A no-op or test fake may be used to test failure outcomes but cannot pass the scanner acceptance gate. Team Lead scope is based on the authenticated user's Department claim; project-team shares continue to resolve only through authorized `ProjectMember` rows.

## Project Structure

### Documentation (this feature)

```text
specs/001-document-management/
├── spec.md              # Product requirements
├── plan.md              # This file
├── research.md          # Decisions and rationale
├── data-model.md        # Entity contracts
├── quickstart.md        # Validation steps
├── contracts/           # Integration notes and examples
├── tasks.md             # Execution plan
└── checklists/
    └── requirements.md
```

### Source Code (repository root)

```text
ContosoDashboard/
├── Data/
│   └── ApplicationDbContext.cs
├── Models/
│   ├── User.cs
│   ├── Project.cs
│   ├── TaskItem.cs
│   ├── Document.cs
│   ├── DocumentShare.cs
│   ├── DocumentActivity.cs
│   ├── Notification.cs
│   └── ...
├── Services/
│   ├── UserService.cs
│   ├── ProjectService.cs
│   ├── TaskService.cs
│   ├── NotificationService.cs
│   ├── DashboardService.cs
│   ├── IDocumentService.cs
│   ├── DocumentService.cs
│   ├── FileStorageService.cs
│   └── DocumentReportingService.cs (if reporting remains in the service layer)
├── Pages/
│   ├── Index.razor
│   ├── Projects.razor
│   ├── ProjectDetails.razor
│   ├── Tasks.razor
│   ├── Documents.razor
│   ├── DocumentUpload.razor
│   └── Shared components
├── Shared/
│   └── NavMenu.razor
├── Program.cs
└── appsettings*.json
```

**Structure Decision**: Single Blazor Server application with the document domain layered into the existing Models, Services, and Pages structure. The plan adds no separate API host. UI actions call authorized application services, and file retrieval is exposed only through an authorized server endpoint or authorized page/download handler.

## Implementation Design

### 1. Data and persistence

- Keep `Document.DocumentId` as an integer key.
- Preserve text values for `Document.Category` and validate against the six approved categories.
- Extend `DocumentShare` for project-team shares without adding an unrelated team model: store exactly one nullable recipient, either `UserId` or `TeamRole`. A user recipient MUST be the project manager or a current project member of the document's project. A team role MUST be resolved case-insensitively against `ProjectMember.Role`; it may match the project manager only when the requested role is explicitly `ProjectManager`. A team share may be created only when at least one eligible project recipient resolves. Personal-document shares are not offered.
- Add `DocumentActivity` for upload, download, delete, share, metadata update, and file replacement events.
- Add read-only reporting projections for uploader, category/MIME, and access-pattern summaries without introducing a persistent report table.
- Add unique indexes for active shares and audit-friendly indexes for document activity in one mapping task; add a separate test task for the mappings rather than a second index-creation task.
- Permanently delete the `Document` record and physical file after confirmed authorized deletion. Do not persist or rely on `Document.IsDeleted`; retain only bounded `DocumentActivity` accountability events with document identity, acting user, action, UTC timestamp, and minimal context.
- Cap `DocumentActivity.Details` at 500 characters and do not retain file contents or full document metadata in deletion events. The stakeholder has not specified a retention duration; obtain approval before any time-based purge.

### 2. Storage and file lifecycle

- Keep `IFileStorageService`/`LocalFileStorageService` as the storage seam, including `UploadAsync`, `DeleteAsync`, `DownloadAsync`, and `GetUrlAsync`.
- `GetUrlAsync` must not expose a public file path or unauthenticated URL. Any returned location is only usable through a protected server handler that rechecks document authorization.
- Generate a GUID-based stored name before database metadata is saved.
- Validate against the approved extension/MIME pairs from the specification, together with non-zero size and the 25 MB limit.
- Submit every accepted file to a provider-neutral `IMalwareScanService` abstraction backed by a functioning local scanner before storage. Fail closed: unavailable, failed, errored, or indeterminate scan results reject the file and store neither file bytes nor document/audit metadata.
- A no-op or test-only fake does not satisfy malware scanning. Test doubles may exercise failure cases, but acceptance requires verification that the configured local scanner performs a real scan and returns a trustworthy result. The current MVP does not implement this capability and must remain marked pending until verified.
- Do not accept a path supplied by the caller.
- Process batch uploads independently. Persist each valid file and return a per-file success/failure result; do not roll back unrelated valid files when one file fails.
- Build and save metadata only after file validation; if database save fails, delete the newly written file.
- Write document metadata and the `Upload` audit activity in one EF Core database transaction. If the database operation fails, call storage deletion for the newly generated path; do not return success or retain metadata/audit rows. If deletion itself fails, record the orphan path and failure for cleanup retry and surface the operation failure.
- Replacement writes the replacement first, updates metadata and audit state, then removes the old file.
- Deletion requires owner, same-Department Team Lead, project-manager, or administrator authorization as specified and permanently removes both the file and document record consistently.

### 3. Authorization and sharing

- Implement a private/centralized server-side authorization check in `DocumentService` and use it for direct lookup, list/search, download, preview, update, replace, delete, and share.
- Owner access includes upload owner only.
- Project access includes project members and project manager.
- A Team Lead may view and manage documents uploaded by users whose authenticated Department claim matches the Team Lead's claim. Never trust a client-supplied role or department. This scope does not itself make the Team Lead a project member or an eligible task assignee.
- An administrator may view, preview, download, update, replace, and delete any document, with mutations audited. Administrator access to a document does not broaden the permitted recipients of a direct share.
- Direct user shares are additional access only if that user is a member or manager of the document's project.
- Team shares resolve a project role/team label to project members and are rejected when no authorized recipients resolve.
- Personal documents have no project and remain owner-only; direct sharing is not offered because the clarified rule requires a related project.
- Shared with Me queries and views return only explicit shares whose recipients remain authorized for the project.
- Share notifications go to each eligible recipient. Project-document upload notifications go to project members except the uploader.

### 4. Search and retrieval

- Search title, description, tags, uploader display name, project name, category, and task association. Filtering includes a validated date range.
- Compose authorization predicates first, then apply title/category/project/task/uploader/date-range filters.
- Sort by upload date, title, size, or category with bounded page size.
- Preview/download requires the same service authorization used for UI display.
- Add no public file URL or URL parameter that bypasses document-ID authorization.

### 5. Dashboard, tasks, reporting

- Add a Recent Documents widget showing the current user's five most recently uploaded documents, plus a count of documents the user is authorized to access, to the existing dashboard data/service rather than a separate dashboard subsystem.
- Allow direct upload from task detail pages for project members and project managers. Include the existing optional `TaskId` in document associations. Upload/update accepts it only when the task exists and its project equals the document project. Personal documents may not have task associations.
- Add P3 administrator reporting queries for active uploaders, MIME/category counts, and document access patterns. Reporting output must remain administrator-only.
- Keep audit/reporting independent of the P1 MVP: implement P3 after the core upload, retrieval, sharing, and task/dashboard stories.

### 6. UI

- Extend `Documents.razor` with search, category/project/date-range/task filters, preview/download actions, edit/replace, delete confirmation, share controls, and Shared with Me.
- Reuse project/task detail context instead of duplicating document state; provide the direct task-page upload flow in task context.
- Show per-file upload progress and clear success/failure outcomes, and disable repeated submissions.
- Keep the stakeholder target of no more than three clicks from upload entry through submission.
- UI visibility is supplemental; all mutations and reads remain protected in services.

## Constitution Re-check After Design

- Training purpose: retained.
- Server-side authorization: planned as a centralized document service policy for owners, project members/managers, same-Department Team Leads, and administrators; direct shares remain project-authorized.
- Offline infrastructure: planned with SQLite, local disk, and a functioning local malware scanner. The scanner is a release gate and is currently unimplemented.
- Small architecture: no new application host or framework.
- Test coverage: planned for upload progress and outcomes, date-range filtering, Shared with Me, task-page uploads, each role's access, GetUrlAsync non-bypass behavior, at least 100 negative authorization cases, scanner operation/failure, replacement, deletion, and audit records. Audit-retention cleanup remains blocked pending an approved retention duration.

## Complexity Tracking

No unjustified architecture expansion is planned. The existing partial implementation is only a foundation: the current MVP does not satisfy the functioning malware-scanner requirement, and the new Team Lead/administrator access, Shared with Me, date-range filtering, per-file progress, direct task-page upload, and 100-case authorization gates remain pending. Do not report those capabilities as complete before their implementation tasks and acceptance checks pass.
