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
        private static bool _isSerializerRegistered;

        public MongoService()
        {
            // 1. Безопасная регистрация GUID
            if (!_isSerializerRegistered)
            {
                BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
                _isSerializerRegistered = true;
            }

            string connectionString = "СЮДА СВЮ БАЗУ ДАННЫХ";

            var settings = MongoClientSettings.FromConnectionString(connectionString);

            settings.MaxConnectionPoolSize = 100;

            var client = new MongoClient(settings);
            var database = client.GetDatabase("PersikMusicDB");

            _users = database.GetCollection<User>("Users");
            _playlists = database.GetCollection<Playlist>("Playlists");

            _ = EnsureIndexesAsync();
        }

        private async Task EnsureIndexesAsync()
        {
            var userIndex = new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(u => u.Username));
            await _users.Indexes.CreateOneAsync(userIndex).ConfigureAwait(false);

            var playlistIndex = new CreateIndexModel<Playlist>(Builders<Playlist>.IndexKeys.Ascending(p => p.OwnerId));
            await _playlists.Indexes.CreateOneAsync(playlistIndex).ConfigureAwait(false);
        }

        public async Task<User> GetOrCreateUser(string username)
        {
            if (string.IsNullOrEmpty(username)) return null!;

            try
            {
                var user = await _users.Find(u => u.Username == username)
                                      .FirstOrDefaultAsync()
                                      .ConfigureAwait(false);

                if (user == null)
                {
                    user = new User
                    {
                        Id = Guid.NewGuid(),
                        Username = username,
                        FavoriteDriveIds = new List<string>()
                    };
                    await _users.InsertOneAsync(user).ConfigureAwait(false);
                }

                user.FavoriteDriveIds ??= new List<string>();
                return user;
            }
            catch (Exception ex)
            {
                throw new Exception($"DB Error: {ex.Message}");
            }
        }

        public async Task UpdateFavorites(Guid userId, List<string> favorites)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update.Set(u => u.FavoriteDriveIds, favorites);

            await _users.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = false })
                        .ConfigureAwait(false);
        }

        public async Task CreatePlaylist(string name, Guid ownerId)
        {
            var playlist = new Playlist
            {
                Id = Guid.NewGuid(),
                Name = name,
                OwnerId = ownerId,
                SongIds = new List<string>()
            };
            await _playlists.InsertOneAsync(playlist).ConfigureAwait(false);
        }

        public async Task<List<Playlist>> GetUserPlaylists(Guid userId)
        {
            return await _playlists.Find(p => p.OwnerId == userId)
                                  .ToListAsync()
                                  .ConfigureAwait(false);
        }
    }
}