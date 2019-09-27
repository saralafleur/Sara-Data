using System;
using System.Data;

namespace Sara.NETStandard.Data.SQL
{
    /// <summary>
    /// All database connectors should implement this class. 
    /// </summary>
    /// <remarks>
    /// The IDatabaseConnection interface manages the database connection, querying and transactions.
    /// The "Get" methods will retrieve the column values for a given column. NextRow() must be called
    /// before any values can be retrieved. If duplicate column names exist in the data set use the 
    /// tableName.columnName format to reference the correct value.
    /// </remarks>
    public interface IDatabaseConnection : IDisposable
    {
        /// <summary>
        /// Returns the type of the database. Common values are: mysql, sqlserver
        /// </summary>
        string DatabaseType { get; }

        /// <summary>
        /// Opens the connection if it was closed. Otherwise there is no effect
        /// </summary>
        /// <returns>True if the database was connected to.</returns>
        bool Open();

        /// <summary>
        /// Closes the connection to the database. After this call Open() must be called 
        /// to use the connection again.
        /// </summary>
        void Close();

        /// <summary>
        /// The state of the current connection
        /// </summary>
        ConnectionState State { get; }

        #region Transaction Management

        /// <summary>
        /// Begins a transaction on the connection.
        /// </summary>
        /// <exception cref="InvalidOperationException">Nested transactions are not supported by the database.</exception>	
        void BeginTransaction();

        /// <summary>
        /// Commits a transaction
        /// </summary>
        /// <exception cref="Exception">An error occurred trying to Commit the transaction.</exception>
        /// <exception cref="InvalidOperationException">
        ///			There is no transaction 
        ///			<para>-or-</para>
        ///			The transaction has already been committed or rolled back.
        /// 		<para>-or-</para>	
        /// 		The connection is broken.
        /// </exception> 
        void CommitTransaction();

        /// <summary>
        /// Rollsback a transaction
        /// </summary>
        /// <exception cref="Exception">An error occurred trying to Rollback the transaction.</exception>
        /// <exception cref="InvalidOperationException">
        ///			There is no transaction 
        ///			<para>-or-</para>
        ///			The transaction has already been committed or rolled back.
        /// 		<para>-or-</para>	
        /// 		The connection is broken.
        /// </exception> 
        void RollbackTransaction();

        #endregion

        #region Database Commands

        /// <summary>
        /// Executes a SQL statement and returns number of rows affected.
        /// </summary>
        /// <param name="sqlCommandString">The SQL Command. This is database specific.</param>
        /// <param name="parameters"></param>
        /// <returns>Number of rows affected.</returns>
        /// <exception cref="Exception">A database error occurred. Most likely there is in error in the SQL statement.</exception>
        int ExecuteNonQuery(string sqlCommandString, params DbParam[] parameters);

        /// <summary>
        /// Executes a SQL statement a query statement that returns rows.
        /// </summary>
        /// <param name="sqlCommandString">The SQL Command. This is database specific.</param>
        /// <param name="parameters"></param>
        /// <exception cref="Exception">A database error occurred. Most likely there is in error in the SQL statement.</exception>
        /// <remarks>Use NextRow() and the Get methods to access the returned data.</remarks>
        void ExecuteQuery(string sqlCommandString, params DbParam[] parameters);

        void ExecuteStoredProcedure(string procedureName, params DbParam[] parameters);
        void ExecuteStoredProcedure(string procedureName, bool useMinimumLocking, params DbParam[] parameters);
        int ExecuteNonQueryStoredProcedure(string procedureName, params DbParam[] parameters);
        int ExecuteNonQueryStoredProcedure(string procedureName, bool useMinimumLocking, params DbParam[] parameters);

        /// <summary>
        /// Executes a SQL statement and returns a table containing the information.
        /// </summary>
        /// <param name="sqlCommandString">The SQL Command.  This is database specific.</param>
        /// <returns>A data table populated with the requested information.</returns>
        DataTable GetTable(string sqlCommandString);

        int GetInsertIdentifier();

        int CommandTimeout { get; set; }

        #endregion

        #region Result Set Methods

