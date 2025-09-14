# EF Core Workshop – 5 postupových projektů

Tento balík obsahuje **5 samostatných projektů**, které na sebe didakticky navazují. Každý projekt má vlastní `README.md` s úkoly pro studenty.

1. **01_Pubs_Readonly** – připojení k existující DB *pubs*, mapování tabulek a jednoduché dotazy (read-only).
2. **02_Bookings_MappingBasics** – založení domény *Bookings*, základní mapování (klíče, vztahy, indexy), první migrace + seed.
3. **03_Bookings_MappingAdvanced** – owned types (Money), value converters (Email), dědičnost TPH, explicitní many-to-many.
4. **04_Bookings_Querying** – LINQ projekce, Include vs. projekce, AsNoTracking, split queries, globální filtry, kompilované dotazy.
5. **05_Bookings_SaveChanges_Migrations** – SaveChanges, ChangeTracker, optimistic concurrency (rowversion), transakce, migration bundle, temporal tables nástin.

> Všechny projekty předpokládají **.NET 9** a **SQL Server** (lokálně/Container). Připojovací řetězce lze upravit v `appsettings.json`.
