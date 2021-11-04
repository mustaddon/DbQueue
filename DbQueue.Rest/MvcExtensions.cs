namespace Microsoft.Extensions.DependencyInjection
{
    public static class DbqRestExtensions
    {
        public static IMvcBuilder AddDbqRest(this IMvcBuilder builder)
        {
            return builder.AddApplicationPart(typeof(DbQueue.Rest.Controllers.QueueController).Assembly);
        }
    }
}