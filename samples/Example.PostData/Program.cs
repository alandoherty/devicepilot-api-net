// Licensed under the MIT License. See LICENSE in the project root for license information.

using DevicePilot.Api;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Example.PostData
{
    class Program
    {
        static async Task Main(string[] args) {
            ApiClient apiClient = new ApiClient(Environment.GetEnvironmentVariable("DP_TOKEN"));

            List<DeviceData> datas = new List<DeviceData>();
            Random r = new Random();

            await apiClient.IngestAsync<OutletDevice>(new OutletDevice() {
                Id = "outlet1",
                Latitude = 53.70076d,
                Longitude = -2.28442d
            });
        }
    }
}
