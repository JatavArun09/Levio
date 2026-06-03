using Levio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Levio.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Register DbContext
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
            // Register repositories
            //services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            // Register other services (e.g., authentication, authorization, etc.)
            // services.AddScoped<IAuthService, AuthService>();
            return services;
        }
    }
}
