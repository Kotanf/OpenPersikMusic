using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using PersikMusic.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PersikMusic.Services
{
    public class MongoService
    {
        private readonly IMongoCollection<User> _users;
        private readonly IMongoCollection<Playlist> _playlists;

        public MongoService()
        {
            // 1. ГОВОРИМ ДРАЙВЕРУ, КАК ЖИТЬ С GUID
            // Это нужно вызвать ОДИН РАЗ до создания клиента
#pragma warning disable CS0618 // Подавляем предупреждение об устаревании, так как это стандартный фикс
            BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
#pragma warning restore CS0618

            // сюда ссылку на базу данных
            string connectionString = "";

            var client = new MongoClient(connectionString);
            var database = client.GetDatabase("PersikMusicDB");

            _users = database.GetCollection<User>("Users");
            _playlists = database.GetCollection<Playlist>("Playlists");
        }

        // ПОЛУЧЕНИЕ ИЛИ СОЗДАНИЕ ПОЛЬЗОВАТЕЛЯ
        public async Task<User> GetOrCreateUser(string username)
        {
            // Проверка 1: Если коллекция не инициализирована (NullReferenceException)
            if (_users == null)
            {
                throw new Exception("Ошибка: Коллекция пользователей MongoDB не инициализирована. Проверьте подключение.");
            }

            try
            {
                var user = await _users.Find(u => u.Username == username).FirstOrDefaultAsync();

                if (user == null)
                {
                    user = new User
                    {
                        Id = Guid.NewGuid(),
                        Username = username,
                        // Проверка 2: Гарантируем, что список не null
                        FavoriteDriveIds = new List<string>()
                    };
                    await _users.InsertOneAsync(user);
                }

                // На всякий случай проверяем список у найденного юзера
                if (user.FavoriteDriveIds == null) user.FavoriteDriveIds = new List<string>();

                return user;
            }
            catch (Exception ex)
            {
                // Если здесь вылетает FormatException (Guid), значит в базе старые «кривые» данные
                throw new Exception($"Ошибка при работе с пользователем: {ex.Message}");
            }
        }

        // СОХРАНЕНИЕ ЛАЙКОВ (ИЗБРАННОГО)
        public async Task UpdateFavorites(Guid userId, List<string> favorites)
        {
            var update = Builders<User>.Update.Set(u => u.FavoriteDriveIds, favorites);
            await _users.UpdateOneAsync(u => u.Id == userId, update);
        }

        // СОЗДАНИЕ ПЛЕЙЛИСТА
        public async Task CreatePlaylist(string name, Guid ownerId)
        {
            var playlist = new Playlist
            {
                Id = Guid.NewGuid(),
                Name = name,
                OwnerId = ownerId,
                SongIds = new List<string>()
            };
            await _playlists.InsertOneAsync(playlist);
        }

        // ПОЛУЧЕНИЕ ПЛЕЙЛИСТОВ ПОЛЬЗОВАТЕЛЯ
        public async Task<List<Playlist>> GetUserPlaylists(Guid userId)
        {
            return await _playlists.Find(p => p.OwnerId == userId).ToListAsync();
        }
    }
}