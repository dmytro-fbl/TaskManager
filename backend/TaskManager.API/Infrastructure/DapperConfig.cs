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
            SqlMapper.AddTypeHandler(new GuidListHandler());
        }

        private sealed class GuidListHandler : SqlMapper.TypeHandler<List<Guid>>
        {
            public override void SetValue(IDbDataParameter parameter, List<Guid>? value)
            {
                parameter.Value = value?.ToArray() ?? (object)DBNull.Value;
            }

            public override List<Guid> Parse(object value) => value switch
            {
                Guid[] array => array.ToList(),
                IEnumerable<Guid> seq => seq.ToList(),
                _ => new List<Guid>()
            };
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
                DateOnly d => new DateTimeOffset(d.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)),
                _ => throw new InvalidCastException($"Cannot convert {value.GetType()} to DateTimeOffset.")
            };
        }
    }
}
