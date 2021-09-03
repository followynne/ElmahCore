using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ElmahCore.MongoDB
{
    [BsonIgnoreExtraElements]
    public sealed class MongoDBLogEntry
    {
        public MongoDBLogEntry(string id, string xml, Error error)
        {
            Id = ObjectId.GenerateNewId();
            ErrorId = id;
            XmlError = xml;
            FromErrorToMongo(error);
        }
        [BsonId]
        public ObjectId Id { get; set; }
        public string ErrorId { get; set; }
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
        public MongoException Exception { get; set; }
        public List<ElmahLogParamEntry> Params { get; set; }
        public List<ElmahLogMessageEntry> MessageLog { get; set; }
        public Dictionary<string, string[]> Form { get; set; }
        public Dictionary<string, string[]> Cookies { get; set; }

        internal void FromErrorToMongo(Error error)
        {
            User = error.User;
            StatusCode = error.StatusCode;
            Time = error.Time;
            ApplicationName = error.ApplicationName;
            Detail = error.Detail;
            Message = error.Message;
            Source = error.Source;
            Body = error.Body;
            Type = error.Type;
            WebHostHtmlMessage = error.WebHostHtmlMessage;
            HostName = error.HostName;
            Params = error.Params;
            Exception = new MongoException(error.Exception);
            QueryString = error.QueryString.AllKeys.ToDictionary(byKey => byKey, byKey => error.QueryString.GetValues(byKey));
            ServerVariables = error.ServerVariables.AllKeys.ToDictionary(byKey => byKey, byKey => error.ServerVariables.GetValues(byKey));
            Form = error.Form.AllKeys.ToDictionary(k => k, k => error.Form.GetValues(k));
            Cookies = error.Cookies.AllKeys.ToDictionary(k => k, k => error.Cookies.GetValues(k));
        }
    }
    // custom class to help Mongo Serialization on Exceptions
    public class MongoException
    {
        public MongoException(Exception ex)
        {
            Message = ex.Message;
            StackTrace = ex.StackTrace;
            InnerException = ex.InnerException != null ? new MongoException(ex.InnerException) : null;
            Source = ex.Source;
        }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public string Source { get; set; }
        public MongoException InnerException { get; set; }
    }
}
