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
            //var x = options.Value.ConnectionString.Split('|');
            //_factory = factory;
            //var dbname = x[0];
            var db = client.GetDatabase(databaseName);
            var collectionname = "elmah_collection";
            //_dbname = dbname;
            //var data = new Func<IMongoCollection<MongoLogEntry>>(() => db.GetCollection<MongoLogEntry>(collectionname));

            _collection = CreateCollectionIfNotExistent(db, collectionname);

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
            var errorLog = _collection.Find(a => a.ErrorId == id).FirstOrDefault();
            return new ErrorLogEntry(this, id, ErrorXml.DecodeString(errorLog.XmlError));
            //throw new NotImplementedException();
        }

        public override int GetErrors(int errorIndex, int pageSize, ICollection<ErrorLogEntry> errorEntryList)
        {
            var errorLog = _collection.Find(a => true).Skip(errorIndex).Limit(pageSize).ToList();
            errorLog.ForEach(a => errorEntryList.Add(
              new ErrorLogEntry(this, a.ErrorId, ErrorXml.DecodeString(a.XmlError))));
            return errorLog.Count;
            throw new NotImplementedException();
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
            //throw new NotImplementedException();
        }
    }
}

