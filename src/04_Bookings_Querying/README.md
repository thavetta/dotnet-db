# Demo04 Bookings Querying

## Cíl
Psát efektivní dotazy: projekce, AsNoTracking, Include vs. projekce, globální filtr, split queries.
## Úkoly pro studenty
1. Uprav `appsettings.json` a spusť migraci:
   ```bash
   dotnet ef migrations add Initial -o Migrations
   dotnet ef database update
   ```
2. Spusť aplikaci a zavolej `POST /seed`.
3. Ověř data přes jednoduchý `GET`/`SELECT` dotaz dle zadání níže.

### Zadání – dotazování
- Rozšiř endpoint `/reservations` o filtrování `from/to` a stránkování `page/pageSize` (projekce do DTO).
- Porovnej `Include` vs. projekci: přidej endpoint `/authors-like` (analogicky) – vysvětli rozdíl v SQL.
- Zapni `AsSplitQuery()` u problematického dotazu a sleduj počet roundtripů v logu.
