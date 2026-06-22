# Student Portal

Student Portal is an ASP.NET Core MVC web app where students can share class content (notes, links, images, and PDFs) into classroom spaces, and admins moderate submissions before they become visible.

## What this project does

- Shows a list of classrooms grouped by subject/section.
- Lets students submit content in a room.
- Supports optional file uploads (JPG, PNG, WEBP, GIF, PDF) via Cloudinary.
- Queues all submissions for admin review.
- Allows admins to approve/reject submissions and manage rooms.
- Serves secure file open/download actions for approved content.

## Tech stack

- **Backend/UI:** ASP.NET Core MVC (.NET 10)
- **Database:** PostgreSQL (configured for Supabase)
- **ORM:** Entity Framework Core + Npgsql provider
- **File storage:** Cloudinary API
- **Auth:** Cookie authentication for admin access
- **Optional mobile shell:** Capacitor Android project (`/Android`)

## Repository structure

- `/Controllers` - Room browsing/submission, admin moderation, auth, and file delivery endpoints.
- `/Models` - Domain entities and enums (`ClassRoom`, `ContentItem`, `ContentFile`, `ContentType`, `ContentStatus`).
- `/Models/ViewModels` - Form/view input models with validation.
- `/Data` - EF Core DbContext and SQL bootstrap schema (`SupabaseSchema.sql`).
- `/Services` - Cloudinary integration and options classes.
- `/Views` - Razor views for rooms, admin pages, login, and shared layout.
- `/wwwroot` - Static assets (CSS, JS, libraries).
- `/Android` - Capacitor Android wrapper targeting a hosted Student Portal URL.

## Core flows

1. **Student submission**
   - Student opens a room and submits metadata + optional file.
   - Submission is stored as `Pending`.
2. **Admin moderation**
   - Admin logs in and reviews pending submissions.
   - Admin can approve (becomes visible in room) or reject (and delete uploaded files).
3. **Content access**
   - Approved items appear in room details.
   - Images/videos open directly.
   - PDFs are proxied for inline viewing and downloadable through `FilesController`.

## Prerequisites

- .NET 10 SDK
- PostgreSQL database (or Supabase Postgres)
- Cloudinary account/API credentials

## Required configuration

The app validates these settings at startup:

- `ConnectionStrings:SupabasePostgres`
- `AdminAccount:Username`
- `AdminAccount:Password` (12+ chars outside Development)
- `Cloudinary:CloudName`
- `Cloudinary:ApiKey`
- `Cloudinary:ApiSecret`
- `Cloudinary:Folder`

`appsettings.json` includes only non-secret defaults (for example `Cloudinary:Folder`).

### Example local setup with user-secrets

From repository root:

```bash
dotnet user-secrets set "ConnectionStrings:SupabasePostgres" "<your-supabase-postgres-connection-string>"
dotnet user-secrets set "AdminAccount:Username" "admin"
dotnet user-secrets set "AdminAccount:Password" "replace-with-strong-password"
dotnet user-secrets set "Cloudinary:CloudName" "your-cloud-name"
dotnet user-secrets set "Cloudinary:ApiKey" "your-api-key"
dotnet user-secrets set "Cloudinary:ApiSecret" "your-api-secret"
dotnet user-secrets set "Cloudinary:Folder" "student-portal"
```

## Database bootstrapping

In Development, the app executes `/Data/SupabaseSchema.sql` at startup to create required tables/indexes if they do not already exist.

## Run the app

```bash
dotnet restore
dotnet run
```

Default local URLs are configured in `Properties/launchSettings.json`:

- `http://localhost:5025`
- `https://localhost:7005`

## Admin usage

1. Open `/Account/Login`.
2. Sign in with `AdminAccount` credentials.
3. Use sidebar pages:
   - **To Review**: approve/reject pending submissions
   - **Shared**: review/delete approved content
   - **Manage Rooms**: create/delete rooms

## Validation commands

```bash
dotnet build
dotnet test
```

## Android (Capacitor) wrapper

`/Android` contains a Capacitor Android project configured to load a hosted Student Portal URL (see `Android/capacitor.config.ts`). It is separate from local ASP.NET development.

## Troubleshooting

- **Startup fails with missing configuration**  
  Ensure all required settings above are present.
- **Uploads fail**  
  Confirm Cloudinary credentials and allowed file types.
- **No content visible in room**  
  Submissions must be approved by an admin before appearing publicly.
- **File actions return errors**  
  Verify the referenced Cloudinary asset still exists and database file metadata is valid.
