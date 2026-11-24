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

The implementation will define `IFileStorageService` with `UploadAsync`, `DeleteAsync`, `DownloadAsync`, and `GetUrlAsync` operations. A training implementation will be `LocalFileStorageService`. `GetUrlAsync` is an internal storage abstraction operation, not permission to publish a public file path or unauthenticated URL: any returned location must route through a server handler that rechecks document authorization. No file-path query parameter may bypass authorization.

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

- No unresolved clarification remains. The six approved decisions cover project-authorized direct shares, independent mixed-batch outcomes, same-project task attachment, permanent deletion with bounded audit retention, notification recipients, and fail-closed local scanning.
- Implementation remains pending for a functioning local malware scanner, Department-based Team Lead access, administrator access to all documents, Shared with Me, direct task-detail uploads, date-range filtering, per-file progress, and 100 negative authorization cases. A no-op or test-only scanner does not satisfy the scan requirement.
- Remaining implementation choices stay within the existing stack. The audit-retention duration remains pending compliance-owner approval and is tracked separately from the six resolved clarifications.

## Decision 12: Process batch uploads independently

Each file in a multi-file request will validate and process independently. The result set will include one success or rejection record per file so valid files are not blocked by unrelated invalid files.

**Rationale**
- Matches the approved clarification and provides clear per-file feedback.
- Avoids partial database rollback or misleading whole-batch success.

## Decision 13: Use a provider-neutral malware scan abstraction

The implementation will introduce `IMalwareScanService` with a provider-neutral scan result. The application will call a functioning local scanner through this hook before storage and reject unavailable, failed, errored, threat-detected, or indeterminate results. A no-op hook, fabricated `Clean`, or test fake cannot satisfy the requirement. Test doubles may verify rejection paths only. The current MVP has no functioning malware scanner, so this decision is pending implementation and verification.

**Rationale**
- Keeps the training application offline and does not require a mandatory external scanner.
- Fails closed as required while preserving a migration seam.
- Must not imply any scanning capability until an actual local scanner is implemented and verified; even then, do not claim production-grade protection without evidence.

## Decision 14: Keep deletion permanent and audit bounded

Deletion permanently removes the document record and file after authorization and confirmation. `DocumentActivity` remains as bounded accountability data, contains no deleted file content or full document metadata, and uses a maximum 500-character `Details` value. No audit-retention duration is specified in the stakeholder brief, so a compliance owner must approve a duration before any time-based purge is designed or implemented.

**Rationale**
- Matches the stakeholder out-of-scope soft-delete/trash decision and approved clarification.
- Retains enough evidence for administrator audit reporting.

## Decision 7: Model project teams through existing project membership roles

There is no standalone Team aggregate in the current application. Project membership already contains a `Role` label and a project relationship. Direct team shares will therefore resolve the requested team/role label among `ProjectMember` rows for the document's project.

**Rationale**
- Satisfies the clarified “project-authorized team” rule with the smallest repository-compatible change.
- Avoids introducing a duplicate team hierarchy that would not be used elsewhere.

Team Lead document access is a separate Department-based rule from project-team shares: the authenticated Team Lead may manage documents uploaded by users with the same Department claim. Administrators may manage any document. Neither permission changes the clarified rule that direct-share recipients must already be authorized for the document's project.

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
