using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Models;
using MongoDB.Driver;

namespace EventGenerator.Repository
{
    public class RegistrationEventInfoRepository
    {
        private readonly IMongoCollection<RegistrationEventInfo> registrationEventInfo;

        public RegistrationEventInfoRepository(Configuration config)
        {
            var client = new MongoClient(config.GetConnectionString("SkimaDb"));
            var database = client.GetDatabase("SkimaDb");
            registrationEventInfo = database.GetCollection<RegistrationEventInfo>("RegistrationEventInfo");
        }

        public Task<List<RegistrationEventInfo>> GetAllRegistrationEventInfo()
        {
            var search = registrationEventInfo.Find(info => true);
            var result = search.ToList();
            return Task.FromResult(result);
        }

        public Task<RegistrationEventInfo> CreateRegistrationEventInfoAsync(RegistrationEventInfo newRegistrationEventInfo)
        {
            if (newRegistrationEventInfo == null)
            {
                throw new ArgumentNullException();
            }
            
            registrationEventInfo.InsertOne(newRegistrationEventInfo);
            return Task.FromResult(newRegistrationEventInfo);
        }
        
        public Task UpdateAsync(string id, RegistrationEventInfo registrationEventInfo)
        {
            this.registrationEventInfo.ReplaceOne(info => info.Id == id, registrationEventInfo);

            return Task.CompletedTask;
        }
    }
} 