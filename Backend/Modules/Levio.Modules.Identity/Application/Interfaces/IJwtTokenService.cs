using Levio.Modules.Identity.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Levio.Modules.Identity.Application.Interfaces
{
    public interface IJwtTokenService
    {
        string GenerateAccessToken(User user);

        string GenerateRefreshToken();
    }
}
