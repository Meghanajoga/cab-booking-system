using MongoDB.Driver;
using CabBookingSystem.Models;
using CabBookingSystem.Repositories;
using Microsoft.Extensions.Configuration;
using System.Linq.Expressions;
using Microsoft.Extensions.Options;

namespace CabBookingSystem.Repositories.MongoDB
{
    public class MongoUserRepository : IUserRepository
    {
        private readonly IMongoCollection<ApplicationUser> _users;

        public MongoUserRepository(IOptions<MongoDBSettings> settings)
        {
            if (string.IsNullOrEmpty(settings.Value.ConnectionString))
            {
                throw new ArgumentException("MongoDB connection string is not configured");
            }

            var client = new MongoClient(settings.Value.ConnectionString);
            var database = client.GetDatabase(settings.Value.DatabaseName);
            _users = database.GetCollection<ApplicationUser>("Users");
        }

        public async Task<ApplicationUser> GetByIdAsync(string id)
        {
            return await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<ApplicationUser>> GetAllAsync()
        {
            return await _users.Find(_ => true).ToListAsync();
        }

        public async Task<IEnumerable<ApplicationUser>> FindAsync(Expression<Func<ApplicationUser, bool>> predicate)
        {
            return await _users.Find(predicate).ToListAsync();
        }

        public async Task AddAsync(ApplicationUser entity)
        {
            await _users.InsertOneAsync(entity);
        }

        public async Task UpdateAsync(ApplicationUser entity)
        {
            await _users.ReplaceOneAsync(u => u.Id == entity.Id, entity);
        }

        public async Task DeleteAsync(string id)
        {
            await _users.DeleteOneAsync(u => u.Id == id);
        }

        public async Task<ApplicationUser> GetUserByEmailAsync(string email)
        {
            return await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
        }

        public async Task<ApplicationUser> GetUserByIdAsync(string id)
        {
            return await GetByIdAsync(id);
        }

        public async Task<bool> UserExistsAsync(string email)
        {
            var user = await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
            return user != null;
        }

        public async Task<ApplicationUser> GetByEmailAsync(string email)
        {
            return await GetUserByEmailAsync(email);
        }

        public async Task<bool> ValidateUserAsync(string email, string password)
        {
            var user = await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
            return user != null && user.PasswordHash == password;
        }
    }
}