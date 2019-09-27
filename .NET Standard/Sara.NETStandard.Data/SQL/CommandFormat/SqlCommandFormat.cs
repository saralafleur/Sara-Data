using System;
using System.Collections;

namespace Sara.NETStandard.Data.SQL.CommandFormat
{
    public class SqlCommandFormat : IDatabaseCommandFormat
    {
        public ExternalDatabaseType DatabaseType => _type;
        private readonly ExternalDatabaseType _type;

        #region Constructors
        public SqlCommandFormat(string type)
        {
            try
            {
                _type = (ExternalDatabaseType)Enum.Parse(typeof(ExternalDatabaseType), type, true);
            }
            catch
            {
                _type = ExternalDatabaseType.Unknown;
            }
        }
        public SqlCommandFormat(ExternalDatabaseType type)
        {
            _type = type;
        }
        #endregion


        #region SQL Format Methods
        public string CurrentDateTime()
        {
            var method = "";
            switch (_type)
            {
                case ExternalDatabaseType.MySql:
                    method = "NOW()";
                    break;
                case ExternalDatabaseType.MsSql:
                    method = "GETDATE()";
                    break;
            }
            return method;
        }

        public string CurrentDate()
        {
            var method = "";
            switch (_type)
            {
                case ExternalDatabaseType.MySql:
                    method = "CURRENT_DATE";
                    break;
                case ExternalDatabaseType.MsSql:
                    method = "GETDATE()";
                    break;
            }
            return method;
        }

        public string DateTimeToString(DateTime time)
        {
            var argArray = new ArrayList();

            const string dateFormat = "{0}-{1}-{2} {3}:{4}:{5}";
            argArray.Add(time.Year);
            argArray.Add(time.Month);
            argArray.Add(time.Day);
            argArray.Add(time.Hour);
            argArray.Add(time.Minute);
            argArray.Add(time.Second);

            var args = (object[])argArray.ToArray(typeof(object));
            return ToDbString(string.Format(dateFormat, args));
        }

        public string DateTimeToDateOnlyString(DateTime dt)
        {
            var argArray = new ArrayList();

            const string dateFormat = "{0}-{1}-{2}";
            argArray.Add(dt.Year);
            argArray.Add(dt.Month);
            argArray.Add(dt.Day);

            var args = (object[])argArray.ToArray(typeof(object));
            return ToDbString(string.Format(dateFormat, args));
        }

        public string ToDbString(string originalString)
        {
            if (originalString == null)
                originalString = "";
            originalString = originalString.Replace(@"\", @"\\");
            originalString = originalString.Replace("'", "''");
            return "'" + originalString + "'";
        }

        public string ToDbParameter(string parameterName)
        {
            switch (_type)
            {
                case ExternalDatabaseType.MySql:
                    return "?" + parameterName;
                case ExternalDatabaseType.MsSql:
                    return "@" + parameterName;
            }
            throw new Exception("Unknown database '" + parameterName + "'");
        }

        public string ToDbString(DateTime date)
        {
            return "'" + date.ToString("yyyy'-'MM'-'dd HH':'mm':'ss") + "'";
        }

        public string ToDbString(TimeSpan timeSpan)
        {
            timeSpan = timeSpan + new TimeSpan(0, 0, 0, 0, 500); // will round to nearest second
            return "'" + string.Format("{0}:{1}:{2}", new object[] { timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds }) + "'";
        }

        #endregion
    }
}
