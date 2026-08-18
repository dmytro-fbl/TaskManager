using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using TaskManager.API.GraphQL.Mutations;
using TaskManager.API.GraphQL.Mutations.Projects;
using TaskManager.API.GraphQL.Extensions;
using TaskManager.API.GraphQL.Queries;
using TaskManager.API.Repositories;
using TaskManager.API.Repositories.ProjectsRepository;
using TaskManager.API.Services;
using TaskManager.API.Repositories.TasksRepository;
using TaskManager.API.GraphQL.Mutations.Tasks;
using TaskManager.API.Repositories.CommentsRepository;
using TaskManager.API.Repositories.TaskCommentsRepository;
using TaskManager.API.Services.TaskServices;

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
                .ModifyRequestOptions(opt =>
                {
                    opt.IncludeExceptionDetails = true;
                })
                .AddGraphQLServer()
                .AddAuthorization()
                .AddQueryType<Query>()
                .AddMutationType<Mutation>()
                
                .AddTypeExtension<UserQuery>()
                .AddTypeExtension<UserMutation>()

                .AddTypeExtension<InviteQuery>()
                .AddTypeExtension<InviteMutations>()

                .AddTypeExtension<AuthMutations>()

                .AddTypeExtension<ProjectQuery>()
                .AddTypeExtension<ProjectMutations>()

                .AddTypeExtension<TaskQuery>()
                .AddTypeExtension<TaskMutations>()

                .AddTypeExtension<TaskAssignmentMutations>()

                .AddTypeExtension<ProjectInviteMutations>()

                .AddTypeExtension<ProjectRoleMutations>()

                .AddTypeExtension<ProjectMembershipExtensions>()

                .AddTypeExtension<TaskCommentQueries>()
                .AddTypeExtension<TaskCommentMutations>()


            ;



            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowReact", policy =>
                {
                    policy.WithOrigins(
                        "http://localhost:5173",
                        "http://localhost:5174"
                    )
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
            //builder.Services.AddScoped<IUserRepository, UserRepository>();

            builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
            //builder.Services.AddScoped<IProjectRepository, ProjectRepository>();

            builder.Services.AddScoped<ITaskRepository, TaskRepository>();
            builder.Services.AddScoped<ITaskCommentsRepository, TaskCommentsRepository>();

            builder.Services.AddScoped<IUserServices, UserServices>();
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<ITaskCommentService, TaskCommentService>();

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
