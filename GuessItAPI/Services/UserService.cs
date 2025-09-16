using GuessItAPI.Context;
using GuessItAPI.Jwt;
using GuessItAPI.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using System.Reflection.Metadata.Ecma335;

namespace GuessItAPI.Services
{
    public class UserService
    {
        private readonly GuessItDbContext _dbContext;
        public UserService(GuessItDbContext db)
        {
            _dbContext = db;
        }

        public async Task Register(User user, string role) => await Register(user.Username, user.PasswordHash, role);
        public async Task Register(string username, string password, string role)
        {
            GuessItDbContext context = new GuessItDbContext();

            User user = new User();
            user.Username = username;
            user.PasswordHash = PasswordHasher.Generate(password);
            user.Role = role;

            context.Users.Add(user);

            await context.SaveChangesAsync();
        }

        public async Task Remover(User user) => await Remover(user.Username);
        public async Task Remover(string username)
        {
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user != null)
            {
                var userData = _dbContext.UserData
                    .Where(u => u.UserId == user.UserId);

                if (await userData.AnyAsync())
                {
                    _dbContext.UserData.RemoveRange(userData);
                }

                _dbContext.Users.Remove(user);
                await _dbContext.SaveChangesAsync();
            }
        }
        public bool Checker(string username = null)
        {
            if (!string.IsNullOrEmpty(username) && _dbContext.Users.Any(u => u.Username == username))
                return false;
            return true;
        }

        public async Task<User?> GetByUsername(string username) =>
            await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username) ?? null;
        public async Task<User?> GetById(int id) =>
            await _dbContext.Users.FirstOrDefaultAsync(u => u.UserId == id) ?? null;
        public async Task<User?> GetByAuth(string auth)
        {
            User? user = null;
            if (int.TryParse(auth, out int id))
                user = await GetById(id);
            else
                user = await GetByUsername(auth);
            return user;
        }

        public async Task<bool> CheckAuth(string username, string password)
        {
            User? user = await GetByUsername(username);

            if (user != null)
                return PasswordHasher.Verify(password, user.PasswordHash);
            else return false;
        }

        public async Task<string> Login(string username)
        {
            User? user = await GetByUsername(username);
            if (user == null)
                return "Error";
            var token = JwtProvider.GenerateToken(user, true);

            return token;
        }
        public async Task<bool> PutAvatar(User user, byte[] avatar)
        {
            UserDatum? userDatum = await _dbContext.UserData.FirstOrDefaultAsync(u => u.UserId == user.UserId);
            if (userDatum != null)
                userDatum.Avatar = avatar;
            else
            {
                UserDatum userData = new UserDatum();
                userData.UserId = user.UserId;
                userData.Avatar = avatar;
                await _dbContext.UserData.AddAsync(userData);
            }
            await _dbContext.SaveChangesAsync();
            return true;
        }
        public async Task<byte[]?> GetAvatar(User user)
        {
            UserDatum? userDatum = await _dbContext.UserData.FirstOrDefaultAsync(u => u.UserId == user.UserId);
            if (userDatum == null)
                return null;
            return userDatum.Avatar;
        }
        public async Task<bool> DeleteAvatar(User user)
        {
            UserDatum? userDatum = await _dbContext.UserData.FirstOrDefaultAsync(u => u.UserId == user.UserId);
            UserDatum? userDatumModel = await _dbContext.UserData.FirstOrDefaultAsync(u => u.UserId == 0);
            userDatum!.Avatar = userDatumModel!.Avatar;
            //userDatum!.UserAvatar = null!;
            _dbContext.SaveChanges();
            return true;
        }
        public bool ChangeUsername(string oldUsername, string newUsername)
        {

            if (_dbContext.Users.Any(u => u.Username == newUsername))
                return false;

            _dbContext.Users.FirstOrDefault(u => u.Username == oldUsername)!.Username = newUsername;
            _dbContext.SaveChanges();
            return true;
        }
        public async Task<bool> ChangePassword(string username, string newPassword)
        {

            User? user = await GetByUsername(username)!;
            if (user == null) return false;
            _dbContext.Users.FirstOrDefault(u => u.Username == user.Username)!.PasswordHash = PasswordHasher.Generate(newPassword);
            _dbContext.SaveChanges();
            return true;
        }
    }
}
