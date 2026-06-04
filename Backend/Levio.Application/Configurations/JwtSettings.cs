using System;
using System.Collections.Generic;
using System.Text;

namespace Levio.Application.Configurations
{
    public class JwtSettings
    {
        public string Issuer { get; set; } = string.Empty;
        public string audience { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public int AccessTokenMinutes { get; set; }

        public int RefreshTokenDays{ get; set; }
    }
}
