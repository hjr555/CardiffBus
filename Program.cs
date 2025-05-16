using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;

if (args.Length != 1 || !Regex.IsMatch(args[0], @"^\d{3}$"))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Error: You must supply a 3-digit vehicle ID. Example usage:");
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("  dotnet run -- 123");
    Console.ResetColor();
    return;
}

var vehicleId = args[0];
var baseClient = new HttpClient();

// Step 1: Get vehicle info HTML
string vehicleHtml;
try
{
    var response = await baseClient.GetAsync($"https://www.cardiffbus.com/_ajax/vehicles/{vehicleId}");
    if (!response.IsSuccessStatusCode)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error: Vehicle ID {vehicleId} not found or invalid.");
        Console.ResetColor();
        return;
    }

    vehicleHtml = await response.Content.ReadAsStringAsync();
}
catch (HttpRequestException)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Error: Failed to connect to cardiffbus.com.");
    Console.ResetColor();
    return;
}

// Extract from HTML
string ExtractGroup(string pattern, int index = 1) =>
    Regex.Match(vehicleHtml, pattern, RegexOptions.Singleline) is var m && m.Success ? m.Groups[index].Value.Trim() : "N/A";

string ExtractMultiGroup(string pattern, int index) =>
    Regex.Matches(vehicleHtml, pattern, RegexOptions.Singleline) is var matches && matches.Count > index ? matches[index].Groups[1].Value.Trim() : "N/A";

var route = ExtractGroup(@"<h3>(\d+)</h3>");
var plate = ExtractGroup(@"<span class=""vehicle-information__plate"">(.+?)</span>");
var reg = ExtractMultiGroup(@"<span class=""vehicle-information__plate"">(.+?)</span>", 1);
var cap1 = ExtractGroup(@"<p class=""capacity-notice__count"">(.+?)</p>");
var cap2 = ExtractGroup(@"<p class=""capacity-notice__count"">.+?</p>\s*<p>(.+?)</p>");

// Step 2: Get vehicle metadata from JSON
var jsonUrl = $"https://www.cardiffbus.com/_ajax/lines/vehicles?lines[0]=CB:{route}";
string json;

try
{
    json = await baseClient.GetStringAsync(jsonUrl);
}
catch (HttpRequestException)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Error: Failed to retrieve additional vehicle metadata.");
    Console.ResetColor();
    return;
}

using var doc = JsonDocument.Parse(json);

var props = doc.RootElement
    .GetProperty("features")[0]
    .GetProperty("properties");

var meta = props.GetProperty("meta");

string FormatType(string rawType)
{
    var spaced = Regex.Replace(rawType, "(\\B[A-Z])", " $1");
    return char.ToUpper(spaced[0]) + spaced[1..];
}

var type = FormatType(meta.GetProperty("type").GetString() ?? "");
var make = meta.GetProperty("make").GetString() ?? "N/A";
var model = meta.GetProperty("model").GetString() ?? "N/A";
var direction = props.GetProperty("direction").GetString() ?? "N/A";

// Colour helper
void WriteField(string label, string value, ConsoleColor color)
{
    Console.ForegroundColor = ConsoleColor.Gray;
    Console.Write($"{label}: ");
    Console.ForegroundColor = color;
    Console.WriteLine(value);
    Console.ResetColor();
}

// Output
WriteField("Fleet No", plate, ConsoleColor.Cyan);
WriteField("Reg No", reg, ConsoleColor.Cyan);
Console.WriteLine();
WriteField("Type", type, ConsoleColor.Green);
WriteField("Make", make, ConsoleColor.Green);
WriteField("Model", model, ConsoleColor.Green);
Console.WriteLine();
WriteField("Route", route, ConsoleColor.Yellow);
WriteField("Direction", char.ToUpper(direction[0]) + direction[1..], ConsoleColor.Blue);
WriteField("Capacity", $"{cap1}, {cap2}", ConsoleColor.Magenta);
