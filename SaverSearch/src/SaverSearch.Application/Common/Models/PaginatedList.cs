namespace SaverSearch.Application.Common.Models;

public class PaginatedList<T>
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }

    public PaginatedList()
    {
    }

    public PaginatedList(IEnumerable<T> items, int totalCount, int currentPage, int pageSize)
    {
        CurrentPage = currentPage;
        PageSize = pageSize;
        TotalCount = totalCount;
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        Items = items;
    }
}
