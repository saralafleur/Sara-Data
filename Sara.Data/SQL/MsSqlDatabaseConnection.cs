using System.Data;
using System.Data.SqlClient;

namespace Sara.Data.SQL
{
    class MsSqlDatabaseConnection : DatabaseConnectionBase, IDatabaseConnection
    {
        #region Constructor / Destructor

        public MsSqlDatabaseConnection(string connectionString)
        {
            InternalConnection = new SqlConnection { ConnectionString = connectionString };
            Type = "mssql";
        }

        #endregion

        public override bool HasRows => ((SqlDataReader)DataReader).HasRows;

        public void ExecuteStoredProcedure(string procedureName, params DbParam[] parameters)
        {
            throw new System.NotImplementedException();
        }

        public int ExecuteNonQueryStoredProcedure(string procedureName, params DbParam[] parameters)
        {
            throw new System.NotImplementedException();
        }

        public DataTable GetTable(string sqlCommandString)
        {
            if (DataReader != null)
            {
                DataReader.Close();
                DataReader = null;
            }

            var adapter = new SqlDataAdapter(sqlCommandString, (SqlConnection)InternalConnection);
            var ret = new DataTable();
            adapter.Fill(ret);
            return ret;
        }

        public int GetInsertIdentifier()
        {
            var ret = 0;
            const string sql = "SELECT @@IDENTITY AS id";
            try
            {
                ExecuteQuery(sql);
                if (HasRows && NextRow())
                {
                    ret = GetInt("id");
                }
            }
            catch
            {
            }
            return ret;
        }

        public bool IsDBNull(string columnName)
        {
            throw new System.NotImplementedException();
        }

        public IDbCommand CreateCommand(string commandString)
        {
            var command = new SqlCommand(commandString, InternalConnection as SqlConnection);
            var commandTimeout = CommandTimeout;
            if (commandTimeout >= 0)
                command.CommandTimeout = commandTimeout;
            return command;
        }
    }
}
