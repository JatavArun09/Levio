using Levio.Application.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Levio.Infrastructure.Authentication
{
    public class PasswordHasher :IPasswordHasher
    {
        private readonly PasswordHasher<object> _passwordHasher;

        public PasswordHasher()
        {
            _passwordHasher = new PasswordHasher<object>();
        }

        public string Hash(string password)
        {
            return _passwordHasher.HashPassword(new object(), password);
        }

        public bool Verify(string password, string passwordHash)
        {
            var result = _passwordHasher.VerifyHashedPassword(new object(), passwordHash, password);
            return result != PasswordVerificationResult.Failed;
        }
    }
}
