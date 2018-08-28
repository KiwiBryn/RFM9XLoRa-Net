//---------------------------------------------------------------------------------
// Copyright (c) July 2018, devMobile Software
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
//---------------------------------------------------------------------------------
namespace devMobile.IoT.Rfm9x.RegisterRead
{
	using System;
	using System.Diagnostics;
	using System.Threading.Tasks;
	using Windows.ApplicationModel.Background;
	using Windows.Devices.Gpio;
	using Windows.Devices.Spi;

	public sealed class StartupTask : IBackgroundTask
	{
		private const int ChipSelectLine = 25;
		private SpiDevice rfm9XLoraModem;

		public void Run(IBackgroundTaskInstance taskInstance)
		{
			// Have to setup the SPI bus with custom Chip Select line rather than std CE0/CE1
			SpiController spiController = SpiController.GetDefaultAsync().AsTask().GetAwaiter().GetResult();
			var settings = new SpiConnectionSettings(0)
			{
				ClockFrequency = 500000,
				Mode = SpiMode.Mode0,
			};

			GpioController gpioController = GpioController.GetDefault();
			GpioPin chipSelectGpioPin = gpioController.OpenPin(ChipSelectLine);
			chipSelectGpioPin.SetDriveMode(GpioPinDriveMode.Output);
			chipSelectGpioPin.Write(GpioPinValue.High);

			rfm9XLoraModem = spiController.GetDevice(settings);

			while (true)
			{
				byte[] writeBuffer = new byte[]{ 0x42 }; // RegVersion
				byte[] readBuffer = new byte[1];

				chipSelectGpioPin.Write(GpioPinValue.Low);
				rfm9XLoraModem.Write(writeBuffer);
				rfm9XLoraModem.Read(readBuffer);
				chipSelectGpioPin.Write(GpioPinValue.High);

				Debug.WriteLine("RegVersion {0:x2}", readBuffer[0]);

				Task.Delay(10000).Wait();
			}
		}
	}
}
