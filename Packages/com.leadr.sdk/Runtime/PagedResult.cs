using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leadr.Internal;

namespace Leadr
{
    public class PagedResult<T>
    {
        public List<T> Items { get; private set; }
        public int Count { get; private set; }
        public bool HasNext { get; private set; }
        public bool HasPrev { get; private set; }

        internal string NextCursor { get; private set; }
        internal string PrevCursor { get; private set; }
        internal Func<string, Task<LeadrResult<PagedResult<T>>>> FetchPage { get; private set; }

        internal PagedResult(
            List<T> items,
            int count,
            bool hasNext,
            bool hasPrev,
            string nextCursor,
            string prevCursor,
            Func<string, Task<LeadrResult<PagedResult<T>>>> fetchPage)
        {
            Items = items;
            Count = count;
            HasNext = hasNext;
            HasPrev = hasPrev;
            NextCursor = nextCursor;
            PrevCursor = prevCursor;
            FetchPage = fetchPage;
        }

        public async Task<LeadrResult<PagedResult<T>>> NextPageAsync()
        {
            if (!HasNext || string.IsNullOrEmpty(NextCursor))
                return LeadrResult<PagedResult<T>>.Failure(0, "no_next_page", "No next page available");

            if (FetchPage == null)
                return LeadrResult<PagedResult<T>>.Failure(0, "fetch_unavailable", "Page fetch function not available");

            return await FetchPage(NextCursor);
        }

        public async Task<LeadrResult<PagedResult<T>>> PrevPageAsync()
        {
            if (!HasPrev || string.IsNullOrEmpty(PrevCursor))
                return LeadrResult<PagedResult<T>>.Failure(0, "no_prev_page", "No previous page available");

            if (FetchPage == null)
                return LeadrResult<PagedResult<T>>.Failure(0, "fetch_unavailable", "Page fetch function not available");

            return await FetchPage(PrevCursor);
        }

        internal static PagedResult<T> FromJson(
            Dictionary<string, object> json,
            Func<Dictionary<string, object>, T> itemParser,
            Func<string, Task<LeadrResult<PagedResult<T>>>> fetchPage)
        {
            if (json == null)
                return null;

            var items = new List<T>();
            var dataList = json.GetList("data");
            if (dataList != null)
            {
                foreach (var item in dataList)
                {
                    if (item is Dictionary<string, object> itemDict)
                    {
                        var parsed = itemParser(itemDict);
                        if (parsed != null)
                            items.Add(parsed);
                    }
                }
            }

            var pagination = json.GetDict("pagination");
            var count = pagination?.GetInt("count") ?? items.Count;
            var hasNext = pagination?.GetBool("has_next") ?? false;
            var hasPrev = pagination?.GetBool("has_prev") ?? false;
            var nextCursor = pagination?.GetString("next_cursor");
            var prevCursor = pagination?.GetString("prev_cursor");

            return new PagedResult<T>(
                items,
                count,
                hasNext,
                hasPrev,
                nextCursor,
                prevCursor,
                fetchPage);
        }
    }
}
