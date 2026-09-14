# Quickstart: Document Management Validation

## Goal

Verify the new feature works in the existing ContosoDashboard application without introducing a separate deployment host or cloud dependency.

## Prerequisites

- .NET 8 SDK installed
- Local SQL Server LocalDB available to the app
- App running locally through `dotnet run` from the project root

## Validation Steps

1. Launch the application and log in as a known user.
2. Navigate to the Documents page or project detail view.
3. Upload a valid PDF or Office document with a title and category.
4. Confirm the document appears in the user’s document list.
5. Open a project page and verify the document is visible to authorized project members.
6. Search by title or tag to confirm the document appears only when you have permission.
7. Share a document with another user and confirm an in-app notification is generated.
8. Delete a shared or owned document and verify the document is no longer accessible.

## Success Signals

- File upload succeeds while preserving metadata.
- Validation prevents oversized or invalid file types.
- Unauthorized access attempts fail even when a direct document ID is known.
- UI and service layers remain aligned with existing role permissions.
