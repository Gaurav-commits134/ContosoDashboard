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
**Performance Goals**: Upload under 30 seconds for 25 MB files; document list loads under 2 seconds for 500 documents; search returns in under 2 seconds  
**Constraints**: Offline-capable; no production authentication claims; integer `DocumentId`; text categories; 25 MB maximum; server-side authorization on every resource operation; malware scanning fail closed through a replaceable local abstraction
**Scale/Scope**: Single dashboard application used by a small internal team with project-scoped document access

## Constitution Check

### Initial constitution check

| Principle / gate | Assessment |
|---|---|
| I. Training-First Purpose | Pass. The existing mock authentication is retained and the feature is documented as training-only. |
| II. Security and Authorization by Design | Pass, subject to implementation. Every document read/write/delete/share operation will authorize against the Document, Project, and ProjectMember records on the server. Direct shares will be rejected unless the target user or project team is already authorized for that project. |
| III. Offline-First and Replaceable Infrastructure | Pass. SQLite and local filesystem remain the defaults; `IFileStorageService` provides the migration seam. |
| IV. Simple, Layered Architecture | Pass. Existing Models, Services, Pages, and DbContext boundaries are retained; no new host or broad rewrite. |
| V. Verifiable Quality | Pass. Existing MVP tests are insufficient for the complete specification, so plan includes focused authorization, lifecycle, and integration tests. |

### Gate result

No unresolved clarification remains. The plan may proceed. Any team-sharing implementation must use existing project membership and role labels rather than introducing a disconnected team subsystem unless the implementation needs to persist a named team, in which case the spec and data model must be updated first.

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
- Add unique indexes for active shares and audit-friendly indexes for document activity.
- Permanently delete the `Document` record and physical file after confirmed owner/project-manager deletion. Retain only bounded `DocumentActivity` accountability events with document identity, acting user, action, UTC timestamp, and minimal context.

### 2. Storage and file lifecycle

- Keep `IFileStorageService`/`LocalFileStorageService` as the storage seam.
- Generate a GUID-based stored name before database metadata is saved.
- Validate against the approved extension/MIME pairs from the specification, together with non-zero size and the 25 MB limit.
- Submit every accepted file to a provider-neutral `IMalwareScanService` abstraction before storage. Fail closed: unavailable, failed, errored, or indeterminate scan results reject the file and store neither file bytes nor document/audit metadata.
- The MVP must not claim production-grade malware protection. A local/test implementation may be replaceable through dependency injection, but no upload may succeed without a verified scan outcome.
- Do not accept a path supplied by the caller.
- Process batch uploads independently. Persist each valid file and return a per-file success/failure result; do not roll back unrelated valid files when one file fails.
- Build and save metadata only after file validation; if database save fails, delete the newly written file.
- Write document metadata and the `Upload` audit activity in one EF Core database transaction. If the database operation fails, call storage deletion for the newly generated path; do not return success or retain metadata/audit rows. If deletion itself fails, record the orphan path and failure for cleanup retry and surface the operation failure.
- Replacement writes the replacement first, updates metadata and audit state, then removes the old file.
- Deletion requires owner or project-manager authorization and removes or invalidates both the file and document record consistently.

### 3. Authorization and sharing

- Implement a private/centralized authorization check in `DocumentService` and use it for direct lookup, download, preview, search, update, replace, delete, and share.
- Owner access includes upload owner only.
- Project access includes project members and project manager.
- Direct user shares are additional access only if that user is a member or manager of the document's project.
- Team shares resolve a project role/team label to project members and are rejected when no authorized recipients resolve.
- Personal documents have no project and remain owner-only; direct sharing is not offered because the clarified rule requires a related project.
- Share notifications go to each eligible recipient. Project-document upload notifications go to project members except the uploader.

### 4. Search and retrieval

- Search title, description, tags, uploader display name, project name, category, and task association.
- Compose authorization predicates first, then apply title/category/project/task/uploader filters.
- Sort by upload date, title, size, or category with bounded page size.
- Preview/download requires the same service authorization used for UI display.
- Add no public file URL or URL parameter that bypasses document-ID authorization.

### 5. Dashboard, tasks, reporting

- Add recent documents and document counts to the existing dashboard data/service rather than a separate dashboard subsystem.
- Include the existing optional `TaskId` in document associations. Upload/update accepts it only when the task exists and its project equals the document project. Personal documents may not have task associations.
- Add P3 administrator reporting queries for active uploaders, MIME/category counts, and document access patterns. Reporting output must remain administrator-only.
- Keep audit/reporting independent of the P1 MVP: implement P3 after the core upload, retrieval, sharing, and task/dashboard stories.

### 6. UI

- Extend `Documents.razor` with search, category/project/task filters, preview/download actions, edit/replace, delete confirmation, and share controls.
- Reuse project/task detail context instead of duplicating document state.
- Keep clear upload errors and disable repeated submissions.
- UI visibility is supplemental; all mutations and reads remain protected in services.

## Constitution Re-check After Design

- Training purpose: retained.
- Server-side authorization: planned as a centralized document service policy.
- Offline infrastructure: planned with SQLite and local disk only.
- Small architecture: no new application host or framework.
- Test coverage: planned for upload, direct-ID access, project access, share recipient eligibility, replacement, deletion, and audit records.

## Complexity Tracking

No unjustified constitution violations were identified. The existing partial implementation is useful MVP foundation, but the plan requires additional work for unimplemented search/retrieval, team and user shares, replacement/update, audit/reporting, and task/dashboard integration.
