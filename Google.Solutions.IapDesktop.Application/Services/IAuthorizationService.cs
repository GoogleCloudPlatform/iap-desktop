using Google.Solutions.Compute.Auth;

namespace Google.Solutions.IapDesktop.Application.Services
{
    public interface IAuthorizationService
    {
        IAuthorization Authorization { get; }
    }
}
