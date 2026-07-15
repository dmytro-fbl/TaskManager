using Npgsql;
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

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();


            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddNpgsqlDataSource(connectionString);

            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IUserServices, UserServices>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();



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
