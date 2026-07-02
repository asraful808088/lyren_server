using Algolia.Search.Clients;


public static class AlgoliaModule
{
    public static IServiceCollection AddAlgolia(this IServiceCollection services, IConfiguration config)
    {
        var appId  = config["Algolia:AppId"];
        var apiKey = config["Algolia:ApiKey"];

        if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("❌ Algolia config missing in appsettings.json");
        var client = new SearchClient(appId, apiKey);
        services.AddSingleton(client);

        return services;
    }

    public static WebApplication UseAlgolia(this WebApplication app)
    {
        try
        {
            var client = app.Services.GetRequiredService<SearchClient>();
            client.ListIndices();
            Console.WriteLine("✅ Algolia connected successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Algolia connection failed: {ex.Message}");
        }

        return app;
    }
}