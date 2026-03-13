namespace Audit.API.Application;

public record PagedResponse<T> {
	public List<T> Items { get; init; } = new();
	public int Page { get; init; }
	public int PageSize { get; init; }
	public long TotalCount { get; init; }
	public int TotalPages { get; init; }
}
