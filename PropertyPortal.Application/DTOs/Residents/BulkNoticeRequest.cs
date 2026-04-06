using System;
using System.Collections.Generic;
using System.Text;

namespace PropertyPortal.Application.DTOs.Residents
{
    public class BulkNoticeRequest
    {
        public List<Guid> ResidentIds { get; set; } = null!;
        public string MessageTemplate { get; set; } = null!;
    }
}
