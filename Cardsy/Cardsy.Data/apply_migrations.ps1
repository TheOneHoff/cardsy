dotnet ef migrations script --project Cardsy.Data --startup-project Cardsy.API --idempotent

dotnet ef database update --project Cardsy.Data --startup-project Cardsy.API