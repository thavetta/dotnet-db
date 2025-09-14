# 01 – PUBS (read‑only)
**Cíl:** Připojit EF Core ke stávající DB `pubs`, nakonfigurovat `DbContext`, vypsat autory a počty jejich titulů.

## Kroky pro studenty
1. Uprav `appsettings.json` – nastav správný connection string k `pubs`.
2. Spusť projekt: `dotnet run`. Sleduj SQL v konzoli (zapnuté logování).
3. Přidej do modelu jednoduchý index (např. na `authors(au_lname)`) – ukaž přes fluent API.
4. (Stretch) Změň projekci tak, aby vracela TOP 5 autorů.

## Kontrolní body
- Konzole vypíše seznam autorů a počty titulů.
