using System.Net;

namespace Clean.Application.Dtos.Responses;

public class PaginatedResponse<T> : Response<List<T>>
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalRecords { get; set; }

    public PaginatedResponse(List<T> data, int pageNumber, int pageSize, int totalRecords)
        : base(HttpStatusCode.OK, "Success", data)
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalRecords = totalRecords;
    }
}