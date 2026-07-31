using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using TaskManager.API.GraphQL.Mutations;
using TaskManager.API.GraphQL.Mutations.Projects;
using TaskManager.API.GraphQL.Queries;
using TaskManager.API.Repositories;
using TaskManager.API.Services;

namespace TaskManager.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddAuthorization();

            //builder.Services.AddControllers();

            builder.Services
                .AddGraphQLServer()
                .AddAuthorization()
                .AddQueryType<Query>()
                .AddMutationType<Mutation>()
                
                .AddTypeExtension<UserQuery>()
                .AddTypeExtension<UserMutation>()

                .AddTypeExtension<InviteQuery>()
                .AddTypeExtension<InviteMutations>()

                .AddTypeExtension<AuthMutations>()

                .AddTypeExtension<ProjectMutations>()
                ;



            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowReact", policy =>
                {
                    policy.WithOrigins("http://localhost:5173")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddNpgsqlDataSource(connectionString);

            var jwtSecretKey = builder.Configuration.GetSection("Jwt:Key").Value;


            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,

                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
                        ValidateIssuer = false,
                        ValidateAudience = false
                    };
                });

            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IUserServices, UserServices>();
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<IAuthService, AuthService>();

            var app = builder.Build();

            app.UseHttpsRedirection();

            app.UseCors("AllowReact");

            app.UseAuthentication();

            app.UseAuthorization();


            app.MapGraphQL();
            //app.MapControllers();



            Console.OutputEncoding = System.Text.Encoding.UTF8;
            try
            {
                using var connection = new NpgsqlConnection(connectionString);
                connection.Open();

                using var command = new NpgsqlCommand("SELECT 1", connection);
                var result = command.ExecuteScalar();

                Console.WriteLine($"PostgreSQL працює! Результат: {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка підключення: {ex.Message}");
            }

            app.Run();
        }
    }
}
