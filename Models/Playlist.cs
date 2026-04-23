using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace PersikMusic.Models
{
    public class Playlist
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        [BsonRepresentation(BsonType.String)]
        public Guid OwnerId { get; set; }

        public List<string> SongIds { get; set; } = new List<string>();
    }
}