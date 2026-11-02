using System.Text.Json;
using System.Text.Json.Nodes;
using Levio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseSqlServer("Server=localhost;Database=Levio_ModelOnly;Integrated Security=true;TrustServerCertificate=true").Options);
var model = db.GetService<IDesignTimeModel>().Model;
var baseline = JsonSerializer.Serialize(new
{
    Tables = model.GetRelationalModel().Tables.OrderBy(t => t.Name).Select(t => new
    {
        t.Name, t.Schema,
        Columns = t.Columns.OrderBy(c => c.Name).Select(c => new { c.Name, c.StoreType, c.IsNullable, Default = c.DefaultValueSql, Computed = c.ComputedColumnSql }),
        Keys = t.UniqueConstraints.OrderBy(k => k.Name).Select(k => new { k.Name, Columns = k.Columns.Select(c => c.Name) }),
        Indexes = t.Indexes.OrderBy(i => i.Name).Select(i => new { i.Name, i.IsUnique, i.Filter, Columns = i.Columns.Select(c => c.Name) }),
        ForeignKeys = t.ForeignKeyConstraints.OrderBy(f => f.Name).Select(f => new { f.Name, Principal = f.PrincipalTable.Name, f.OnDeleteAction, Columns = f.Columns.Select(c => c.Name), PrincipalColumns = f.PrincipalColumns.Select(c => c.Name) })
    }),
    Seeds = model.GetEntityTypes().OrderBy(e => e.GetTableName()).Select(e => new
    {
        Table = e.GetTableName(),
        Rows = e.GetSeedData(providerValues: true).OrderBy(r => r["Id"]).Select(r => r.OrderBy(p => p.Key).ToDictionary(p => p.Key, p => p.Value))
    }),
    Migrations = db.Database.GetMigrations(),
    MigrationScript = db.GetService<IMigrator>().GenerateScript()
}, new JsonSerializerOptions { WriteIndented = true });
var baselinePath = Path.Combine(AppContext.BaseDirectory, "database-baseline.json");
if (!JsonNode.DeepEquals(Normalize(File.ReadAllText(baselinePath)), Normalize(baseline)))
{
    var actualPath = Path.Combine(AppContext.BaseDirectory, "database-actual.json");
    File.WriteAllText(actualPath, baseline);
    throw new Exception($"Database model, seeds or migration SQL changed; compare {actualPath} with the committed baseline.");
}
Console.WriteLine("PASS: Database compatibility baseline.");
if (db.Database.HasPendingModelChanges())
    throw new Exception("Migration snapshot does not match the current model.");
Console.WriteLine("PASS: Migration snapshot matches current model.");
ArchitectureChecks.Run();
LeaveCalendarChecks.Run();
await AuthenticationChecks.RunAsync();
await DatabaseIntegrationChecks.RunAsync();

static JsonNode Normalize(string json)
{
    var document = JsonNode.Parse(json.Replace("\\r\\n", "\\n"))!;
    // EF includes navigation values in GetSeedData; compare only persisted columns.
    var columns = document["Tables"]!.AsArray().ToDictionary(t => t!["Name"]!.GetValue<string>(),
        t => t!["Columns"]!.AsArray().Select(c => c!["Name"]!.GetValue<string>()).ToHashSet());
    foreach (var seeds in document["Seeds"]!.AsArray())
        foreach (var row in seeds!["Rows"]!.AsArray())
            foreach (var property in row!.AsObject().Select(p => p.Key).ToArray())
                if (!columns[seeds["Table"]!.GetValue<string>()].Contains(property)) row.AsObject().Remove(property);
    return document;
}
