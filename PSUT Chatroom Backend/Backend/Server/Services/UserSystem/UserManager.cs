using System.Threading.Tasks;
using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Server.Db;
using Server.Db.Entities;
using RegnewCommon;
namespace Server.Services.UserSystem
{
    public class UserManager
    {
        public static readonly string LoginCookieName = "PSUT";
        public static readonly string LoginHeaderName = "Authorization";
        private string GenerateToken(User user) => $"{user.Id}:{DateTime.UtcNow.Ticks}";

        /// <summary>
        /// To be used by filters only.
        /// </summary>
        public static UserManager Instance { get; private set; }

        public static void Init(IServiceProvider sp)
        {
            var fac = sp.GetRequiredService<IDbContextFactory<AppDbContext>>();

            Instance = new UserManager(fac.CreateDbContext(), sp.GetRequiredService<SaltBae>());
        }

        /// <summary>
        /// Its public to be used only by <see cref="User.Seed"/>.
        /// </summary>
        public static string HashPassword(string password)
        {
            // var bytes = System.Text.Encoding.Unicode.GetBytes(password);
            // var sha1 = new SHA1CryptoServiceProvider();
            // var sha1data = sha1.ComputeHash(bytes);
            // var str = System.Text.Encoding.Unicode.GetString(sha1data);
            // return str;
            return password;
        }

        private readonly AppDbContext _dbContext;
        private readonly SaltBae _saltBae;
        public UserManager(AppDbContext dbContext, SaltBae saltBae)
        {
            _dbContext = dbContext;
            _saltBae = saltBae;
        }
        /// <returns>User Id</returns>
        public async Task<int?> GmailLogin(string? email, IResponseCookies cookies)
        {
            if (email == null)
            {
                return null;
            }
            email = email.ToLowerInvariant();
            var user = _dbContext.Users
                .FirstOrDefault(u => u.Email.ToLower() == email);
            if (user == null)
            {
                return null;
            }

            user.Token = GenerateToken(user);
            var cookieOptions = new CookieOptions
            {
                Path = "/",
                Expires = DateTimeOffset.Now + TimeSpan.FromDays(10),
                IsEssential = true,
                HttpOnly = false,
                Secure = false,
                MaxAge = TimeSpan.FromDays(11)
            };
            cookies.Append(LoginCookieName, user.Token, cookieOptions);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
            return user.Id;
        }

        public async Task Logout(User user, IResponseCookies cookies)
        {
            var myUser = await _dbContext.Users.FindAsync(user.Id).ConfigureAwait(false);
            myUser.Token = null;
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
            cookies.Delete(LoginCookieName);
        }

        public User? GetUserByToken(string token)
        {
            var user = _dbContext.Users.FirstOrDefault(u => u.Token != null && u.Token.ToLower() == token);
            return user;
        }

        public async Task UpdateUser(int userId, string? newName)
        {
            var user = await _dbContext.Users.FindAsync(userId).ConfigureAwait(false);

            if (newName != null)
            {
                user.Name = newName;
            }

            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task<User?> IdentifyUser(HttpRequest request)
        {
            request.Cookies.TryGetValue(LoginCookieName, out var cookie);
            request.Headers.TryGetValue(LoginHeaderName, out var headers);
            if (headers.Count > 1)
            {
                return null;
            }

            var header = headers.Count == 0 ? null : headers[0];
            string token;
            if (cookie == null && header == null)
            {
                return null;
            }

            if (cookie != null && header != null)
            {
                if (cookie[0] != header[0])
                {
                    return null;
                }

                token = cookie;
            }
            else if (cookie != null)
            {
                token = cookie;
            }
            else
            {
                token = header!;
            }

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Token == token).ConfigureAwait(false);
            return user;
        }
        //Todo: Add methods to validate user when signing up, or we can keep them in their own validator
    }
}