namespace OptechX.Library.Drivers.UpdateTool.Models
{
	public class DriverCore
	{
        public int Id { get; set; }
        public string? UID { get; set; }
        public string? Oem { get; set; }
        public string? Make { get; set; }
        public string? Model { get; set; }
        public DateTime LastUpdated { get; set; }
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

