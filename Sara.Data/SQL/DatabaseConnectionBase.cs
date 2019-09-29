using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Sara.NETStandard.Logging;

namespace Sara.Data.SQL
{
    internal abstract class DatabaseConnectionBase
    {
        #region Class Variables

        /// <summary>
        /// Stores the Column name to index pairs
        /// </summary>
        protected Hashtable ColumnNameIndexPairs;

        /// <summary>
        /// The actual connection to the database.
        /// </summary>		
        protected IDbConnection InternalConnection;

        /// <summary>
        /// Assigned when the BeginTransaction method is called.
        /// Cleared when the transaction is committed or rolled-back.
        /// </summary>
        protected IDbTransaction CurrentTransaction;

        /// <summary>
        /// Assigned after a successful call to ExecuteQuery method.
        /// </summary>
        protected IDataReader DataReader;

        protected string Type = "unknown";

        private int _commandTimeout = -1;
        private static readonly object SyncOpenObject = new object();
        private readonly string _name;

        #endregion

        #region Constructor / Destructor

        protected DatabaseConnectionBase()
        {
            _name = GetType().Name;
        }

        protected DatabaseConnectionBase(ref IDbConnection connection, string type)
        {
            Type = type;
            InternalConnection = connection;

            _name = GetType().Name;
        }

        #endregion

        #region ColumnNameIndex Helper Functions

        protected virtual void GenerateColumnNamePairs()
        {
            var schemaTable = DataReader?.GetSchemaTable();
            if (schemaTable == null)
                return;

            ColumnNameIndexPairs = new Hashtable();
            var i = 0;
            foreach (DataRow row in schemaTable.Rows)
            {
                // Add only the name of the field to the index
                var name = row["ColumnName"].ToString();
                try
                {
                    if (!ColumnNameIndexPairs.ContainsKey(name))
                        ColumnNameIndexPairs.Add(name, i);
                }
                catch
                {

                }

                // Add the tablename.fieldname to the index as a key
                var rowBaseTableName = row["baseTableName"];
                try
                {
                    if (rowBaseTableName == DBNull.Value)
                        continue;

                    name = ((string)rowBaseTableName) + "." + name;
                    if (!ColumnNameIndexPairs.ContainsKey(name))
                        ColumnNameIndexPairs.Add(name, i);
                }
                catch
                {
                    // name probably already exists.
                    // We should be covered.
                }
                i++;
            }
        }

        #endregion

        #region Public Parameters

        public virtual ConnectionState State => InternalConnection.State;

        #endregion

        #region IDatabaseConnection Members

        public virtual string DatabaseType => Type;

        public virtual int CommandTimeout
        {
            get => _commandTimeout;
            set => _commandTimeout = value;
        }

        public bool Open()
        {
            var success = false;
            if (InternalConnection.State == ConnectionState.Closed || InternalConnection.State == ConnectionState.Broken)
            {
                try
                {
                    lock (SyncOpenObject)
                        InternalConnection.Open();

                    if (InternalConnection.State != ConnectionState.Closed && InternalConnection.State != ConnectionState.Broken)
                        success = true;
                }
                catch (Exception e)
                {
                    Log.WriteError(_name, MethodBase.GetCurrentMethod().Name, e);
                }
            }
            else
                success = true;

            return success;
        }

        public void Close()
        {
            InternalConnection.Close();
        }

        public void Dispose()
        {
            Close();
        }

        public void BeginTransaction()
        {
            CurrentTransaction = InternalConnection.BeginTransaction();
        }

        public void CommitTransaction()
        {
            if (CurrentTransaction == null)
                throw new InvalidOperationException("No transaction exists.");

            CurrentTransaction.Commit();
            CurrentTransaction = null;
        }

        public void RollbackTransaction()
        {
            if (CurrentTransaction == null)
                throw new InvalidOperationException("No transaction exists.");

            CurrentTransaction.Rollback();
            CurrentTransaction = null;
        }

        private int TimeExecuteNonQuery(IDbCommand command)
        {
            var executionTimer = new Stopwatch();
            executionTimer.Start();

            Exception executeException = null;
            var rowsAffected = 0;
            try
            {
                rowsAffected = command.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                // store this so we can rethrow it after stopping the stopwatch
                executeException = e;
            }
            finally
            {
                executionTimer.Stop();

                // we have to wrap this call in a try-catch to make it benign
                try
                {
                    LogDatabaseCall(command, executionTimer.Elapsed, executeException);
                }
                catch (Exception e)
                {
                    Log.WriteError(GetType().FullName, MethodBase.GetCurrentMethod().Name, e);
                }
            }

            // rethrow exception if needed
            if (executeException != null)
                throw executeException;

            return rowsAffected;
        }

