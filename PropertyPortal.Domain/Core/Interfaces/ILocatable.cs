
using PropertyPortal.Domain.Entities;

namespace PropertyPortal.Domain.Core.Interfaces
{
    /// <summary>
    /// Marker interface which allows to add Address to existing entities without adding it to the BaseEntity class
    ///     which would force all entities to have an Address property.  Now, only entities that actually need an address column
    ///     can implement this interface
    /// </summary>
    public interface ILocatable
    {
        string Name { get; set; }
        string? Description { get; set; }
        Address Address { get; set; }

    }

}
