public record PaginatedResponse <T>
(
    int totalCount,
    int totalPages,
    List<T> items
);