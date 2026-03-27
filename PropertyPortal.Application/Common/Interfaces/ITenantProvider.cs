namespace PropertyPortal.Application.Common.Interfaces
{
    public interface ITenantProvider
    {
        Guid GetTenantId(); 
        Guid? GetUserId();
    }
}
