using Microsoft.Extensions.Options;
using Mongo2Go;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Linq;
using Xunit;

namespace ElmahCore.MongoDB.Tests
{
    public class MongoDBErrorLogTests
    {
        const string DbName = "IntegrationTest";
        private readonly MongoDbRunner _runner;
        private readonly IMongoClient _client;
        private readonly MongoDBErrorLog _logEntry;
        private readonly IMongoCollection<MongoDBLogEntry> _collection;

        public MongoDBErrorLogTests()
        {
            _runner = MongoDbRunner.Start();
            _client = new MongoClient(_runner.ConnectionString);
            _logEntry = new MongoDBErrorLog(_client, Options.Create(new ElmahOptions { DatabaseName = DbName, CollectionName = "ShouldWrite" }));
            var database = _client.GetDatabase(DbName);
            _collection = database.GetCollection<MongoDBLogEntry>(_logEntry.CollectionName);
        }
        [Fact]
        public async void It_should_create_uncapped_collection()
        {
            var logEntry = new MongoDBErrorLog(_client, Options.Create(new ElmahOptions { DatabaseName = DbName, CollectionName = "DefaultCollection" }));
            var database = _client.GetDatabase(DbName);

            var collectionNames = database.ListCollectionNames().ToList();
            Assert.Contains("DefaultCollection", collectionNames);
            var collectionStats = await database.RunCommandAsync(
                new BsonDocumentCommand<BsonDocument>(new BsonDocument { { "collStats", "DefaultCollection" } }));
            Assert.False(collectionStats["capped"].AsBoolean);
            _runner.Dispose();
        }
        [Fact]
        public async void It_should_create_capped_collection()
        {
            var logEntry = new MongoDBErrorLog(_client, Options.Create(new ElmahOptions { DatabaseName = DbName, CollectionName = "CappedDefaultCollection", ShouldCollectionBeCapped = true }));
            var database = _client.GetDatabase(DbName);

            var collectionNames = database.ListCollectionNames().ToList();
            Assert.Contains("CappedDefaultCollection", collectionNames);
            var collectionStats = await database.RunCommandAsync(
                new BsonDocumentCommand<BsonDocument>(new BsonDocument { { "collStats", "CappedDefaultCollection" } }));
            Assert.True(collectionStats["capped"].AsBoolean);
            // add small extra to cover mongo internal overhead
            Assert.False(collectionStats["maxSize"].AsInt32 < 50000);
            Assert.True(collectionStats["maxSize"].AsInt32 < 50000 + 1500);
            _runner.Dispose();
        }
        [Fact]
        public async void It_should_create_capped_collection_with_custom_max_size()
        {
            var logEntry = new MongoDBErrorLog(_client,
                Options.Create(new ElmahOptions { DatabaseName = DbName, CollectionName = "CappedCustomSizeCollection", ShouldCollectionBeCapped = true, CapMbSize = 5000 }));
            var database = _client.GetDatabase(DbName);

            var collectionNames = database.ListCollectionNames().ToList();
            Assert.Contains("CappedCustomSizeCollection", collectionNames);
            var collectionStats = await database.RunCommandAsync(
                new BsonDocumentCommand<BsonDocument>(new BsonDocument { { "collStats", "CappedCustomSizeCollection" } }));
            Assert.True(collectionStats["capped"].AsBoolean);
            // add small extra to cover mongo internal overhead
            Assert.False(collectionStats["maxSize"].AsInt32 < 5000);
            Assert.True(collectionStats["maxSize"].AsInt32 < 5000  + 1500);
            _runner.Dispose();
        }
        [Fact]
        public async void It_should_log_error_to_db()
        {
            var err = _logEntry.Log(new Error(new ArgumentNullException()));

            Assert.Equal(1, await _collection.CountDocumentsAsync(FilterDefinition<MongoDBLogEntry>.Empty));
            var firstAndOnlyItem = await _collection.FindAsync(FilterDefinition<MongoDBLogEntry>.Empty);
            Assert.Equal(err, firstAndOnlyItem.FirstOrDefaultAsync().Result.ErrorId);
            _runner.Dispose();
        }
    }
}
