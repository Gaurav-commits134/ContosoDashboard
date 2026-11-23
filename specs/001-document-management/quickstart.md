# Quickstart: Document Management Validation

## Goal

Verify the new feature works in the existing ContosoDashboard application without introducing a separate deployment host or cloud dependency.

## Prerequisites

- .NET 8 SDK installed
- SQLite connection configured in `ContosoDashboard/appsettings.json`
- App run through `dotnet run --project ContosoDashboard`
- Test packages restored from the solution

## Validation Steps

1. From the repository root, run `dotnet test ContosoDashboard.sln` and confirm the existing and document tests pass.
2. Run `dotnet run --project ContosoDashboard/ContosoDashboard.csproj`.
3. Open the local HTTPS URL and log in as a seeded user.
4. Navigate to `/documents`.
5. Upload a valid PDF, Office, text, or image file with a required title and one of the six categories.
6. Verify oversize, zero-byte, missing-extension, unsupported-type, and missing-metadata submissions are rejected without a document record or orphan file.
7. Simulate a database-save failure after a successful file write and verify the generated file is deleted (or recorded for cleanup retry), no Document/audit row remains, and no success is reported.
8. Associate an upload with task 1 or 2 only when project 1 is selected; reject task/project mismatches and personal-task associations.
9. Associate a valid project document with project 1 and confirm users 2, 3, and 4 can retrieve it but an unrelated user cannot.
10. Request a project document by a known DocumentId while unauthorized; the service must return no document and no download stream.
11. As an owner or project manager, share with a project user or matching project role. Confirm an in-app notification is generated.
12. Reject a direct share to a user or team not associated with the document's project.
13. Search and filter by title, description, tags, uploader, category, project, and task. Confirm unauthorized records are absent.
14. Preview a PDF/image and download an authorized Office/text file.
15. Edit metadata, replace the file, and delete the document; confirm audit activity and that old file data is removed or unreachable.
16. Open the dashboard/task context and confirm recent/associated documents respect the same authorization.

## Success Signals

- File upload succeeds while preserving metadata.
- Validation prevents oversized or invalid file types.
- Unauthorized access attempts fail even when a direct document ID is known.
- UI and service layers remain aligned with existing role permissions.
- Direct share recipients are project-authorized.
- Database metadata and local file cleanup remain consistent after success and failure.
