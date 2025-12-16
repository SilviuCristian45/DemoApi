public record PaginatedQueryDto (
    int PageNumber = 1,      // Dacă nu se trimite, e 1
    int PageSize = 10,       // Dacă nu se trimite, e 10
    string Search = ""       // Dacă nu se trimite, e string gol
);