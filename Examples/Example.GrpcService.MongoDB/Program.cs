var builder = WebApplication.CreateBuilder(args);

// add services to the container
builder.Services.AddGrpc(options => options.EnableDetailedErrors = true);
builder.Services.AddDbqMongo((services, options) =>
{
    // add database provider 
    options.Database.ConnectionString = services.GetRequiredService<IConfiguration>().GetConnectionString("dbq");

    // add blob's path construction algorithm 
    options.BlobStorage.PathBuilder = (filename) => Path.GetFullPath($@"_blob\{DateTime.Now:yyyy\\MM\\dd}\{filename}");
});

var app = builder.Build();

// map gRPC service to the endpoint
app.MapGrpcDbQueue();

app.Run();