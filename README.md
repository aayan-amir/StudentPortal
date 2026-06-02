# Student Portal

ASP.NET Core student content approval portal backed by PostgreSQL and Cloudinary.

## Required Configuration

Set these values in the hosting provider environment or with `.NET user-secrets` for local development:

- `ConnectionStrings__SupabasePostgres`
- `Cloudinary__CloudName`
- `Cloudinary__ApiKey`
- `Cloudinary__ApiSecret`
- `Cloudinary__Folder`
- `AdminAccount__Username`
- `AdminAccount__Password`

Outside `Development`, `AdminAccount__Password` must be at least 12 characters. Do not commit real database, Cloudinary, or admin credentials to `appsettings*.json`.
