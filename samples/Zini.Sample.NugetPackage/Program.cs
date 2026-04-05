using Microsoft.Extensions.Configuration;
using Zini;
using Zini.Configuration;

var sampleFile = "sample.ini";
var sampleIni = await File.ReadAllTextAsync(sampleFile);

Console.WriteLine("========= Plain Parsing =========");

var result = ConfigParser.Parse(sampleIni);
Console.WriteLine("{0} config values parsed", result.Count);
Console.WriteLine("=================================");
Console.WriteLine();
Console.WriteLine();
Console.WriteLine();
Console.WriteLine("===== Configuration Builder =====");

var builder = new ConfigurationBuilder()
	.AddConfigFile(sampleFile);
var config = builder.Build();
var debugView = config.GetDebugView();

Console.WriteLine(debugView);
Console.WriteLine("=================================");