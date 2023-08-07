using System.Text.Json.Serialization;

namespace OptechX.Library.Drivers.UpdateTool.Models
{
	public class DriverCore
	{
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("uid")]
        public string? UID { get; set; }

        [JsonPropertyName("oem")]
        public string? Oem { get; set; }

        [JsonPropertyName("make")]
        public string? Make { get; set; }

        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("lastUpdated")]
        public DateTime LastUpdated { get; set; }

        [JsonPropertyName("supportedWinRelease")]
        public List<string>? SupportedWinRelease { get; set; }

        public void AddNewSupportedWinRelease(DriverCore nDriverCore)
        {
            if (SupportedWinRelease == null)
            {
                SupportedWinRelease = nDriverCore.SupportedWinRelease!.ToList();
            }
            else if (nDriverCore.SupportedWinRelease != null)
            {
                SupportedWinRelease.AddRange(nDriverCore.SupportedWinRelease.Except(SupportedWinRelease));
            }
        }
    }
}

