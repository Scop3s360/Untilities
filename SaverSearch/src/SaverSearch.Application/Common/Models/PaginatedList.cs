namespace SaverSearch.Application.Common.Models;

public class PaginatedList<T>
{
    public IEnumerable<T> Items { get; }
    public int CurrentPage { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages { get; }

    public PaginatedList(IEnumerable<T> items, int count, int pageNumber, int pageSize)
    {
        CurrentPage = pageNumber;
        PageSize = pageSize;
        TotalCount = count;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        Items = items;
    }
}
