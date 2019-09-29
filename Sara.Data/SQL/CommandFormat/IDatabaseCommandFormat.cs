using System;

namespace Sara.Data.SQL.CommandFormat
{
    public interface IDatabaseCommandFormat
    {
        ExternalDatabaseType DatabaseType { get; }

        string CurrentDateTime();
        string CurrentDate();
        string DateTimeToString(DateTime time);
        string DateTimeToDateOnlyString(DateTime dt);
        string ToDbString(string originalString);
        string ToDbParameter(string parameterName);
        string ToDbString(DateTime date);
        string ToDbString(TimeSpan timeSpan);
    }
}
