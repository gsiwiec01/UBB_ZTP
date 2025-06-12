using Microsoft.Extensions.Configuration;
using Ztp.Project1;

IConfiguration cfg = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddCommandLine(args: Environment.GetCommandLineArgs().Skip(1).ToArray())
    .Build();

var defaultProfileName = cfg["DefaultProfile"] ?? throw new InvalidOperationException();

Console.WriteLine($"Podaj nazwę profilu (domyślnie: '{defaultProfileName}'): ");
var inputProfile = Console.ReadLine()?.Trim();
var profile = string.IsNullOrWhiteSpace(inputProfile) ? defaultProfileName : inputProfile;

Console.WriteLine($"Profile: {profile}");

var configuration = Configuration.Load(profile, cfg);
configuration.DumpToConsole();
configuration.ApplyGcConfig();

var imageExists = File.Exists(configuration.ImagePath);
if (!imageExists)
{
    Console.WriteLine("Image not found at the specified path: " + configuration.ImagePath);
    return;
}

var (filter, filterName) = FilterSelector.Choose(configuration);
ImageProcessor.Process(configuration.ImagePath, filter, filterName, configuration);

Console.Read();