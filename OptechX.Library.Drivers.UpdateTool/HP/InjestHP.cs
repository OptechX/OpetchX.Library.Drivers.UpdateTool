using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;
using OptechX.Library.Drivers.UpdateTool.Models;

namespace OptechX.Library.Drivers.UpdateTool.HP
{
	public static class InjestHP
	{
		public static async Task UpdateHP(string csvPath, string ApiEndpoint)
		{
			if (!string.IsNullOrEmpty(csvPath))
			{
				if (!File.Exists(csvPath))
				{
					Console.WriteLine($"CSV file not exists: {csvPath}");
                    return;
				}

                using (HttpClient httpClient = new HttpClient())
				{
                    // read the CSV from here
                    using (TextFieldParser parser = new TextFieldParser(csvPath))
                    {
                        parser.TextFieldType = FieldType.Delimited;
                        parser.SetDelimiters(",");

                        while (!parser.EndOfData)
                        {
                            string[] columns = parser.ReadFields()!;
                            string make;
                            string series;
                            string model;
                            string win7;
                            string win8;
                            string win10;
                            string win11;
                            try
                            {
                                make = columns[0];
                                series = columns[1];
                                model = columns[2];
                                win7 = columns[3];
                                win8 = columns[4];
                                win10 = columns[5];
                                win11 = columns[6];
                            }
                            catch
                            {
                                Console.WriteLine("Incorrect columns in HP CSV");
                                return;
                            }

                            if (make == "Make")
                            {
                                continue;
                            }

                            string thisUid = $"{make}::{series}::{model}";

                            // Define the pattern for characters to be replaced
                            string pattern = @"[^a-zA-Z_0-9:]";

                            // Replace spaces with underscores
                            thisUid = thisUid.Replace(" ", "_");

                            // Replace characters that match the pattern with an empty string
                            thisUid = Regex.Replace(thisUid, pattern, "");

                            List<DriverCore> theseDriverCores = new List<DriverCore>();

                            if (win10 == "Yes")
                            {
                                DriverCore thisDriverCore = new()
                                {
                                    Id = 0,
                                    UID = thisUid,
                                    LastUpdated = DateTime.UtcNow,
                                    Make = series,
                                    Model = model,
                                    Oem = make,
                                    SupportedWinRelease = new List<string>() { "Windows 10" },
                                };

                                theseDriverCores.Add(thisDriverCore);
                            }

                            if (win11 == "Yes")
                            {
                                DriverCore thisDriverCore = new()
                                {
                                    Id = 0,
                                    UID = thisUid,
                                    LastUpdated = DateTime.UtcNow,
                                    Make = series,
                                    Model = model,
                                    Oem = make,
                                    SupportedWinRelease = new List<string>() { "Windows 11" },
                                };

                                theseDriverCores.Add(thisDriverCore);
                            }

                            foreach (DriverCore thisDriverCore in theseDriverCores)
                            {
                                try
                                {
                                    HttpResponseMessage response = await httpClient.GetAsync($"{ApiEndpoint}/api/DriverCore/uid/{thisDriverCore.UID}");
                                    response.EnsureSuccessStatusCode();

                                    Console.WriteLine($"Found UID: {thisDriverCore.UID}");

                                    // read the response as a string
                                    string responseContent = await response.Content.ReadAsStringAsync();

                                    // deserialize
                                    List<DriverCore> drivers = JsonSerializer.Deserialize<List<DriverCore>>(responseContent)!;

                                    if (drivers.Count > 1)
                                    {
                                        Console.WriteLine("More than 1 driverCore error, needs to be investigated");
                                        continue;
                                    }

                                    if (drivers.Count < 1)
                                    {
                                        Console.WriteLine("Less than 1 driverCore error, needs to be investigated");
                                        continue;
                                    }

                                    DriverCore uDriverCore = new DriverCore()
                                    {
                                        Id = drivers[0].Id,
                                        UID = thisUid,
                                        Oem = series,
                                        Make = make,
                                        Model = model,
                                        LastUpdated = DateTime.UtcNow,
                                    };
                                    uDriverCore.AddNewSupportedWinRelease(thisDriverCore);

                                    string apiUpdateDriverCoreUrl = $"{ApiEndpoint}/api/DriverCore/{uDriverCore.Id}";

                                    try
                                    {
                                        HttpResponseMessage updateResponse = await httpClient.PutAsync(apiUpdateDriverCoreUrl, new StringContent(JsonSerializer.Serialize(uDriverCore), Encoding.UTF8, "application/json"));

                                        if (updateResponse.IsSuccessStatusCode)
                                        {
                                            Console.ForegroundColor = ConsoleColor.Green;
                                            Console.Write("DriverCore updated: ");
                                            Console.ForegroundColor = ConsoleColor.Yellow;
                                            Console.WriteLine(uDriverCore.UID);
                                            Console.ResetColor();
                                        }
                                        else if (updateResponse.StatusCode == HttpStatusCode.NoContent)
                                        {
                                            Console.ForegroundColor = ConsoleColor.Green;
                                            Console.Write("DriverCore updated: ");
                                            Console.ForegroundColor = ConsoleColor.Yellow;
                                            Console.WriteLine(uDriverCore.UID);
                                            Console.ResetColor();
                                        }
                                        else
                                        {
                                            Console.WriteLine($"Error: {updateResponse.StatusCode}");
                                            Console.WriteLine($"Unable to update DriverCore: {uDriverCore.UID}");
                                            Console.WriteLine($"uDriverCore: {JsonSerializer.Serialize(uDriverCore)}");
                                            continue;
                                        }
                                    }
                                    catch (HttpRequestException ex)
                                    {
                                        Console.WriteLine($"Error: {ex.Message}");
                                        Console.WriteLine($"Unable to update DriverCore: {uDriverCore.UID}");
                                        Console.WriteLine($"uDriverCore: {JsonSerializer.Serialize(uDriverCore)}");
                                        continue;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error: {ex.Message}");

                                    string apiNewDriverCoreUrl = $"{ApiEndpoint}/api/DriverCore";

                                    try
                                    {
                                        HttpResponseMessage newRecordResponse = await httpClient.PostAsync(apiNewDriverCoreUrl, new StringContent(JsonSerializer.Serialize(thisDriverCore), Encoding.UTF8, "application/json"));
                                        newRecordResponse.EnsureSuccessStatusCode();
                                        Console.ForegroundColor = ConsoleColor.Cyan;
                                        Console.Write("Posted new DriverCore: ");
                                        Console.ForegroundColor = ConsoleColor.Yellow;
                                        Console.WriteLine(thisDriverCore.UID);
                                        Console.ResetColor();
                                    }
                                    catch (HttpRequestException ex2)
                                    {
                                        Console.WriteLine($"Error: {ex2.Message}");
                                        Console.WriteLine(JsonSerializer.Serialize(thisDriverCore));
                                    }

                                    continue;
                                }
                            }
                        }
                    }
                }
			}
		}
	}
}

