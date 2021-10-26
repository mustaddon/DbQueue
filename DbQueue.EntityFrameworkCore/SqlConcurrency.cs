using System.Collections.Generic;

namespace DbQueue.EntityFrameworkCore
{
    internal class SqlConcurrency
    {
        public static string? GetAndLock(string providerName)
        {
            return GetAndLockDict.TryGetValue(providerName, out var sql) ? sql : null;
        }

        static readonly Dictionary<string, string> GetAndLockDict = new()
        {
            {
                "Microsoft.EntityFrameworkCore.SqlServer",
                @";WITH item AS (
	                SELECT * FROM [DbQueue]
	                WHERE [Queue]=@p0 and ([LockId] is NULL or [LockId]<@p4) and ([AvailableAfter] is NULL or [AvailableAfter]<@p5)
	                ORDER BY 
		                CASE WHEN @p1=0 THEN [Id] END ASC,
		                CASE WHEN @p1=1 THEN [Id] END DESC
	                OFFSET @p2 ROWS 
	                FETCH NEXT 1 ROWS ONLY) 
                UPDATE item SET [LockId]=@p3
                SELECT * FROM [DbQueue] WHERE [Queue]=@p0 and [LockId]=@p3"
            },
            {
                "Npgsql.EntityFrameworkCore.PostgreSQL",
                @"UPDATE ""DbQueue"" SET ""LockId""=@p3
                WHERE ""Id""=(
	                SELECT ""Id"" FROM ""DbQueue""
	                WHERE ""Queue""=@p0 and (""LockId"" is NULL or ""LockId""<@p4) and (""AvailableAfter"" is NULL or ""AvailableAfter""<@p5)
	                ORDER BY 
		                CASE WHEN @p1=FALSE THEN ""Id"" END ASC,
		                CASE WHEN @p1=TRUE THEN ""Id"" END DESC
	                OFFSET @p2 
	                LIMIT 1
	                FOR UPDATE SKIP LOCKED)
                RETURNING *;"
            },
        };

    }
}
