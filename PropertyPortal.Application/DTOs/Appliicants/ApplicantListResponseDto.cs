using System;
using System.Collections.Generic;
using System.Text;

namespace PropertyPortal.Application.DTOs.Appliicants
{
    public class ApplicantListResponseDto
    {
        public IEnumerable<ApplicantResponseDto> Applicants { get; set; }
        public int TotalCount { get; set; }
        public int ApprovedApplicants { get; set; }
        public int AverageCreditScore { get; set; }
    }
}
