using System.Collections.Concurrent;

namespace DbQueue.EntityFrameworkCore
{
    internal class SqlConcurrency
    {
        public static string? GetAndLock(string? providerName, string tableName)
        {
            return _cache.GetOrAdd(new { providerName, tableName }, k => GetValue(providerName, tableName));
        }

        static readonly ConcurrentDictionary<object, string?> _cache = new();

        static string? GetValue(string? providerName, string tableName)
        {
            if (providerName == "Microsoft.EntityFrameworkCore.SqlServer")
                return $@";WITH cte AS (
	                SELECT * FROM [{tableName}]
	                WHERE [Queue]=@p0 and ([LockId] is NULL or [LockId]<@p4) and ([AvailableAfter] is NULL or [AvailableAfter]<@p5)
	                ORDER BY 
		                CASE WHEN @p1=0 THEN [Id] END ASC,
		                CASE WHEN @p1=1 THEN [Id] END DESC
	                OFFSET @p2 ROWS 
	                FETCH NEXT 1 ROWS ONLY) 
                UPDATE cte SET [LockId]=@p3
                SELECT * FROM [{tableName}] WHERE [Queue]=@p0 and [LockId]=@p3";

            if (providerName == "Npgsql.EntityFrameworkCore.PostgreSQL")
                return $@"UPDATE ""{tableName}"" SET ""LockId""=@p3
                WHERE ""Id""=(
	                SELECT ""Id"" FROM ""{tableName}""
	                WHERE ""Queue""=@p0 and (""LockId"" is NULL or ""LockId""<@p4) and (""AvailableAfter"" is NULL or ""AvailableAfter""<@p5)
	                ORDER BY 
		                CASE WHEN @p1=FALSE THEN ""Id"" END ASC,
		                CASE WHEN @p1=TRUE THEN ""Id"" END DESC
	                OFFSET @p2 
	                LIMIT 1
	                FOR UPDATE SKIP LOCKED)
                RETURNING *;";


            if (providerName == "MySql.EntityFrameworkCore" || providerName == "Pomelo.EntityFrameworkCore.MySql")
                return $@"LOCK TABLES `{tableName}` WRITE;
                UPDATE `{tableName}` SET `LockId`=@p3
                WHERE `Queue`=@p0 and (`LockId` is NULL or `LockId`<@p4) and (`AvailableAfter` is NULL or `AvailableAfter`<@p5)
                ORDER BY 
	                CASE WHEN @p1=0 THEN `Id` END ASC,
	                CASE WHEN @p1=1 THEN `Id` END DESC
                LIMIT 1;
                UNLOCK TABLES;
                SELECT * FROM `{tableName}` WHERE `Queue`=@p0 and `LockId`=@p3;";

            return null;
        }

    }
}
