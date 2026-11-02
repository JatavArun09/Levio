using Levio.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Levio.Application.Interfaces
{
    public interface IJwtTokenService
    {
        string GenerateAccessToken(User user);

        string GenerateRefreshToken();
    }
}
