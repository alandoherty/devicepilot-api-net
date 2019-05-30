using System;
using System.Collections.Generic;
using System.Text;

namespace DevicePilot.Api
{
    /// <summary>
    /// Specifies a property/field which acts as the device timestamp.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class DevicePropertyAttribute : Attribute
    {
        private string _name;

        /// <summary>
        /// Gets or sets the property name.
        /// </summary>
        public string Name {
            get {
                return _name;
            } set {
                _name = value;
            }
        }
         
        /// <summary>
        /// Creates a new device property attribute.
        /// </summary>
        public DevicePropertyAttribute() {
        }

        /// <summary>
        /// Creates a new device property attribute.
        /// </summary>
        /// <param name="name">The name.</param>
        public DevicePropertyAttribute(string name) {
            _name = name;
        }
    }
}
