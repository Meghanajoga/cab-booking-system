using CabBookingSystem.Models;

namespace CabBookingSystem.Repositories
{
    public interface IUserRepository : IRepository<ApplicationUser>
    {
        Task<ApplicationUser> GetUserByEmailAsync(string email);
        Task<ApplicationUser> GetUserByIdAsync(string id);
        Task<bool> UserExistsAsync(string email);
        Task<ApplicationUser> GetByEmailAsync(string email);
        Task<bool> ValidateUserAsync(string email, string password);
    }
}