        private IDataReader TimeExecuteReader(IDbCommand command, CommandBehavior behavior)
        {
            var executionTimer = new Stopwatch();
            executionTimer.Start();

            Exception executeException = null;
            IDataReader reader = null;
            try
            {
                reader = command.ExecuteReader(behavior);
            }
            catch (Exception e)
            {
                // store this so we can rethrow it after stopping the stopwatch
                executeException = e;
            }
            finally
            {
                executionTimer.Stop();

                // we have to wrap this call in a try-catch to make it benign
                try
                {
                    LogDatabaseCall(command, executionTimer.Elapsed, executeException);
                }
                catch (Exception e)
                {
                    Log.WriteError(GetType().FullName, MethodBase.GetCurrentMethod().Name, e);
                }
            }

            if (executeException != null)
                throw executeException;

            return reader;
        }

        protected virtual void LogDatabaseCall(IDbCommand command, TimeSpan duration, Exception exception)
        {
            var isExcessiveDuration = (duration.TotalMilliseconds >= DefaultValue.DbProcThresholdBeforeLogMilliseconds);

            if (!IsDebug && exception == null && !isExcessiveDuration)
                return;

            var logText = new StringBuilder();

            logText.AppendLine("Database Command Details");
            logText.AppendFormat("  SQL:           {0}\r\n", command.CommandText);
            logText.AppendFormat("  Connection ID: {0}\r\n", command.Connection);
            logText.AppendFormat("  Duration:      {0:0.0000} seconds", duration.TotalSeconds);
            if (command.Parameters != null && command.Parameters.Count > 0)
            {
                logText.Append("\r\n  Parameters:");
                foreach (IDataParameter parameter in command.Parameters)
                    logText.AppendFormat(
                        $"\r\n    {parameter.ParameterName}({parameter.DbType}|{parameter.Direction})={parameter.Value}");
            }

            if (exception == null) return;

            logText.AppendFormat("\r\n  SQL Exception: {0}", exception.Message);
            Log.WriteTrace(logText.ToString(), GetType().FullName, MethodBase.GetCurrentMethod().Name);
        }

        public bool IsDebug { get; set; }

        public int ExecuteNonQuery(string sqlCommandString, params DbParam[] parameters)
        {
            if (DataReader != null)
            {
                DataReader.Close();
                DataReader = null;
            }

            var sqlCommand = InternalConnection.CreateCommand();
            sqlCommand.CommandText = sqlCommandString;
            var timeout = _commandTimeout;
            if (timeout >= 0)
                sqlCommand.CommandTimeout = timeout;

            PrepParameters(sqlCommand, parameters);

            if (CurrentTransaction != null)
                sqlCommand.Transaction = CurrentTransaction;

            if (parameters != null)
                sqlCommand.Prepare();

            return TimeExecuteNonQuery(sqlCommand);
        }

        private void PrepParameters(IDbCommand sqlCommand, IEnumerable<DbParam> parameters)
        {
            if (parameters == null)
                return;

            foreach (var parameter in parameters)
            {
                var commandParameter = sqlCommand.CreateParameter();
                commandParameter.ParameterName = GetParameterName(parameter.Name);
                commandParameter.Value = parameter.Value ?? DBNull.Value;
                sqlCommand.Parameters.Add(commandParameter);
            }
        }

        public void ExecuteQuery(string sqlCommandString, params DbParam[] parameters)
        {
            if (DataReader != null)
            {
                DataReader.Close();
                DataReader = null;
            }

            var sqlCommand = InternalConnection.CreateCommand();
            sqlCommand.CommandText = sqlCommandString;
            var timeout = _commandTimeout;
            if (timeout >= 0)
                sqlCommand.CommandTimeout = timeout;
            PrepParameters(sqlCommand, parameters);

            if (CurrentTransaction != null) sqlCommand.Transaction = CurrentTransaction;

            if (parameters != null)
                sqlCommand.Prepare();

            DataReader = TimeExecuteReader(sqlCommand, CommandBehavior.KeyInfo);
            GenerateColumnNamePairs();
        }

        private void EnableMinimumLocking()
        {
            if (Type != "mssql" && Type != "mysql")
                return;

            ExecuteNonQuery("SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED");
        }

