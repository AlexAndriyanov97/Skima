using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace Models.Tokens.Repository
{
    public class TokenRepository
    {
        private readonly IMongoCollection<Token> tokens;

        public TokenRepository(Configuration config)
        {
            var client = new MongoClient(config.GetConnectionString("SkimaDb"));
            var database = client.GetDatabase("SkimaDb");
            tokens = database.GetCollection<Token>("Tokens");
        }

        public Task<List<Token>> GetAsync()
        {
            var search = tokens.Find(token => true);
            var result = search.ToList();

            return Task.FromResult(result);
        }

        public Task SaveRefreshTokenAsync(string userId, string refreshToken)
        {
            var token = new Token
            {
                UserId = userId,
                RefreshToken = refreshToken
            };
            tokens.InsertOne(token);

            return Task.CompletedTask;
        }

        public Task<String> GetRefreshTokenAsync(string userId)
        {
            var search = tokens.Find(token => token.UserId == userId);
            var result = search.FirstOrDefault();

            return Task.FromResult(result.RefreshToken);
        }

        public Task RemoveRefreshTokenAsync(string userId, string refreshToken)
        {
            tokens.DeleteOne(token => token.UserId == userId && token.RefreshToken == refreshToken);

            return Task.CompletedTask;
        }
        
        public Task RemoveRefreshTokenAsync(string refreshToken)
        {
            tokens.DeleteOne(token => token.RefreshToken == refreshToken);

            return Task.CompletedTask;
        }
    }
}