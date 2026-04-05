using Microsoft.Extensions.Configuration;
using Zini.Configuration;

var config = new ConfigurationBuilder()
	.AddConfigFile("sample.ini")
	.Build();

var debugView = config.GetDebugView();
Console.WriteLine(debugView);