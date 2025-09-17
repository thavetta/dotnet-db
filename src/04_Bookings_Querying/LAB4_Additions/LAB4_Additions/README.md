# LAB4 – EF Core: Seeding, Loading (Eager/Lazy), Compiled Queries, Stored Procedures, SplitQuery, DbFunction

Projekt: **03_Bookings_MappingAdvanced** (net9.0)

## Co je v tomto balíčku
Tento balíček přidává kompletní **LAB4** bez zásahu do stávajících tříd — stačí:
1. Přidat balíček **Bogus** do `.csproj`
2. V `Program.cs` zaregistrovat seeding a endpointy (viz níže)
3. (Volitelné) Přidat mapování SP a DbFunction do `BookingsDbContext.OnModelCreating` (viz níže)
4. Spustit SQL skripty ze složky `sql` (funkce + uložené procedury)

### Přehled přidaných souborů
- `Infrastructure/SeedingExtensions.cs` – endpoint **POST /seed** (idempotentní runtime seeding přes Bogus)
- `Infrastructure/DbFunctions.cs` – CLR definice pro T‑SQL funkci `fn_Nights`
- `Queries/Compiled.cs` – ukázka **EF.CompileAsyncQuery**
- `Dtos/ReservationDtos.cs` – DTO pro CRUD přes SP
- `Endpoints/Lab4Endpoints.cs` – zaregistruje všechny endpointy popsané v zadání
- `sql/01_Create_Function_fn_Nights.sql` – vytvoření `dbo.fn_Nights`
- `sql/02_Create_SPs_Reservation_CUD.sql` – vytvoření `dbo.Reservation_Insert/Update/Delete`
- `sql/00_All.sql` – volá oba výše uvedené

---

## 1) Instalace NuGet balíčku
```xml
<ItemGroup>
  <PackageReference Include="Bogus" Version="35.*" />
</ItemGroup>
```

## 2) Změny v `Program.cs`
```csharp
using Bookings.Infrastructure;
using Bookings.Endpoints;

var app = builder.Build();

// LAB4
app.UseSeeding();
app.MapLab4Endpoints();

app.Run();
```

> Pokud už v `Program.cs` máte health‑checky apod., řádky výše jen přidejte.

## 3) Změny v `BookingsDbContext.OnModelCreating`
### a) Mapování uložených procedur (CUD) pro `Reservation`
```csharp
modelBuilder.Entity<Reservation>().InsertUsingStoredProcedure("dbo.Reservation_Insert");
modelBuilder.Entity<Reservation>().UpdateUsingStoredProcedure("dbo.Reservation_Update");
modelBuilder.Entity<Reservation>().DeleteUsingStoredProcedure("dbo.Reservation_Delete");
```
### b) Mapování T‑SQL funkce `fn_Nights` do LINQ
```csharp
using System.Reflection;
using Bookings.Infrastructure;

var method = typeof(DbFunctions).GetMethod(nameof(DbFunctions.Nights), new[] { typeof(DateOnly), typeof(DateOnly) });
modelBuilder.HasDbFunction(method!).HasName("fn_Nights").HasSchema("dbo");
```

> **Pozn:** Necháváme čistě runtime seeding – všechny `HasData(...)` v modelu **odstraňte**.

---

## 4) Endpointy (souhrn)
- `POST /seed?guests=&rooms=&reservations=` – naplní DB (idempotentně; default 200/50/500)
- `GET /reservations/eager` – Eager Loading (`Include`)
- `GET /reservations/lazy` – Lazy Loading (vyžaduje `UseLazyLoadingProxies()` a `virtual` navigace)
- `GET /reservations/by-email?email=` – zkompilovaná query
- `POST /reservations` – INSERT přes SP (pomocí mapování v modelu)
- `PUT /reservations/{id}` – UPDATE přes SP
- `DELETE /reservations/{id}` – DELETE přes SP
- `GET /reservations/full` – „nalož vše“ (single‑query)
- `GET /reservations/full-split` – totéž, ale `AsSplitQuery()`
- `GET /stats/top-longstays?minNights=3` – použití `fn_Nights` v LINQ

---

## 5) SQL
Spusťte ve vaší DB v pořadí:
1. `sql/01_Create_Function_fn_Nights.sql`
2. `sql/02_Create_SPs_Reservation_CUD.sql`
(nebo `sql/00_All.sql`)

---

## 6) Testovací postup
```bash
dotnet run
curl -X POST "http://localhost:5000/seed?guests=300&rooms=80&reservations=1200"
curl "http://localhost:5000/reservations/eager"
curl "http://localhost:5000/reservations/lazy"
curl "http://localhost:5000/reservations/by-email?email=alice@example.com"
curl "http://localhost:5000/reservations/full"
curl "http://localhost:5000/reservations/full-split"
curl "http://localhost:5000/stats/top-longstays?minNights=5"
```
