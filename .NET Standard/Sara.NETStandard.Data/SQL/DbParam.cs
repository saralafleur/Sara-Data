namespace Sara.NETStandard.Data.SQL
{
    public class DbParam
    {
        public string Name { get; set; }
        public object Value { get; set; }

        public DbParam(string name, object value)
        {
            Name = name;
            Value = value;
        }
        public override string ToString()
        {
            return $"{{{Name}: {Value}}}";
        }
    }
}
