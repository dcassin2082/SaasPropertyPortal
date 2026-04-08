namespace PropertyPortal.Application.DTOs.Properties
{
    public record PropertyStatsDto(
        Guid PropertyId,
        decimal TotalPotentialRevenue,
        decimal ActualMonthlyRevenue,
        double OccupancyRate,
        int TotalUnits,
        int OccupiedUnits,
        decimal AverageRent
    );
}
