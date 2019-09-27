using System;
using Sara.NETStandard.Data.SQL.CommandFormat;

namespace Sara.NETStandard.Data.SQL
{
    /// <summary>
    /// All external database implementations should implement this interface. 
    /// This allows us to be more Database agnostic.
    /// </summary>
    public interface IExternalDatabase
    {
        /// <summary>
        /// Creates a new connection to the database.
        /// </summary>
        /// <returns>A new connection to the database</returns>
        /// <exception cref="Exception">Some sort of exception occurred while trying to connect.</exception>
        IDatabaseConnection CreateConnection();

        string ConnectionString { get; }
        ExternalDatabaseType Type { get; }
        IDatabaseCommandFormat CommandFormat { get; }

        // timeout is for connection timeout.
        // it is not supported on all databases.
        int ConnectionTimeout { get; set; }
        int CommandTimeout { get; set; }
    }
}
