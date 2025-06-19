using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrHan.Application.Commons
{
    public class AppResponse<T>
    {
        public bool IsSucceeded { get; private set; } = true;
        public DateTime Timestamp { get; private set; } = DateTime.UtcNow;
        public Dictionary<string, string[]> Messages { get; private set; } = [];
        public T? Data { get; private set; }
        public PaginationInfo? Pagination { get; private set; }

        public AppResponse<T> SetSuccessResponse(T data)
        {
            Data = data;
            return this;
        }

        public AppResponse<T> SetSuccessResponse(T data, string key, string value)
        {
            Data = data;
            Messages.Add(key, [value]);
            return this;
        }

        public AppResponse<T> SetSuccessResponse(T data, Dictionary<string, string[]> message)
        {
            Data = data;
            Messages = message;
            return this;
        }

        public AppResponse<T> SetSuccessResponse(T data, string key, string[] value)
        {
            Data = data;
            Messages.Add(key, value);
            return this;
        }
        public AppResponse<T> SetErrorResponse(T data, string key, string value)
        {
            IsSucceeded = false;
            Data = data;
            Messages.Add(key, [value]);
            return this;
        }
        public AppResponse<T> SetErrorResponse(string key, string value)
        {
            IsSucceeded = false;
            Messages.Add(key, [value]);
            return this;
        }

        public AppResponse<T> SetErrorResponse(string key, string[] value)
        {
            IsSucceeded = false;
            Messages.Add(key, value);
            return this;
        }

        public AppResponse<T> SetErrorResponse(Dictionary<string, string[]> message)
        {
            IsSucceeded = false;
            Messages = message;
            return this;
        }

        public AppResponse<T> SetPagination(int pageNumber, int pageSize, int totalCount)
        {
            Pagination = new PaginationInfo
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
            return this;
        }
    }

    public class PaginationInfo
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
