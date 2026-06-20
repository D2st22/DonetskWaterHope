namespace ProjectsDonetskWaterHope.Models
{
    public class IotDeviceStatus
    {
        public int IotDeviceStatusId { get; set; }

        public int DeviceId { get; set; }
        public Device Device { get; set; } = null!;

        public int RawSensorValue { get; set; }
        public int FlowRate { get; set; }
        public int TotalCounter { get; set; }
        public bool LeakageDetected { get; set; }
        public int? WifiRssi { get; set; }
        public string FirmwareVersion { get; set; } = "unknown";
        public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    }
}
