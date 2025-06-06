namespace DrHan.Application.Commons
{
    public interface IPaginatedList<T>
    {
        IReadOnlyList<T> Items { get; }
        int PageNumber { get; }
        int PageSize { get; }
        int TotalCount { get; }
        int TotalPages { get; }
        bool HasPreviousPage { get; }
        bool HasNextPage { get; }
    }
    public class PaginatedList<T> : IPaginatedList<T>
    {
        public IReadOnlyList<T> Items { get; }
        public int PageNumber { get; }
        public int PageSize { get; }
        public int TotalCount { get; }
        public int TotalPages { get; }
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        public PaginatedList(IReadOnlyList<T> items, int pageNumber, int pageSize, int totalCount)
        {
            Items = items;
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalCount = totalCount;
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        }

        public static PaginatedList<T> Create(IReadOnlyList<T> items, int pageNumber, int pageSize, int totalCount)
        {
            return new PaginatedList<T>(items, pageNumber, pageSize, totalCount);
        }
    }
}
