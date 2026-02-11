#:package Microsoft.Extensions.FileSystemGlobbing@10.0.2

using System.Diagnostics;
using Microsoft.Extensions.FileSystemGlobbing;

var cwd = Environment.CurrentDirectory;
var inputPath = Path.GetFullPath(Path.Combine("src", "Sandbox.Reactivity"), Environment.CurrentDirectory);
var outputPath = Path.GetFullPath("publish", Environment.CurrentDirectory);

Console.WriteLine($"📂 Emitting files to {outputPath}");

if (Directory.Exists(outputPath))
{
	Directory.Delete(outputPath, true);
}

Directory.CreateDirectory(outputPath);

Matcher matcher = new();
matcher.AddExcludePatterns([
	"publish/**/*",
	"**/obj/**"
]);
matcher.AddIncludePatterns([
	"reactivity.sbproj",
	"Code/**/*.cs"
]);

foreach (var source in matcher.GetResultsInFullPath(inputPath))
{
	var relative = Path.GetRelativePath(inputPath, source);
	var destination = Path.GetFullPath(relative, outputPath);

	if (Path.GetDirectoryName(destination) is not { } directory)
	{
		throw new FileNotFoundException($"Could not copy {source} to {destination}");
	}

	Console.ForegroundColor = ConsoleColor.Green;
	Console.Write("    + ");
	Console.ResetColor();
	Console.WriteLine($"{Path.Combine(".", Path.GetRelativePath(cwd, destination))}");

	Directory.CreateDirectory(directory);
	File.Copy(source, destination);
}

Console.WriteLine("\n🔧 Preparing for publish");
Console.WriteLine("    ℹ️ Starting editor");

var process = Process.Start(new ProcessStartInfo()
{
	FileName = Path.GetFullPath("reactivity.sbproj", outputPath),
	UseShellExecute = true
});

if (process == null)
{
	throw new InvalidOperationException("Failed to start editor");
}

process.WaitForInputIdle();
Console.WriteLine("    ℹ️ Waiting for editor to load");

while (!process.HasExited && !process.MainWindowTitle.EndsWith(" - s&box editor"))
{
	await Task.Delay(1000);
	process.Refresh();
}

await Task.Delay(5000);
Console.WriteLine("    ℹ️ Cleaning editor content");

Matcher removeMatcher = new();
removeMatcher.AddInclude("**/*");
removeMatcher.AddExcludePatterns([
	"reactivity.sbproj",
	"Code/**/*.cs"
]);

foreach (var file in removeMatcher.GetResultsInFullPath(outputPath))
{
	Console.ForegroundColor = ConsoleColor.Red;
	Console.Write("        - ");
	Console.ResetColor();
	Console.WriteLine($"{Path.Combine(".", Path.GetRelativePath(cwd, file))}");

	File.Delete(file);
}

Console.WriteLine("\n📦 Ready to publish");
