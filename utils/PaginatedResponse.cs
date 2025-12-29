public record PaginatedResponse <T>
(
    int totalCount,
    List<T> items
);