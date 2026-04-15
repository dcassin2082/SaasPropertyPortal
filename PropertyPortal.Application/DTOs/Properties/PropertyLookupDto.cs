namespace PropertyPortal.Application.DTOs.Properties
{
    public class PropertyLookupDto
    {
        public Guid? Id { get; set; }
        public string Name { get; set; } = null!;
        public int ResidentCount { get; set; }
        public int ApplicantCount { get; set; }
    }
}
