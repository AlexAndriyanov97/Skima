using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventGenerator.Models;
using Models;
using Models.Activity.Repository;
using MongoDB.Driver;

namespace EventGenerator.Repository
{
    public class ActivityFinishedRepository
    {
        private readonly IMongoCollection<ActivityFinishedInfo> activityFinishedInfo;

        public ActivityFinishedRepository(Configuration config)
        {
            var client = new MongoClient(config.GetConnectionString("SkimaDb"));
            var database = client.GetDatabase("SkimaDb");
            activityFinishedInfo = database.GetCollection<ActivityFinishedInfo>("ActivityFinishedInfo");
        }
        
        
        public Task<List<ActivityFinishedInfo>> GetAllActivityEventInfo()
        {
            var search = activityFinishedInfo.Find(info => true);
            var result = search.ToList();
            return Task.FromResult(result);
        }
        
        public Task<ActivityFinishedInfo> CreateActivityFinishedInfoAsync(ActivityFinishedInfo newActivityFinishedInfo)
        {
            if (newActivityFinishedInfo == null)
            {
                throw new ArgumentNullException();
            }
            
            activityFinishedInfo.InsertOne(newActivityFinishedInfo);
            return Task.FromResult(newActivityFinishedInfo);
        }
        
        public Task UpdateAsync(string id, ActivityFinishedInfo activityFinishedInfo)
        {
            this.activityFinishedInfo.ReplaceOne(user => user.Id == id, activityFinishedInfo);

            return Task.CompletedTask;
        }
    }
}