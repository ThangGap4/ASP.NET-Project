using Microsoft.EntityFrameworkCore;

namespace QuizAI.Api.Models;

public record PaginationParams(
    int Page = 1,
    int PageSize = 20
)
{
    public int Skip => (Page - 1) * PageSize;
    
    public int PageSizeCapped => PageSize switch
    {
        < 1 => 20,
        > 100 => 100,
        _ => PageSize
    };
}

public record PaginatedResult<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
)
{
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
    
    public static async Task<PaginatedResult<T>> CreateAsync(
        IQueryable<T> query,
        PaginationParams pagination)
    {
        var totalCount = await query.CountAsync();
        var items = await query
            .Skip(pagination.Skip)
            .Take(pagination.PageSizeCapped)
            .ToListAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pagination.PageSizeCapped);
        
        return new PaginatedResult<T>(items, totalCount, pagination.Page, pagination.PageSizeCapped, totalPages);
    }
}
