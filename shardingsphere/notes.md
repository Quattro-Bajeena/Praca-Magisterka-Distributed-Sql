- udało się tylko tryb mysql uruchomić. dlaczego?

Could not create a test database dla większości wersji

- ponieważ pomiędzy 5.4 a 5.5 zmieniły się nazwy i format plików konfiguracyjnych
- pomiędzy 5.2 a 5.3 zmienił się format pliku konfiguracyjnego


większość testów sfailowało - dlaczego?

- nie posiada postgresowych katalogów, więc trzeba osobno obsłużyć podczas otwierania połączenia. Rozwiązane.
- nie mogę się połączyć za pomocą klienta MySQL. nie ma błędu ale się wiesza w nieskończoność. Powinno to działać.
- z jakiegoś powodu nie używają schematów.