using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyPortal.Domain.Entities
{
    /// <summary>
    /// Add the [ComplexType] attribute to your Address record
    /// This tells EF Core 8: "This is not a table; just map its properties to the columns of whatever class uses it.
    /// </summary>
    /// <param name="Street"></param>
    /// <param name="UnitNumber"></param>
    /// <param name="City"></param>
    /// <param name="State"></param>
    /// <param name="ZipCode"></param>
    [ComplexType]
    public record Address
    (
        string Street,
        string? UnitNumber,
        string City,
        string State,
        string ZipCode
    );

}
