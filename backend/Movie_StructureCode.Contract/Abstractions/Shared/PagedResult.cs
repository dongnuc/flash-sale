namespace Movie_StructureCode.Contract.Abstractions.Shared
{
    public class PagedResult<T>
    {
        public const int UperPageSize = 100;
        public const int DefaultPageSize = 10;
        public const int DefaultPageIndex = 1;

        public PagedResult(List<T> items,int pageIndex,int pageSize,int totalCount)
        {
            Items = items;
            PageIndex = pageIndex;
            PageSize = pageSize;
            TotalCount = totalCount;
        }


        public List<T> Items { get; } 
        public int PageIndex { get; } 
        public int PageSize { get; } 
        public int TotalCount { get; } 
        public bool HasNextPage => PageIndex *  PageSize < TotalCount; 
        public bool HasPrevious  => PageIndex > 1;

        
        public static async Task<PagedResult<T>> CreateAsync(IQueryable<T> source,
            int pageIndex, int pageSize)
        {
            pageIndex = pageIndex < 1 ? DefaultPageIndex : pageIndex;
            pageSize = pageSize < 1 ? DefaultPageSize :
                pageSize > UperPageSize ? UperPageSize : pageSize;
            var totalCount = await Task.FromResult(source.Count());
            var items = await Task.FromResult(source.Skip((pageIndex - 1) * pageSize)
                .Take(pageSize).ToList());
            return new PagedResult<T>(items, pageIndex, pageSize, totalCount);
        }


        public static PagedResult<T> Create(List<T> source,
            int pageIndex, int pageSize,int totalCount)
           => new(source, pageIndex, pageSize, totalCount);
    }
}
