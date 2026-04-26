using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Aihrly.Tests;

// shared test server — overrides the DB so tests hit aihrly_test, not aihrly
public class ApiFactory : WebApplicationFactory<Program>
{
    private const string TestConnStr =
        "Host=localhost;Database=aihrly_test;Username=postgres;Password=postgres";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = TestConnStr,
            });
        });
    }
}
