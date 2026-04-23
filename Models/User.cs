using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace PersikMusic.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)] // Лечит ошибку GuidSerializer
        public Guid Id { get; set; }

        // Указываем пустые значения по умолчанию, чтобы убрать предупреждение CS8618
        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        // Инициализируем список сразу, чтобы не было ошибок при добавлении лайков
        public List<string> FavoriteDriveIds { get; set; } = new List<string>();
    }
}