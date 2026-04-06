using System.ComponentModel.DataAnnotations;

namespace PropertyPortal.Domain.Enums
{
    public enum PropertyType
    {
        [StringLength(50)]
        Apartment,

        [StringLength(50)]
        [Display(Name = "Single Family Home")]
        SingleFamily,

        [StringLength(50)]
        [Display(Name = "Multi-Family Home")]
        MultiFamily
    }
}
