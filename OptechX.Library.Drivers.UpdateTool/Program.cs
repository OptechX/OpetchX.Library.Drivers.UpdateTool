﻿using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;
using OptechX.Library.Drivers.UpdateTool.Models;

namespace OptechX.Library.Drivers.UpdateTool
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Any(arg => arg.Contains("--version")))
            {
                Console.WriteLine("Version: 1.0.1");
                return;
            }

            if (args.Any(arg => arg.Contains("--help")))
            {
                Console.WriteLine("Usage: oxldut [--csv <csv_file>]|[--xml <xml_file>] --oem <oem>");
                return;
            }

            if (args.Length != 4)
            {
                Console.WriteLine("Usage: oxldut [--csv <csv_file>]|[--xml <xml_file>] --oem <oem>");
                return;
            }

            string? csvFilePath = null;
            string? xmlFilePath = null;
            string? oem = null;
            string apiEndpoint = "https://definitely-firm-chamois.ngrok-free.app";

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--csv" && i + 1 < args.Length)
                {
                    csvFilePath = args[i + 1];
                }
                else if (args[i] == "--xml" && i + 1 < args.Length)
                {
                    xmlFilePath = args[i + 1];
                }
                else if (args[i] == "--oem" && i + 1 < args.Length)
                {
                    oem = args[i + 1];
                }
            }

            // Check if both CSV and XML files are specified (optional)
            if (csvFilePath != null && xmlFilePath != null)
            {
                Console.WriteLine("Please provide either a CSV file or an XML file, not both.");
                return;
            }

            // Check if CSV or XML file is specified
            if (csvFilePath == null && xmlFilePath == null)
            {
                Console.WriteLine("Please provide either a CSV file or an XML file.");
                return;
            }

            // Check if OEM is specified
            if (oem == null)
            {
                Console.WriteLine("Please specify the OEM name using the --oem argument.");
                return;
            }

            // Now you have the csvFilePath, xmlFilePath, and oem values.
            // You can use them in your update tool logic.

            // work with csvFilePath
            if (!string.IsNullOrEmpty(csvFilePath))
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    if (!File.Exists(csvFilePath))
                    {
                        Console.WriteLine($"CSV file not exist: {csvFilePath}");
                        return;
                    }

                    // set windows version
                    string windowsVersion = csvFilePath switch
                    {
                        var s when s.Contains("win10") => "Windows 10",
                        var s when s.Contains("win11") => "Windows 11",
                        _ => "Windows 11"
                    };

                    Console.WriteLine($"Windows Version: {windowsVersion}");
                    Console.WriteLine($"OEM: {oem}");

                    // start main work here
                    using (TextFieldParser parser = new(csvFilePath))
                    {
                        parser.TextFieldType = FieldType.Delimited;
                        parser.SetDelimiters(",");

                        while (!parser.EndOfData)
                        {
                            string[] columns = parser.ReadFields()!;
                            string make;
                            string model;
                            string updated;
                            try
                            {
                                make = columns[0];
                                model = columns[1];
                                updated = columns[2];    
                            }
                            catch
                            {
                                continue;
                            }
                            if (make == "Make" && model == "Model" && updated == "Updated")
                            {
                                continue;
                            }

                            Console.WriteLine($"Make: {make}, Model: {model}, Updated: {updated}");

                            string thisUid = $"{oem}::{make}::{model}";

                            // Define the pattern for characters to be replaced
                            string pattern = @"[^a-zA-Z_0-9:]";

                            // Replace spaces with underscores
                            thisUid = thisUid.Replace(" ", "_");

                            // Replace characters that match the pattern with an empty string
                            thisUid = Regex.Replace(thisUid, pattern, "");

                            DriverCore thisDriverCore = new()
                            {
                                Id = 0,
                                UID = thisUid,
                                LastUpdated = DateTime.UtcNow,
                                Make = make,
                                Model = model,
                                Oem = oem,
                                SupportedWinRelease = new List<string>() { windowsVersion },
                            };

                            Console.WriteLine($"Check: {apiEndpoint}/api/DriverCore/uid/{thisUid}");

                            try
                            {
                                HttpResponseMessage response = await httpClient.GetAsync($"{apiEndpoint}/api/DriverCore/uid/{thisUid}");
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

                                DateTime dateTimeLastUpdated = DateTime.ParseExact(updated, "MM/dd/yyyy", null);

                                DriverCore uDriverCore = new DriverCore()
                                {
                                    Id = drivers[0].Id,
                                    UID = thisUid,
                                    Oem = oem,
                                    Make = make,
                                    Model = model,
                                    LastUpdated = dateTimeLastUpdated,
                                };
                                uDriverCore.AddNewSupportedWinRelease(thisDriverCore);

                                string apiUpdateDriverCoreUrl = $"{apiEndpoint}/api/DriverCore/{uDriverCore.Id}";

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
                                    else if (updateResponse.StatusCode == System.Net.HttpStatusCode.NoContent)
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

                                string apiNewDriverCoreUrl = $"{apiEndpoint}/api/DriverCore";

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
