using System;
using System.Collections.Generic;
using System.Text;

namespace PropertyPortal.Application.DTOs.Auth
{
    public class RegisterTenantDto
    {
        // Tenant Info
        public string CompanyName { get; set; } = null!;

        // Admin User Info
        public string AdminEmail { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}
