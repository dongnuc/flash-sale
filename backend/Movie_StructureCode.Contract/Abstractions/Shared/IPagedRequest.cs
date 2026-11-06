namespace Movie_StructureCode.Contract.Abstractions.Shared;

public interface IPagedRequest
{
    int PageIndex { get; }
    int PageSize { get; }
}

public record PagedRequest(int PageIndex = 1, int PageSize = 10) : IPagedRequest;
