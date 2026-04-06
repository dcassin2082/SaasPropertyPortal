using System;
using System.Collections.Generic;
using System.Text;

namespace PropertyPortal.Application.DTOs.Residents
{
    public class BulkRenewalRequest
    {
        public List<Guid> ResidentIds { get; set; } = new();
        public decimal? NewRentAmount { get; set; }
        public double? PercentIncrease { get; set; }
        public DateTime NewEndDate { get; set; }
    }
}
