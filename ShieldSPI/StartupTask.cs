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

namespace devMobile.IoT.Rfm9x.ShieldSpi
{
	using System;
	using System.Diagnostics;
	using System.Threading.Tasks;
	using Windows.ApplicationModel.Background;
	using Windows.Devices.Gpio;
	using Windows.Devices.Spi;

	public sealed class StartupTask : IBackgroundTask
	{
		public void Run(IBackgroundTaskInstance taskInstance)
		{
			GpioController gpioController = GpioController.GetDefault();
			GpioPin chipSelectGpioPin = null;

			//chipSelectGpioPin = gpioController.OpenPin(25); // DIY CS for Dragino
			if (chipSelectGpioPin != null)
			{
				chipSelectGpioPin.SetDriveMode(GpioPinDriveMode.Output);
				chipSelectGpioPin.Write(GpioPinValue.High);
			}

			SpiController spiController = SpiController.GetDefaultAsync().AsTask().GetAwaiter().GetResult();
			var settings = new SpiConnectionSettings(1)	// GPIO7 Elecrow shield
			//var settings = new SpiConnectionSettings(0) // GPI08 Electronic tricks
			{
				ClockFrequency = 500000,
				Mode = SpiMode.Mode0,   // From SemTech docs pg 80 CPOL=0, CPHA=0
				//Mode = SpiMode.Mode1,
				//Mode = SpiMode.Mode2,
				//Mode = SpiMode.Mode3,
				//SharingMode = SpiSharingMode.Shared,
				//SharingMode = SpiSharingMode.Exclusive,
			};

			SpiDevice Device = spiController.GetDevice(settings);

			Task.Delay(500).Wait();

			while (true)
			{
				byte[] writeBuffer = new byte[] { 0x42 };
				byte[] readBuffer = new byte[1];

				if (chipSelectGpioPin != null)
				{
					chipSelectGpioPin.Write(GpioPinValue.Low);
				}
				//Device.Write(writeBuffer);
				//Device.Read(readBuffer);
				Device.TransferSequential(writeBuffer, readBuffer);
				if (chipSelectGpioPin != null)
				{
					chipSelectGpioPin.Write(GpioPinValue.High);
				}

				byte registerValue = readBuffer[0];
				Debug.WriteLine("Register 0x{0:x2} - Value 0X{1:x2} - Bits {2}", 0x42, registerValue, Convert.ToString(registerValue, 2).PadLeft(8, '0'));

				Task.Delay(10000).Wait();
			}
		}
	}
}
