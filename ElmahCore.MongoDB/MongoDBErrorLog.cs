using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;

namespace ElmahCore.MongoDB
{
    public sealed class MongoDBErrorLog : ErrorLog
    {
        private readonly IMongoCollection<MongoDBLogEntry> _collection;
        /// <summary>
        ///     Gets the name of this error log implementation.
        /// </summary>
        public override string Name => "MongoDB Error Log";

        /// <summary>
        ///     Gets the connection string used by the log to connect to the database.
        /// </summary>
        // ReSharper disable once MemberCanBeProtected.Global
        public string ConnectionString { get; }
        public MongoDBErrorLog(IMongoClient client, IOptions<ElmahOptions> options) : this(client, options.Value.ConnectionString, options.Value.DatabaseName, options.Value.CollectionName)
        {
        }
        public MongoDBErrorLog(IMongoClient client, string connectionString, string databaseName, string collectionName = "elmahcore_collection")
        {
            IMongoClient internalClient;
            if (client is null)
            {
                internalClient = new MongoClient(connectionString);
            }
            else
            {
                internalClient = client;
            }
            var db = internalClient.GetDatabase(databaseName);
            collectionName ??= "elmahcore_collection";
            _collection = CreateCollectionIfNotExistent(db, collectionName);
        }

        private IMongoCollection<MongoDBLogEntry> CreateCollectionIfNotExistent(IMongoDatabase db, string collectionname)
        {
            var checkCollection = db.ListCollectionNames(new ListCollectionNamesOptions()
            {
                Filter = Builders<BsonDocument>.Filter.Eq("name", collectionname)
            });
            if (!checkCollection.Any())
            {
                db.CreateCollection(collectionname);
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

