using Levio.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Levio.Domain.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByEmployeeCodeAsync(string employeeCode);
        Task<User?> GetUserWithRolesAsync(long userId);
        Task<bool> ExistsByEmailAsync(string email);
    }
}