        /// <summary>
        /// Tells if the dataReader has any rows.
        /// </summary>
        bool HasRows { get; }

        /// <summary>
        /// Moves Row pointer to the next row.
        /// </summary>
        /// <returns>True of move was successfull. False, otherwise i.e. End of Result Set.</returns>
        bool NextRow();

        #endregion

        #region Row Methods

        /// <summary>
        /// Gets a column as a boolean value
        /// </summary>
        /// <param name="columnName">The name of the column to retrieve. If duplicate column name exist in the resultSet use the TableName.Columnname for the column name.</param>
        /// <returns>The value of the column. If null returns false.</returns>
        /// <exception cref="InvalidCastException">The specified cast is not valid.</exception>
        /// <exception cref="IndexOutOfRangeException">The specified columnName does not exist.</exception>
        bool GetBoolean(string columnName);

        bool? GetBooleanNullable(string columnName);

        /// <summary>
        /// Gets a column as a byte value
        /// </summary>
        /// <param name="columnName">The name of the column to retrieve.  If duplicate column name exist in the resultSet use the TableName.Columnname for the column name.</param>
        /// <returns>The value of the column. If null returns 0.</returns>
        /// <exception cref="InvalidCastException">The specified cast is not valid.</exception>
        /// <exception cref="IndexOutOfRangeException">The specified columnName does not exist.</exception>
        byte GetByte(string columnName);

        /// <summary>
        /// Reads a stream of bytes from the specified column offset into the buffer as an array starting at the given buffer offset.
        /// </summary>
        /// <param name="columnName">The name of the column.  If duplicate column name exist in the resultSet use the TableName.Columnname for the column name.</param>
        /// <param name="dataIndex">The index where reading will begin in the field.</param>
        /// <param name="buffer">The buffer to receive the stream of bytes.</param>
        /// <param name="bufferIndex">The index in the buffer to receive the data</param>
        /// <param name="length">The max length to copy into the buffer.</param>
        /// <returns>The number of bytes read into the buffer. If null returns 0.</returns>
        /// <exception cref="IndexOutOfRangeException">The specified columnName does not exist.</exception>
        long GetBytes(string columnName, long dataIndex, byte[] buffer, int bufferIndex, int length);

        /// <summary>
        /// Gets a column as a char value
        /// </summary>
        /// <param name="columnName">The name of the column to retrieve.  If duplicate column name exist in the resultSet use the TableName.Columnname for the column name.</param>
        /// <returns>The value of the column. If null returns char.MinValue</returns>
        /// <exception cref="InvalidCastException">The specified cast is not valid.</exception>
        /// <exception cref="IndexOutOfRangeException">The specified columnName does not exist.</exception>
        char GetChar(string columnName);

        /// <summary>
        /// Retrieves an array of chars from the column.
        /// </summary>
        /// <param name="columnName">Name of the column.  If duplicate column name exist in the resultSet use the TableName.Columnname for the column name.</param>
        /// <param name="fieldoffset">Where to begin reading in the field.</param>
        /// <param name="buffer">Buffer to store the read data.</param>
        /// <param name="bufferoffset">Offset into the buffer.</param>
        /// <param name="length">Max length to read.</param>
        /// <returns>Actual number of chars read. If null returns 0.</returns>
        /// <exception cref="IndexOutOfRangeException">The specified columnName does not exist.</exception>		
        long GetChars(string columnName, long fieldoffset, char[] buffer, int bufferoffset, int length);

        /// <summary>
        /// Retrieves a Guid
        /// </summary>
        /// <param name="columnName">Name of the column.  If duplicate column name exist in the resultSet use the TableName.Columnname for the column name.</param>
        /// <returns>Value of the column. If null returns 0.</returns>
        /// <exception cref="InvalidCastException">The specified cast is not valid.</exception>
        /// <exception cref="IndexOutOfRangeException">The specified columnName does not exist.</exception>
        Guid GetGuid(string columnName);

