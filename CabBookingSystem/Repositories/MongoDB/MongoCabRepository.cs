using MongoDB.Driver;
using CabBookingSystem.Models;
using CabBookingSystem.Repositories;
using Microsoft.Extensions.Configuration;
using System.Linq.Expressions;
using Microsoft.Extensions.Options;

namespace CabBookingSystem.Repositories.MongoDB
{
    public class MongoCabRepository : ICabRepository
    {
        private readonly IMongoCollection<Cab> _cabs;

        public MongoCabRepository(IOptions<MongoDBSettings> settings)
        {
            if (string.IsNullOrEmpty(settings.Value.ConnectionString))
            {
                throw new ArgumentException("MongoDB connection string is not configured");
            }

            var client = new MongoClient(settings.Value.ConnectionString);
            var database = client.GetDatabase(settings.Value.DatabaseName);
            _cabs = database.GetCollection<Cab>("Cabs");

            // Seed initial cabs if collection is empty
            SeedInitialCabs();
        }

        private async void SeedInitialCabs()
        {
            var count = await _cabs.CountDocumentsAsync(_ => true);
            if (count == 0)
            {
                var initialCabs = new List<Cab>
                {
                    new Cab { Id = Guid.NewGuid().ToString(), Type = CabType.Mini, IsAvailable = true },
                    new Cab { Id = Guid.NewGuid().ToString(), Type = CabType.Mini, IsAvailable = true },
                    new Cab { Id = Guid.NewGuid().ToString(), Type = CabType.Mini, IsAvailable = true },
                    new Cab { Id = Guid.NewGuid().ToString(), Type = CabType.Sedan, IsAvailable = true },
                    new Cab { Id = Guid.NewGuid().ToString(), Type = CabType.Sedan, IsAvailable = true },
                    new Cab { Id = Guid.NewGuid().ToString(), Type = CabType.Sedan, IsAvailable = true },
                    new Cab { Id = Guid.NewGuid().ToString(), Type = CabType.SUV, IsAvailable = true },
                    new Cab { Id = Guid.NewGuid().ToString(), Type = CabType.SUV, IsAvailable = true },
                    new Cab { Id = Guid.NewGuid().ToString(), Type = CabType.Luxury, IsAvailable = true },
                    new Cab { Id = Guid.NewGuid().ToString(), Type = CabType.Luxury, IsAvailable = true }
                };
                await _cabs.InsertManyAsync(initialCabs);
            }
        }

        public async Task<Cab> GetByIdAsync(string id)
        {
            return await _cabs.Find(c => c.Id == id).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Cab>> GetAllAsync()
        {
            return await _cabs.Find(_ => true).ToListAsync();
        }

        public async Task<IEnumerable<Cab>> FindAsync(Expression<Func<Cab, bool>> predicate)
        {
            return await _cabs.Find(predicate).ToListAsync();
        }

        public async Task AddAsync(Cab entity)
        {
            await _cabs.InsertOneAsync(entity);
        }

        public async Task UpdateAsync(Cab entity)
        {
            await _cabs.ReplaceOneAsync(c => c.Id == entity.Id, entity);
        }

        public async Task DeleteAsync(string id)
        {
            await _cabs.DeleteOneAsync(c => c.Id == id);
        }

        public async Task UpdateCabAvailabilityAsync(string cabId, bool isAvailable)
        {
            var update = Builders<Cab>.Update.Set(c => c.IsAvailable, isAvailable);
            await _cabs.UpdateOneAsync(c => c.Id == cabId, update);
        }

        public async Task<IEnumerable<Cab>> GetAvailableCabsAsync()
        {
            return await _cabs.Find(c => c.IsAvailable).ToListAsync();
        }

        public async Task<IEnumerable<Cab>> GetCabsByTypeAsync(string cabType)
        {
            if (Enum.TryParse<CabType>(cabType, out var cabTypeEnum))
            {
                return await _cabs.Find(c => c.Type == cabTypeEnum && c.IsAvailable).ToListAsync();
            }
            return new List<Cab>();
        }

        public async Task<Cab> GetAvailableCabOrCreateNewAsync(string cabType)
        {
            if (!Enum.TryParse<CabType>(cabType, out var cabTypeEnum))
            {
                return null;
            }

            // Find available cab
            var availableCab = await _cabs.Find(c => c.Type == cabTypeEnum && c.IsAvailable).FirstOrDefaultAsync();

            if (availableCab != null)
            {
                return availableCab;
            }

            // Create new cab if none available
            var newCab = new Cab
            {
                Id = Guid.NewGuid().ToString(),
                Type = cabTypeEnum,
                IsAvailable = true
            };

            await AddAsync(newCab);
            return newCab;
        }
    }
}