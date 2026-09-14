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

The implementation will define `IFileStorageService` with methods for upload, delete, download, and URL generation. A training implementation will be `LocalFileStorageService`.

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

- No blocking ambiguities remain for the feature scope.
- Security expectations are sufficiently specified for the offline MVP.
- Remaining implementation details will be captured during the design and coding tasks.
