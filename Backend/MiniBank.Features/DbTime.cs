namespace MiniBank.Features;

/// <summary>
/// Converts PostgreSQL timestamptz columns to <see cref="DateTimeOffset"/>.
/// Npgsql returns timestamptz as <see cref="DateTime"/> (UTC), so Dapper cannot
/// materialize row records with DateTimeOffset members directly — every Dapper
/// row record must declare timestamp columns as DateTime and convert here.
/// </summary>
internal static class DbTime
{
    public static DateTimeOffset Utc(DateTime value)
        => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    public static DateTimeOffset? Utc(DateTime? value)
        => value is null ? null : new(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc));
}
