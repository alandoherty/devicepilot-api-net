using System;
using System.Collections.Generic;
using System.Text;

namespace DevicePilot.Api
{
    /// <summary>
    /// Specifies a property/field which acts as the device ID.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class DeviceIdAttribute : Attribute
    {
        /// <summary>
        /// Creates a new device id attribute.
        /// </summary>
        public DeviceIdAttribute() {
        }
    }
}
