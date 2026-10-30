using System.Xml.Linq;
using Levio.Modules.Auditing;
using Levio.Modules.Identity;
using Levio.Modules.Identity.Contracts;
using Levio.Modules.LeaveManagement;
using Levio.SharedKernel.Domain;

internal static class ArchitectureChecks
{
    public static void Run()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Levio.slnx"))) root = root.Parent;
        if (root is null) throw new Exception("Cannot locate backend solution for project dependency checks.");
        var assemblies = new[] { typeof(IdentityModule).Assembly, typeof(LeaveManagementModule).Assembly,
            typeof(AuditingModule).Assembly, typeof(BaseEntity).Assembly, typeof(IEmployeeDirectory).Assembly };
        foreach (var assembly in assemblies)
        {
            var name = assembly.GetName().Name!;
            var module = name.StartsWith("Levio.Modules.");
            var path = module ? Path.Combine(root.FullName, "Modules", name, name + ".csproj")
                : Path.Combine(root.FullName, name, name + ".csproj");
            var references = XDocument.Load(path).Descendants("ProjectReference")
                .Select(e => Path.GetFileNameWithoutExtension(((string)e.Attribute("Include")!).Replace('\\', '/')));
            foreach (var reference in references)
                Check(Allowed(name, reference), $"Forbidden project reference: {name} -> {reference}");
            foreach (var reference in assembly.GetReferencedAssemblies().Where(a => a.Name!.StartsWith("Levio.")))
                Check(Allowed(name, reference.Name!), $"Forbidden assembly dependency: {name} -> {reference.Name}");
            Check(!assembly.GetReferencedAssemblies().Any(a => a.Name!.StartsWith("Microsoft.EntityFrameworkCore")),
                $"EF Core escaped shared Infrastructure into {name}.");
        }
        Console.WriteLine("PASS: Module and SharedKernel dependency boundaries (projects and assemblies).");
    }

    private static bool Allowed(string source, string target) => source switch
    {
        "Levio.Modules.Identity" or "Levio.Modules.LeaveManagement" => target is "Levio.SharedKernel" or "Levio.Modules.Identity.Contracts",
        "Levio.Modules.Auditing" => target == "Levio.SharedKernel",
        _ => false
    };

    public static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
