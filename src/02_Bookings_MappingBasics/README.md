# Demo02 Bookings MappingBasics

## Cíl
Postavit základ domény Bookings, nastavit DbContext, vztahy a první migraci + seed.
## Úkoly pro studenty
1. Uprav `appsettings.json` a spusť migraci:
   ```bash
   dotnet ef migrations add Initial -o Migrations
   dotnet ef database update
   ```
2. Spusť aplikaci a zavolej `POST /seed`.
3. Ověř data přes jednoduchý `GET`/`SELECT` dotaz dle zadání níže.
