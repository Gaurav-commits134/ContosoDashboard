# Research: Document Management Decisions

## Decision 1: Store metadata in EF Core and physical files on disk

The application already uses EF Core and a local database. The document feature will follow that same pattern by storing document metadata in the database and the actual file bytes in a secure local directory outside `wwwroot`.

**Rationale**
- Keeps the solution offline and simple for training.
- Aligns with the repository’s existing architecture and data model conventions.
- Preserves a future migration path to Azure Blob Storage by abstracting file operations behind a service.

**Trade-offs**
- Local disk storage is not horizontally scalable, but it matches the project’s training and offline requirements.
- File cleanup must be handled manually if metadata is deleted without storage cleanup.

## Decision 2: Introduce a `Document` domain model with project-aware visibility

The new `Document` model will include a required title, category, associated project, uploader, upload date, file size, MIME type, file path, and tags. This model will sit alongside the current `Project`, `TaskItem`, and `User` models.

**Rationale**
- Helps maintain the current domain consistency without introducing a new architectural ecosystem.
- Lets the app filter documents by project membership and ownership across list, search, and task views.

## Decision 3: Use an abstraction for storage, not direct file IO in business logic

The implementation will define `IFileStorageService` with methods for upload, delete, and authorized download/preview stream access. A training implementation will be `LocalFileStorageService`. It will not expose public URL generation or file-path query parameters.

**Rationale**
- Fulfills the migration-ready design requirement from the stakeholder brief.
- Prevents the business layer from hard-coding storage technology.

## Decision 4: Validate permission in service methods, not only in the UI

Authorization checks will be enforced in the document service before returning or mutating a document.

**Rationale**
- Prevents insecure direct object reference exposure.
- Matches the repository’s existing service-layer security patterns.

## Decision 5: Add general-purpose document search with project and ownership filtering

Search will operate on uploaded document metadata fields rather than by scanning binary file content. The same pattern will be used for filters and sorting.

**Rationale**
- Simpler and faster within the current architecture.
- Keeps the feature aligned with the current project size and training scope.

## Decision 6: Keep category values as text for simplicity

Document categories will be stored as strings rather than enums to keep the schema simpler and easier to extend.

**Rationale**
- Predefined categories are already specified by the stakeholders.
- It reduces implementation friction and is consistent with the requirement for text-based categories.

## Open Question tracking

- No unresolved specification question remains.
- The remaining implementation choices are bounded to the existing stack and are resolved below.

## Decision 7: Model project teams through existing project membership roles

There is no standalone Team aggregate in the current application. Project membership already contains a `Role` label and a project relationship. Direct team shares will therefore resolve the requested team/role label among `ProjectMember` rows for the document's project.

**Rationale**
- Satisfies the clarified “project-authorized team” rule with the smallest repository-compatible change.
- Avoids introducing a duplicate team hierarchy that would not be used elsewhere.

**Alternatives considered**
- A new Team and TeamMember entity: more complete, but broader and unsupported by the current specification and UI.
- A direct share to every project member: useful but not expressive enough to distinguish a named team.

## Decision 8: Use SQLite for local planning assumptions

The repository is configured for SQLite; LocalDB was not retained in the actual implementation.

**Rationale**
- Matches `Program.cs`, `appsettings`, and the offline Linux environment.
- Existing EF Core data contracts remain portable.

## Decision 9: Record document actions in a dedicated audit entity

Uploads, downloads, updates, replacements, deletes, and share events will be represented by `DocumentActivity`, rather than only log output.

**Rationale**
- Supports FR-015 and administrator access reporting.
- Keeps audit behavior queryable and associated with the document and acting user.

## Decision 10: Validate types and cleanup transactions explicitly

The service will use the approved extension/MIME pairs defined in the specification. Because MIME can be spoofed by a browser, no upload can rely on MIME alone or accept mismatched pair data. Storage is local and outside web roots.

**Rationale**
- Meets 25 MB and unsupported-type validation while reducing spoofing risk.
- Preventing orphaned files or database rows requires compensating cleanup on storage/database failures.

## Decision 11: Keep team shares and all direct identifiers behind service authorization

No Razor or HTTP UI state will determine whether access is permitted. The document service will recalculate project membership and share eligibility for every operation.
