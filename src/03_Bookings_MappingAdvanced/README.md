# Demo03 Bookings MappingAdvanced

## Cíl
Procvičit pokročilé mapování: owned types (Money), value converter (Email), TPH dědičnost, explicitní many-to-many (Guest–Tag).
## Úkoly pro studenty
1. Uprav `appsettings.json` a spusť migraci:
   ```bash
   dotnet ef migrations add Initial -o Migrations
   dotnet ef database update
   ```
2. Spusť aplikaci a zavolej `POST /seed`.
3. Ověř data přes jednoduchý `GET`/`SELECT` dotaz dle zadání níže.

### Zadání – pokročilé mapování
- Přidej `Money` jako owned type (už je nastaveno) a rozšiř `Reservation` o `Price` validace.
- Přidej entity `Tag` a vazbu `GuestTag` (explicitní many-to-many). Doplň endpoint:
  `POST /guests/{id}/tags/{tagId}` pro přiřazení tagu.
- Přidej `Suite.HasLounge` a ukaž TPH.
