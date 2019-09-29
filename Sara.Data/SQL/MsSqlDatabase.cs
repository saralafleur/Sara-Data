using System;
using System.Globalization;
using System.Reflection;
using Sara.Data.SQL.CommandFormat;
using Sara.NETStandard.Logging;

namespace Sara.Data.SQL
{
    public class MsSqlDatabase : IExternalDatabase
    {
        #region Class Variables

        private readonly IDatabaseCommandFormat _commandFormat;
        private int _connectTimeout = -1;
        private int _commandTimeout = -1;
        private readonly string _className;

        #endregion

        #region Constructor / Destructor

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="connectionString">
        /// The connection string. 
        /// It should be of the form "Server=127.0.0.1;Uid=root;Pwd=12345;Database=test;"
        /// </param>
        public MsSqlDatabase(string connectionString)
        {
            ConnectionString = connectionString;
            _commandFormat = new SqlCommandFormat(ExternalDatabaseType.MsSql);
            _className = GetType().Name;
        }

        private MsSqlDatabase(string connectionString, IDatabaseCommandFormat format)
        {
            ConnectionString = connectionString;
            _commandFormat = format;
            _className = GetType().Name;
        }

        public static MsSqlDatabase Create(string connectionString)
        {
            return connectionString == null ? null : new MsSqlDatabase(connectionString);
        }

        public static MsSqlDatabase Create(string host, string user, string password)
        {
            try
            {
                password = "'" + password.Replace("'", "''") + "'";
                // for some reason, if the password contains a \, it's not being formatted correctly. above fixes it
                IDatabaseCommandFormat formatter = new SqlCommandFormat(ExternalDatabaseType.MsSql);
                //return new MySqlDatabase("Server=" + host + ";Uid=" + user + ";Pwd=" + format.ToDbString(password) + ";", format);
                return new MsSqlDatabase("Server=" + host + ";Uid=" + formatter.ToDbString(user) + ";Pwd=" + password + ";", formatter);
            }
            // ReSharper disable EmptyGeneralCatchClause
            catch
            // ReSharper restore EmptyGeneralCatchClause
            {
            }
            return null;
        }

        public static MsSqlDatabase Create(string host, string user, string password, string database)
        {
            try
            {
                password = "'" + password.Replace("'", "''") + "'";
                // for some reason, if the password contains a \, it's not being formatted correctly. above fixes it

                IDatabaseCommandFormat formatter = new SqlCommandFormat(ExternalDatabaseType.MsSql);
                return new MsSqlDatabase("Server=" + host + ";Uid=" + formatter.ToDbString(user) + ";Pwd=" + password + ";Database=" + database + ";", formatter);
            }
            // ReSharper disable EmptyGeneralCatchClause
            catch
            // ReSharper restore EmptyGeneralCatchClause
            {
            }
            return null;
        }

        #endregion

        #region IExternalDatabase Members

        public IDatabaseConnection CreateConnection()
        {
            try
            {
                IDatabaseConnection theConnection = new MsSqlDatabaseConnection(ConnectionString);
                var commandTimeout = _commandTimeout;
                if (commandTimeout >= 0)
                    theConnection.CommandTimeout = commandTimeout;
                if (theConnection.Open())
                {
                    return theConnection;
                }
            }
            catch (Exception e)
            {
                Log.WriteError("Could not create SQL Server connection",_className, MethodBase.GetCurrentMethod().Name, e);
            }

            Log.Write("Error creating SQL Server connection",_className, MethodBase.GetCurrentMethod().Name, LogEntryType.Error);
            return null;

        }

        IDatabaseConnection IExternalDatabase.CreateConnection()
        {
            try
            {
                IDatabaseConnection theConnection = new MsSqlDatabaseConnection(ConnectionString);
                var commandTimeout = _commandTimeout;
                if (commandTimeout >= 0)
                    theConnection.CommandTimeout = commandTimeout;
                if (theConnection.Open())
                {
                    return theConnection;
                }
            }
            catch (Exception e)
            {
                Log.WriteError("Could not create SQL Server connection",_className, MethodBase.GetCurrentMethod().Name, e);
            }

            Log.Write("Error creating SQL Server connection",_className, MethodBase.GetCurrentMethod().Name, LogEntryType.Error);
            return null;
        }

        public string ConnectionString { get; private set; }

        public ExternalDatabaseType Type => ExternalDatabaseType.MsSql;

        public IDatabaseCommandFormat CommandFormat => _commandFormat;

        public int ConnectionTimeout
        {
            get => _connectTimeout;
            set
            {
                var str = ConnectionString;
                var lower = str.ToLower();

                var offset = lower.IndexOf("connection timeout", StringComparison.Ordinal);
                if (offset >= 0)
                {
                    var end = lower.IndexOf(';', offset);
                    str = end >= 0 ? str.Remove(offset, end - offset + 1) : str.Remove(offset);
                }

                if (!str.EndsWith(";"))
                {
                    str += ';';
                }

                str += "Connection Timeout=" + value.ToString(CultureInfo.InvariantCulture) + ";";
                _connectTimeout = value;
                ConnectionString = str;
            }
        }

        public int CommandTimeout
        {
            get => _commandTimeout;
            set => _commandTimeout = value;
        }

        #endregion
    }
}
