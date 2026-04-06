using System;
using System.Collections.Generic;
using System.Text;

namespace PropertyPortal.Application.DTOs.Residents
{
    public class ResidentListResponse
    {
        public List<ResidentResponseDto> Residents { get; set; } = new();
        public decimal TotalMonthlyRent { get; set; }
        public int TotalCount { get; set; }
    }

}
