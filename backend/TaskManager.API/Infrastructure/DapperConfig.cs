using System.Data;
using Dapper;

namespace TaskManager.API.Infrastructure
{
    public static class DapperConfig
    {
        public static void Configure()
        {
            DefaultTypeMap.MatchNamesWithUnderscores = true;
            SqlMapper.AddTypeHandler(new UtcDateTimeOffsetHandler());
        }

        private sealed class UtcDateTimeOffsetHandler : SqlMapper.TypeHandler<DateTimeOffset>
        {
            public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
            {
                parameter.Value = value.ToUniversalTime();
            }

            public override DateTimeOffset Parse(object value) => value switch
            {
                DateTimeOffset dto => dto,
                DateTime dt => new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc)),
                _ => throw new InvalidCastException($"Cannot convert {value.GetType()} to DateTimeOffset.")
            };
        }
    }
}
