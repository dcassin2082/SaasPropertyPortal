namespace PropertyPortal.Application.Extensions
{
    public static class DateExtensions
    {
        public static DateOnly ToDateOnly(this DateTime dateTime)
            => DateOnly.FromDateTime(dateTime);

        public static DateOnly Today(this DateOnly _)
            => DateOnly.FromDateTime(DateTime.UtcNow);
    }
}
