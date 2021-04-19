using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ElmahCore.MongoDB
{
    [BsonIgnoreExtraElements]
    internal sealed class MongoDBLogEntry
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string ErrorId { get; set; }
        [BsonIgnore]
        public Error Error { get; set; }
        public string XmlError { get; set; }
        // mapping
        public Dictionary<string, string[]> QueryString { get; set; }
        public Dictionary<string, string[]> ServerVariables { get; set; }
        public string WebHostHtmlMessage { get; set; }
        public int StatusCode { get; set; }
        public DateTime Time { get; set; }
        public string User { get; set; }
        public string Detail { get; set; }
        public string Message { get; set; }
        public string Source { get; set; }
        public string Body { get; set; }
        public string Type { get; set; }
        public string HostName { get; set; }
        public string ApplicationName { get; set; }
        public Exception Exception { get; set; }
        public List<ElmahLogParamEntry> Params { get; set; }
        public List<ElmahLogMessageEntry> MessageLog { get; set; }
        public Dictionary<string, string[]> Form { get; set; }
        public Dictionary<string, string[]> Cookies { get; set; }

        //[BsonElement("internalid")]
        //public string Id { get; set; }
        public MongoDBLogEntry(string id, string xml, Error error) //(ErrorLog log, string id, Error error) : base(log, id, error)
        {
            Id = ObjectId.GenerateNewId();
            ErrorId = id;
            XmlError = xml;
            FromErrorToMongo(error);
        }

        public void FromErrorToMongo(Error error)
        {
            //https://stackoverflow.com/questions/7003740/how-to-convert-namevaluecollection-to-json-string
            var d = error.QueryString.AllKeys.ToDictionary(k => k, k => error.QueryString.GetValues(k));
            QueryString = d;
            var e = error.ServerVariables.AllKeys.ToDictionary(k => k, k => error.ServerVariables.GetValues(k));
            ServerVariables = e;
            WebHostHtmlMessage = error.WebHostHtmlMessage;
            StatusCode = error.StatusCode;
            Time = error.Time;
            User = error.User;
            Detail = error.Detail;
            Message = error.Message;
            Source = error.Source;
            Body = error.Body;
            Type = error.Type;
            HostName = error.HostName;
            ApplicationName = error.ApplicationName;
            Exception = error.Exception;
            Params = error.Params;
            MessageLog = error.MessageLog;
            var g = error.Form.AllKeys.ToDictionary(k => k, k => error.Form.GetValues(k));
            var h = error.Cookies.AllKeys.ToDictionary(k => k, k => error.Cookies.GetValues(k));
            Form = g;
            Cookies = h;

            //throw new NotImplementedException();      //throw new NotImplementedException();
        }
    }
}
