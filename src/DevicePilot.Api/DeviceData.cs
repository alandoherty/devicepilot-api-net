using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DevicePilot.Api
{
    /// <summary>
    /// Represents device data.
    /// </summary>
    public struct DeviceData
    {
        /// <summary>
        /// Defines the list of accepted property types.
        /// </summary>
        internal static readonly Type[] ValidPropertyTypes = new Type[] {
            typeof(string),
            typeof(bool),
            typeof(byte),
            typeof(sbyte),
            typeof(ushort),
            typeof(short),
            typeof(uint),
            typeof(int),
            typeof(ulong),
            typeof(long),
            typeof(float),
            typeof(double),
            typeof(decimal),
            typeof(DateTime),
            typeof(byte[]),
            typeof(Type),
            typeof(Guid),
        };

        /// <summary>
        /// Gets or sets the device ID.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the properties.
        /// </summary>
        public IReadOnlyDictionary<string, object> Properties { get; set; }

        /// <summary>
        /// Gets or sets the optional timestamp.
        /// </summary>
        public DateTime? Timestamp { get; set; }

        /// <summary>
        /// Gets the structure as a <see cref="JObject"/>.
        /// </summary>
        /// <returns>The <see cref="JObject"/> instance.</returns>
        public JObject AsJObject() {
            JObject obj = new JObject();
            obj["$id"] = Id;

            if (Timestamp != null)
                obj["$ts"] = Timestamp.Value;

            foreach (var kv in Properties)
                obj[kv.Key] = JToken.FromObject(kv.Value);

            return obj;
        }

        /// <summary>
        /// Throws an exception if the contents of this structure are invalid.
        /// </summary>
        internal void ThrowIfInvalid() {
            // check if the id is null
            if (Id == null)
                throw new ArgumentNullException(nameof(Id), "The device id cannot be null");

            // check if the timestamp is in the future
            if (Timestamp != null && Timestamp > DateTimeOffset.Now)
                throw new ArgumentException("The timestamp cannot be in the future");

            // check if the properties is not null and are valid types
            if (Properties == null)
                throw new ArgumentNullException(nameof(Properties), "The device properties cannot be null");

            foreach (var kv in Properties) {
                if (string.IsNullOrEmpty(kv.Key))
                    throw new FormatException("The device property name cannot be empty or null");

                if (!ValidPropertyTypes.Contains(kv.Value.GetType()))
                    throw new FormatException($"The type {kv.Value.GetType().FullName} is not supported for device property `{kv.Key}`");
            }
        }
    }
}
