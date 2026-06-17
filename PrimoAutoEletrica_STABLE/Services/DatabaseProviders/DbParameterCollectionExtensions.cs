using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using System;
using System.Data.Common;

namespace System.Data.Common
{
    public static class DbParameterCollectionExtensions
    {
        public static DbParameter AddWithValue(this DbParameterCollection parameters, string parameterName, object? value)
        {
            if (parameters is null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            DbParameter parameter = parameters.GetType().FullName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true
                ? new SqliteParameter(parameterName, value ?? DBNull.Value)
                : new SqlParameter(parameterName, value ?? DBNull.Value);

            parameters.Add(parameter);
            return parameter;
        }
    }
}