        /// <summary>
        /// Retrieves DateTime
        /// </summary>
        /// <param name="columnName">Name of the column.  If duplicate column name exist in the resultSet use the TableName.Columnname for the column name.</param>
        /// <returns>Value of the column. If column is null returns DateTime.MinValue</returns>
        /// <exception cref="InvalidCastException">The specified cast is not valid.</exception>
        /// <exception cref="IndexOutOfRangeException">The specified columnName does not exist.</exception>
        DateTime GetDateTime(string columnName);

        DateTime? GetDateTimeNullable(string columnName);

        /// <summary>
        /// Retrieves Decimal
        /// </summary>
        /// <param name="columnName">Name of the column.  If duplicate column name exist in the resultSet use the TableName.Columnname for the column name.</param>
        /// <returns>Value of the column. If null returns 0.</returns>
        /// <exception cref="InvalidCastException">The specified cast is not valid.</exception>
        /// <exception cref="IndexOutOfRangeException">The specified columnName does not exist.</exception>
        decimal GetDecimal(string columnName);

        /// <summary>
        /// Retrieves Double
        /// </summary>
        /// <param name="columnName">Name of the column.  If duplicate column name exist in the resultSet use the TableName.Columnname for the column name.</param>
        /// <returns>Value of the column. If null returns 0.</returns>
        /// <exception cref="InvalidCastException">The specified cast is not valid.</exception>
        /// <exception cref="IndexOutOfRangeException">The specified columnName does not exist.</exception>
        double GetDouble(string columnName);

        /// <summary>
        /// Retrieves Float
        /// </summary>
        /// <param name="columnName">Name of the column.  If duplicate column name exist in the resultSet use the TableName.Columnname for the column name.</param>
        /// <returns>Value of the column. If null returns 0.</returns>
        /// <exception cref="InvalidCastException">The specified cast is not valid.</exception>
        /// <exception cref="IndexOutOfRangeException">The specified columnName does not exist.</exception>
        float GetFloat(string columnName);

        /// <summary>
        /// Retrieves an short
        /// </summary>
        /// <param name="columnName">Name of the column.  If duplicate column name exist in the resultSet use the TableName.Columnname for the column name.</param>
        /// <returns>Value of the column. If null returns 0.</returns>
        /// <exception cref="InvalidCastException">The specified cast is not valid.</exception>
        /// <exception cref="IndexOutOfRangeException">The specified columnName does not exist.</exception>
        short GetShort(string columnName);

        /// <summary>
        /// Retrieves an int
        /// </summary>
        /// <param name="columnName">Name of the column.  If duplicate column name exist in the resultSet use the TableName.Columnname for the column name.</param>
        /// <returns>Value of the column. If null returns 0.</returns>
        /// <exception cref="InvalidCastException">The specified cast is not valid.</exception>
        int GetInt(string columnName);

        /// <summary>
        /// Retrieves a long
        /// </summary>
        /// <param name="columnName">Name of the column.  If duplicate column name exist in the resultSet use the TableName.Columnname for the column name.</param>
        /// <returns>Value of the column. If null returns 0.</returns>
        /// <exception cref="InvalidCastException">The specified cast is not valid.</exception>
        /// <exception cref="IndexOutOfRangeException">The specified columnName does not exist.</exception>
        long GetLong(string columnName);

        /// <summary>
        /// Retrieves a string
        /// </summary>
        /// <param name="columnName">Name of the column.  If duplicate column name exist in the resultSet use the TableName.Columnname for the column name.</param>
        /// <returns>Value of the column. If null returns 0.</returns>
        /// <exception cref="InvalidCastException">The specified cast is not valid.</exception>
        /// <exception cref="IndexOutOfRangeException">The specified columnName does not exist.</exception>
        string GetString(string columnName);

        /// <summary>
        /// Checks if the column is null
        /// </summary>
        /// <param name="columnName">Name of the column.  If duplicate column name exist in the resultSet use the TableName.Columnname for the column name.</param>
        /// <returns>True if null, false otherwise</returns>		
        /// <exception cref="IndexOutOfRangeException">The specified columnName does not exist.</exception>
        bool IsDBNull(string columnName);

        #endregion

        IDbCommand CreateCommand(string commandString);
    }
}
