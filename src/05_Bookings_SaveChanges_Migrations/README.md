# Demo05 Bookings SaveChanges Migrations

## Cíl
Porozumět SaveChanges, ChangeTrackeru, optimistic concurrency (rowversion), transakcím a migration bundle.
## Úkoly pro studenty
1. Uprav `appsettings.json` a spusť migraci:
   ```bash
   dotnet ef migrations add Initial -o Migrations
   dotnet ef database update
   ```
2. Spusť aplikaci a zavolej `POST /seed`.
3. Ověř data přes jednoduchý `GET`/`SELECT` dotaz dle zadání níže.

### Zadání – zápis změn & migrace
- Přidej `RowVersion` na `Reservation` (již je v modelu) a nasimuluj konflikt dvou klientů (dva paralelní požadavky).
- Implementuj handler konfliktu (store wins / client wins / merge) a ukaž rozdíl.
- Vytvoř **migration bundle**:
  ```bash
  dotnet ef migrations bundle -o ./BookingsBundle.exe
  ```
- (SqlServer) Přidej **temporal table** na `Reservations` a endpoint `/reservations/{id}/history` s `FOR SYSTEM_TIME AS OF` (hinty v zadání).
