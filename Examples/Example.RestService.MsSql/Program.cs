using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// add services to the container
builder.Services.AddDbqEfc((services, options) =>
{
    // add database provider 
    var connectionString = services.GetRequiredService<IConfiguration>().GetConnectionString("dbq");
    options.Database.ContextConfigurator = (db) => db.UseSqlServer(connectionString);

    // add blob's path construction algorithm 
    options.BlobStorage.PathBuilder = (filename) => Path.GetFullPath($@"_blob\{DateTime.Now:yyyy\\MM\\dd}\{filename}");
});

//// for net5.0 add via controllers
//builder.Services.AddControllers().AddDbqRest();

var app = builder.Build();

// map REST service to the endpoint
app.MapDbqRest();

//// for net5.0 map controllers
//app.MapControllers();

app.Run();