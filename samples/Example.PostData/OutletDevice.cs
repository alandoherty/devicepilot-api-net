using DevicePilot.Api;
using System;
using System.Collections.Generic;
using System.Text;

namespace Example.PostData
{
    public class OutletDevice
    {
        [DeviceId]
        public string Id { get; set; }

        [DeviceProperty]
        public double Latitude { get; set; }

        [DeviceProperty]
        public double Longitude { get; set; }

        [DeviceProperty("isOn")]
        public bool IsOn { get; set; }

        [DeviceTimestamp]
        public DateTime? Timestamp { get; set; }
    }
}
