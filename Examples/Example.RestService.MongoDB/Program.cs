var builder = WebApplication.CreateBuilder(args);

// add services to the container
builder.Services.AddDbqMongo((services, options) =>
{
    // add database provider 
    options.Database.ConnectionString = services.GetRequiredService<IConfiguration>().GetConnectionString("dbq")!;

    // add blob's path construction algorithm 
    options.BlobStorage.PathBuilder = (filename) => Path.GetFullPath($@"_blob\{DateTime.Now:yyyy\\MM\\dd}\{filename}");
});

//// net5.0: add REST controllers
//builder.Services.AddControllers().AddDbqRest();

var app = builder.Build();

//// net5.0: map controllers
//app.MapControllers();

// net6.0: map REST service
app.MapDbqRest();

app.Run();