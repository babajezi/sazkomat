Spusť pouze rychlé testy (Unit + Repository):

1. Přejdi do adresáře testů: `cd tests/Sazkomat.Tests`
2. Spusť pouze fast testy: `dotnet test --filter "Category=Fast" --logger "console;verbosity=normal"`
3. Zobraz shrnutí:
   - Počet prošlých testů
   - Počet selhaných testů
   - Seznam selhaných testů s chybovými hláškami (pokud nějaké selhaly)
4. Informuj uživatele o výsledku
