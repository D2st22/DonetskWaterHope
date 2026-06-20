using System.ComponentModel.DataAnnotations;

namespace ProjectsDonetskWaterHope.DTOs
{
    public record UpdateIotStatusDto(
        [Required] int DeviceId,
        [Range(0, 4095)] int RawSensorValue,
        [Range(0, 1000)] int FlowRate,
        [Range(0, int.MaxValue)] int TotalCounter,
        bool LeakageDetected,
        int? WifiRssi,
        string? FirmwareVersion
    );

    public record IotDeviceStatusDto(
        int DeviceId,
        string DeviceName,
        string SerialNumber,
        string DeviceStatus,
        string AccountNumber,
        int RawSensorValue,
        int FlowRate,
        int TotalCounter,
        bool LeakageDetected,
        int? WifiRssi,
        string FirmwareVersion,
        DateTime LastSeenAt,
        bool IsOnline
    );
}
