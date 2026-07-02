using CloudinaryDotNet;

namespace server
{
    public static class CloudinaryStartup
    {
        public static async Task ValidateCloudinaryConnection(this WebApplication app)
        {
            try
            {
                // Create a scope so we can resolve scoped services
                using var scope = app.Services.CreateScope();
                var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

                var cloudName = config["Cloudinary:CloudName"]
                    ?? throw new InvalidOperationException("Cloudinary:CloudName not configured.");
                var apiKey = config["Cloudinary:ApiKey"]
                    ?? throw new InvalidOperationException("Cloudinary:ApiKey not configured.");
                var apiSecret = config["Cloudinary:ApiSecret"]
                    ?? throw new InvalidOperationException("Cloudinary:ApiSecret not configured.");

                var account = new Account(cloudName, apiKey, apiSecret);
                var cloudinary = new Cloudinary(account);
                cloudinary.Api.Secure = true;

                var result = await cloudinary.GetUsageAsync();

                if (result != null)
                    Console.WriteLine($"✅ Cloudinary connected successfully! (Cloud: {cloudName})");
                else
                    Console.WriteLine("⚠️  Cloudinary responded but returned no data.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Cloudinary connection failed: {ex.Message}");
            }
        }
    }
}