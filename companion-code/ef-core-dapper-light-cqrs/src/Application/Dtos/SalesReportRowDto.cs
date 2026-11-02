namespace OrderLightCqrs.Application.Dtos;

public record SalesReportRowDto(string Category, int Year, int Month, decimal Revenue, int OrderCount);
