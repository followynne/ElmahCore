using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;

namespace ElmahCore.MongoDB
{
#nullable enable
    public sealed class MongoDBErrorLog : ErrorLog
    {
        private readonly IMongoCollection<MongoDBLogEntry> _collection;
        /// <summary>
        ///     Gets the name of this error log implementation.
        /// </summary>
        public override string Name => "MongoDB Error Log";
        public string CollectionName { get; set; }
        public MongoDBErrorLog(IMongoClient client, IOptions<ElmahOptions> options) : this(client, options.Value.ConnectionString, options.Value.DatabaseName, options.Value.CollectionName, options.Value.ShouldCollectionBeCapped, options.Value.CapMbSize) { }
        /// <summary>
        /// it searches for a singleton registration; if it's null it instances a new client
        /// it expects an existing database - it will create the collection if not existent
        /// </summary>
        /// <example>
        /// IMongoClient singleton registration example
        /// services.AddSingleton<>IMongoClient>(s =>
        /// {
        ///   var client = new MongoClient("mongodb://localhost:27017/?readPreference=primary");
        ///   return client;
        /// });
        /// </example>
        public MongoDBErrorLog(IMongoClient client, string? connectionString, string databaseName,
            string? collectionName, bool createCappedCollection, long collectionSize)
        {
            IMongoClient internalClient = client;
            if (internalClient is null)
            {
                if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException("Mongo Client instance not provided and connection string null.");
                internalClient = new MongoClient(connectionString);
            }

            var db = internalClient.GetDatabase(databaseName);
            CollectionName = collectionName ?? "elmahcore_collection";
            _collection = CreateCollectionIfNotExistent(db, CollectionName, createCappedCollection, collectionSize);
        }

        private IMongoCollection<MongoDBLogEntry> CreateCollectionIfNotExistent(IMongoDatabase db, string collectionname, bool createCappedCollection, long collectionSize)
        {
            var checkCollection = db.ListCollectionNames(new ListCollectionNamesOptions()
            {
                Filter = Builders<BsonDocument>.Filter.Eq("name", collectionname)
            });
            if (!checkCollection.Any())
            {
                if (createCappedCollection)
                {
                    var options = new CreateCollectionOptions
                    {
                        Capped = createCappedCollection,
                        MaxSize = collectionSize > 0 ? collectionSize : 50000,
                        MaxDocuments = 10000,
                    };
                    db.CreateCollection(collectionname, options);
                }
                else
                {
                    db.CreateCollection(collectionname);
                }
            }
            return db.GetCollection<MongoDBLogEntry>(collectionname);
        }

        public override ErrorLogEntry GetError(string id)
        {
            var searchedError = _collection.Find(a => a.ErrorId == id)
                .Project(err => err.XmlError).FirstOrDefault();
            return new ErrorLogEntry(this, id, ErrorXml.DecodeString(searchedError));
        }

        public override int GetErrors(int errorIndex, int pageSize, ICollection<ErrorLogEntry> errorEntryList)
        {
            if (errorIndex < 0) throw new ArgumentOutOfRangeException(nameof(errorIndex), errorIndex, null);
            if (pageSize < 0) throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, null);

            var errorLog = _collection.Find(FilterDefinition<MongoDBLogEntry>.Empty)
            .Sort(Builders<MongoDBLogEntry>.Sort.Descending(a => a.Time));

            var documentsCount = errorLog.CountDocuments();
            var errors = errorLog.Skip(errorIndex).Limit(pageSize).ToList();

            errors.ForEach(a => errorEntryList.Add(
              new ErrorLogEntry(this, a.ErrorId, ErrorXml.DecodeString(a.XmlError))));
            return documentsCount < int.MaxValue ? Convert.ToInt32(documentsCount) : int.MaxValue;
        }

        public override string Log(Error error)
        {
            var id = Guid.NewGuid();
            Log(id, error);
            return id.ToString();
        }

        public override void Log(Guid id, Error error)
        {
            var xmlerror = ErrorXml.EncodeString(error);
            _collection.InsertOne(new MongoDBLogEntry(id.ToString(), xmlerror, error));
        }
    }
}

