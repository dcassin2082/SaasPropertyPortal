namespace PropertyPortal.Application.DTOs.Residents
{
    //internal class UnassignedResidentDto
    //{
    //    //Id = r.Id,
    //    //        FirstName = r.FirstName,
    //    //        LastName = r.LastName,
    //    //        Email = r.Email,
    //    //        Status = r.Status ?? "Pending"
    //}

    public record UnassignedResidentDto
    {
        public Guid Id { get; init; }
        public string FirstName { get; init; } = null!;
        public string LastName { get; init; } = null!;
        public string? Email { get; init; } 
        public string? Status { get; init; }
    }
}
