using System;
using System.Collections.Generic;
using System.Text;

namespace DevicePilot.Api
{
    /// <summary>
    /// Specifies a property/field which acts as the device timestamp.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class DeviceTimestampAttribute : Attribute
    {
        /// <summary>
        /// Creates a new device timestamp attribute.
        /// </summary>
        public DeviceTimestampAttribute() {
        }
    }
}
