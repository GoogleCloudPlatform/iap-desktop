using Google.Solutions.Compute.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Services
{
    public interface IAuthorizationService
    {
        IAuthorization Authorization { get; }
    }
}
