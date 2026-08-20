using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using TUnit.Core.Interfaces;

namespace WiseAuth.Sample.IntegrationTests;

public class SampleAppFactory : WebApplicationFactory<Program>, IAsyncInitializer
{
    private const string DbFileName = "wiseauth-sample-integration-tests.db";
    private int _disposed;

    public SampleAppFactory()
    {
        // DbSeeder.SeedAsync runs EnsureCreatedAsync + seeding on every host startup, so a
        // leftover file from a previous run would make tests see stale/duplicate data instead
        // of the fresh seed.
        DeleteDatabaseFiles();
    }

    // Implementing IAsyncInitializer lets TUnit await this exactly once before handing the
    // shared instance to any test, across every class that references it - without it, the
    // host (and DbSeeder's EnsureCreatedAsync) would instead build lazily on each class's
    // first CreateClient() call, and TUnit does not serialize that against other classes
    // concurrently doing the same thing, so two hosts could both try to create the same
    // Sqlite schema at once.
    public async Task InitializeAsync()
    {
        _ = Server;

        // Every test class shares this one Sqlite file and TUnit runs their write-heavy
        // methods in parallel. The default rollback-journal mode locks the whole file for
        // any writer; WAL mode lets readers and a writer proceed concurrently instead, so
        // contention stays well inside Microsoft.Data.Sqlite's busy_timeout rather than
        // risking an intermittent SQLITE_BUSY failure.
        await using var connection = new SqliteConnection($"Data Source={DbFileName}");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode=WAL;";
        await command.ExecuteNonQueryAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = $"Data Source={DbFileName}",
            });
        });
    }

    // As a SharedType.PerAssembly fixture this outlives every test class that references
    // it, and TUnit's shared-instance teardown has been observed calling DisposeAsync more
    // than once on the same instance (a known TUnit issue, see thomhurst/TUnit#2867). The
    // Interlocked guard skips an outright second call; the try/catch covers the case where
    // TUnit's *first* call already races against its own teardown machinery and the base
    // WebApplicationFactory NREs on its own partially-torn-down state. Either way this only
    // ever fires during teardown, after the test's real assertions already ran - it should
    // never mask an actual test failure. The Source filter keeps this from also swallowing
    // an unrelated NRE thrown while disposing a registered service (e.g. AppDbContext or the
    // DataProtection key store), which would come from a different assembly.
    public override async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        try
        {
            await base.DisposeAsync();
        }
        catch (NullReferenceException ex) when (ex.Source == "Microsoft.AspNetCore.Mvc.Testing")
        {
        }
    }

    private static void DeleteDatabaseFiles()
    {
        // Sqlite can leave -shm/-wal sidecar files alongside the main db file.
        foreach (var suffix in new[] { "", "-shm", "-wal" })
        {
            File.Delete(DbFileName + suffix);
        }
    }
}
