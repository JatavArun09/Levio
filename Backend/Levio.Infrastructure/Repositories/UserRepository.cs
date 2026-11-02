using Levio.Domain.Entities;
using Levio.Domain.Repositories;
using Levio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;


namespace Levio.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext Context;

        public UserRepository(ApplicationDbContext context) { 
        Context = context;
        }

        
        public async Task<User?> GetByEmailAsync(string email)
        {
            return await Context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByEmployeeCodeAsync(string employeeCode)
        {
            return await Context.Users.FirstOrDefaultAsync(u => u.EmployeeCode == employeeCode);
        }

        public async Task<User?> GetUserWithRolesAsync(long userId)
        {
            return await Context.Users.Include(x => x.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            return await Context.Users.AnyAsync(u => u.Email == email);
        }
    }
}
