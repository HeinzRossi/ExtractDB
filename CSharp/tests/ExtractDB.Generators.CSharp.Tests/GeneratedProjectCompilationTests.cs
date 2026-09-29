using System.Diagnostics;

namespace ExtractDB.Generators.CSharp.Tests;

public sealed class GeneratedProjectCompilationTests
{
    [Fact]
    public async Task Generate_output_compiles_in_net10_project()
    {
        using var directory = TempDirectory.Create();
        var generator = new CSharpDatabaseGenerator();
        var outputDirectory = Path.Combine(directory.Path, "Generated");

        var result = generator.Generate(
            CSharpDatabaseGeneratorTests.CreateDatabase(),
            new GenerationContext
            {
                NamespaceBase = "Demo.Generated",
                OutputDirectory = outputDirectory,
                SelectedTables = ["cliente", "pedido", "pedido_item"]
            });

        Assert.Equal(0, result.ErrorCount);

        var projectPath = Path.Combine(outputDirectory, "Generated.csproj");
        await File.WriteAllTextAsync(
            projectPath,
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <Nullable>enable</Nullable>
                <ImplicitUsings>enable</ImplicitUsings>
              </PropertyGroup>
            </Project>
            """);

        var build = await RunDotnetBuildAsync(projectPath);

        Assert.True(build.ExitCode == 0, build.Output);
    }

    private static async Task<(int ExitCode, string Output)> RunDotnetBuildAsync(string projectPath)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            ArgumentList = { "build", projectPath },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return (process.ExitCode, output + error);
    }
}