        public void ExecuteStoredProcedure(string procedureName, bool useMinimumLocking, params DbParam[] parameters)
        {
            if (useMinimumLocking)
                EnableMinimumLocking();

            if (DataReader != null)
            {
                DataReader.Close();
                DataReader = null;
            }

            var command = InternalConnection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = procedureName;
            PrepParameters(command, parameters);

            if (_commandTimeout >= 0)
                command.CommandTimeout = _commandTimeout;

            if (CurrentTransaction != null)
                command.Transaction = CurrentTransaction;

            var failedAttempts = 0;

            while (failedAttempts < 3)
            {
                try
                {
                    DataReader = TimeExecuteReader(command, CommandBehavior.KeyInfo);
                    GenerateColumnNamePairs();
                    break;
                }
                catch (Exception e)
                {
                    failedAttempts++;

                    if (Type == "mysql" && e.Message.Contains("Deadlock found"))
                    {
                        if (failedAttempts >= 3)
                        {
                            throw new Exception(
                                $"Execution of stored procedure '{procedureName}' failed due to deadlock. {failedAttempts} attempts made.", e);
                        }
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            if (failedAttempts != 0)
                Log.Write("Execution of stored procedure '{procedureName}' succeeded after {failedAttempts - 1} failed attempts due to deadlock", _name, MethodBase.GetCurrentMethod().Name, LogEntryType.Error);
        }

        public int ExecuteNonQueryStoredProcedure(string procedureName, bool useMinimumLocking, params DbParam[] parameters)
        {
            if (useMinimumLocking)
                EnableMinimumLocking();

            if (DataReader != null)
            {
                DataReader.Close();
                DataReader = null;
            }

            var command = InternalConnection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = procedureName;
            PrepParameters(command, parameters);

            if (_commandTimeout >= 0)
                command.CommandTimeout = _commandTimeout;

            if (CurrentTransaction != null)
                command.Transaction = CurrentTransaction;

            var failedAttempts = 0;
            var rowsAffected = 0;

            while (failedAttempts < 3)
            {
                try
                {
                    rowsAffected = TimeExecuteNonQuery(command);
                    break;
                }
                catch (Exception e)
                {
                    failedAttempts++;

                    if (Type == "mysql" && e.Message.Contains("Deadlock found"))
                    {
                        if (failedAttempts >= 3)
                            throw new Exception(
                                $"Execution of stored procedure '{procedureName}' failed due to deadlock. {failedAttempts} attempts made.", e);
                    }
                    else
                        throw;
                }
            }

            if (failedAttempts != 0)
                Log.Write("Execution of stored procedure '{procedureName}' succeeded after {failedAttempts - 1} failed attempts due to deadlock",_name, MethodBase.GetCurrentMethod().Name, LogEntryType.Error);

            return rowsAffected;
        }

        public abstract bool HasRows
        {
            get;
        }

        public bool NextRow()
        {
            return DataReader.Read();
        }

        /// <remarks>Throws an exception if the column is null and it is read as a boolean</remarks>
        public bool GetBoolean(string columnName)
        {
            try
            {
                var index = DataReader.GetOrdinal(columnName);
                return !DataReader.IsDBNull(index) && DataReader.GetBoolean(index);
            }
            catch (Exception e)
            {
                if (e is InvalidCastException || e is IndexOutOfRangeException)
                    throw; 

                throw new InvalidCastException(e.Message, e);
            }
        }

        public bool? GetBooleanNullable(string columnName)
        {
            try
            {
                var index = DataReader.GetOrdinal(columnName);
                return DataReader.IsDBNull(index) ? (bool?) null : DataReader.GetBoolean(index);
            }
            catch (Exception e)
            {
                if (e is InvalidCastException || e is IndexOutOfRangeException)
                    throw;

                throw new InvalidCastException(e.Message, e);
            }
        }

        public byte GetByte(string columnName)
        {
            try
            {
                var index = DataReader.GetOrdinal(columnName);
                return DataReader.IsDBNull(index) ? (byte) 0 : DataReader.GetByte(index);
            }
            catch (Exception e)
            {
                if (e is InvalidCastException || e is IndexOutOfRangeException)
                    throw;

                throw new InvalidCastException(e.Message, e);
            }
        }

        /// <remarks>Throws an exception if the column is null and it is read as a boolean</remarks>
        public long GetBytes(string columnName, long dataIndex, byte[] buffer, int bufferIndex, int length)
        {
            try
            {
                var index = DataReader.GetOrdinal(columnName);
                return DataReader.IsDBNull(index) ? 0 : DataReader.GetBytes(index, dataIndex, buffer, bufferIndex, length);
            }
            catch (Exception e)
            {
                if (e is InvalidCastException || e is IndexOutOfRangeException)
                    throw;

                throw new InvalidCastException(e.Message, e);
            }
        }

        /// <remarks>Throws an exception if the column is null and it is read as a boolean</remarks>
        public char GetChar(string columnName)
        {
            try
            {
                var index = DataReader.GetOrdinal(columnName);
                return DataReader.IsDBNull(index) ? default(char) : DataReader.GetChar(index);
            }
            catch (Exception e)
            {
                if (e is InvalidCastException || e is IndexOutOfRangeException)
                    throw; 

                throw new InvalidCastException(e.Message, e);
            }
        }

        /// <remarks>Throws an exception if the column is null and it is read as a boolean</remarks>
        public long GetChars(string columnName, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            try
            {
                var index = DataReader.GetOrdinal(columnName);
                return DataReader.IsDBNull(index) ? 0 : DataReader.GetChars(index, fieldoffset, buffer, bufferoffset, length);
            }
            catch (Exception e)
            {
                if (e is InvalidCastException || e is IndexOutOfRangeException)
                    throw;

                throw new InvalidCastException(e.Message, e);
            }
        }

        public Guid GetGuid(string columnName)
        {
            try
            {
                var index = DataReader.GetOrdinal(columnName);
                return DataReader.IsDBNull(index) ? Guid.Empty : (Guid) DataReader.GetValue(index);
            }
            catch (Exception e)
            {
                if (e is InvalidCastException || e is IndexOutOfRangeException)
                    throw;

                throw new InvalidCastException(e.Message, e);
            }
        }

        /// <remarks>Throws an exception if the column is null and it is read as a boolean</remarks>
        public DateTime GetDateTime(string columnName)
        {
            try
            {
                var index = DataReader.GetOrdinal(columnName);
                return DataReader.IsDBNull(index) ? default(DateTime) : Convert.ToDateTime(DataReader.GetValue(index));
            }
            catch (Exception e)
            {
                if (e is InvalidCastException || e is IndexOutOfRangeException)
                    throw; 

                throw new InvalidCastException(e.Message, e);
            }
        }

        public DateTime? GetDateTimeNullable(string columnName)
        {
            try
            {
                var index = DataReader.GetOrdinal(columnName);
                return DataReader.IsDBNull(index) ? (DateTime?) null : DataReader.GetDateTime(index);
            }
            catch (Exception e)
            {
                if (e is InvalidCastException || e is IndexOutOfRangeException)
                    throw; 

                throw new InvalidCastException(e.Message, e);
            }
        }

        public decimal GetDecimal(string columnName)
        {
            return GetValue<decimal>(DataReader.GetDecimal, columnName);
        }

        public double GetDouble(string columnName)
        {
            return GetValue<double>(DataReader.GetDouble, columnName);
        }

        public float GetFloat(string columnName)
        {
            return GetValue<float>(DataReader.GetFloat, columnName);
        }

        public short GetShort(string columnName)
        {
            return GetValue<short>(DataReader.GetInt16, columnName);
        }

        public int GetInt(string columnName)
        {
            try
            {
                var index = DataReader.GetOrdinal(columnName);
                return DataReader.IsDBNull(index) ? 0 : Convert.ToInt32(DataReader.GetValue(index));
            }
            catch (Exception e)
            {
                if (e is InvalidCastException || e is IndexOutOfRangeException)
                    throw;

                throw new InvalidCastException(e.Message, e);
            }
        }

        public long GetLong(string columnName)
        {
            return GetValue<long>(DataReader.GetInt64, columnName);
        }

        public string GetString(string columnName)
        {
            return GetValue<string>(DataReader.GetString, columnName);
        }
        public T GetValue<T>(Func<int,T> getValue, string columnName)
        {
            try
            {
                var index = DataReader.GetOrdinal(columnName);
                return DataReader.IsDBNull(index) ? default(T) : getValue(index);
            }
            catch (Exception e)
            {
                Log.WriteError(_name, MethodBase.GetCurrentMethod().Name, e);
                if (e is InvalidCastException || e is IndexOutOfRangeException)
                    throw;

                throw new InvalidCastException(e.Message, e);
            }
        }
        public bool IsDbNull(string columnName)
        {
            try
            {
                var index = DataReader.GetOrdinal(columnName);
                return DataReader.IsDBNull(index);
            }
            catch (Exception e)
            {
                Log.WriteError(_name, MethodBase.GetCurrentMethod().Name, e);
                if (e is InvalidCastException || e is IndexOutOfRangeException)
                    throw;

                throw new InvalidCastException(e.Message, e);
            }
        }
        #endregion

        private string GetParameterName(string name)
        {
            if (Type.Equals("mssql") && !name.StartsWith("@"))
                return "@" + name;

            // else we are MySql:
            return "p_" + name;
        }
    }
}
