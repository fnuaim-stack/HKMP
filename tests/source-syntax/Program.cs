using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

if (args.Length != 1) {
    Console.Error.WriteLine("Usage: dotnet run --project tests/source-syntax/SourceSyntax.csproj -- <repository root>");
    return 2;
}

var root = Path.GetFullPath(args[0]);
var directories = new[] { "HKMP", "HKMPServer" };
var paths = new List<string>();

foreach (var relative in directories) {
    var directory = Path.Combine(root, relative);
    if (!Directory.Exists(directory)) {
        Console.Error.WriteLine($"Required source directory not found: {directory}");
        return 2;
    }

    paths.AddRange(Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories)
        .Where(path => {
            var normalized = path.Replace('\\', '/');
            return !normalized.Contains("/bin/") && !normalized.Contains("/obj/");
        }));
}

var parseOptions = new CSharpParseOptions(
    languageVersion: LanguageVersion.Preview,
    documentationMode: DocumentationMode.Parse);

var failures = 0;
foreach (var path in paths.OrderBy(path => path, StringComparer.Ordinal)) {
    var source = File.ReadAllText(path);
    var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions, path);
    var errors = syntaxTree.GetDiagnostics()
        .Where(d => d.Severity == DiagnosticSeverity.Error)
        .ToArray();

    foreach (var diagnostic in errors) {
        Console.Error.WriteLine(diagnostic.ToString());
        failures++;
    }
}

Console.WriteLine($"C# syntax check: {paths.Count} source files, {failures} syntax errors");
if (failures == 0) {
    Console.WriteLine("PASS: All HKMP and HKMPServer C# files parse with Roslyn");
    Console.WriteLine("NOTE: Parsing does not prove type-checking, linking against game DLLs, or working gameplay.");
}
return failures == 0 ? 0 : 1;
