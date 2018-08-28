//---------------------------------------------------------------------------------
// Copyright (c) August 2018, devMobile Software
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
namespace devMobile.IoT.Rfm9x.UputronicsSPI
{
	using System;
	using System.Diagnostics;
	using System.Threading;
	using Windows.ApplicationModel.Background;
	using Windows.Devices.Gpio;
	using Windows.Devices.Spi;

	public sealed class StartupTask : IBackgroundTask
	{
		public void Run(IBackgroundTaskInstance taskInstance)
		{
			GpioPin ChipSelectGpioPin = null;
			const int chipSelectPinNumber = 25;

			SpiController spiController = SpiController.GetDefaultAsync().AsTask().GetAwaiter().GetResult();
			var settings = new SpiConnectionSettings(1)
			{
				ClockFrequency = 500000,
				Mode = SpiMode.Mode0,   // From SemTech docs pg 80 CPOL=0, CPHA=0
			};

			// Chip select pin configuration
			GpioController gpioController = GpioController.GetDefault();
			ChipSelectGpioPin = gpioController.OpenPin(chipSelectPinNumber);
			ChipSelectGpioPin.SetDriveMode(GpioPinDriveMode.Output);
			ChipSelectGpioPin.Write(GpioPinValue.High);

			SpiDevice Device = spiController.GetDevice(settings);

			while (true)
			{
				byte[] writeBuffer = new byte[] { 0x42 }; // RegVersion
				byte[] readBuffer = new byte[1];

				// Read the RegVersion silicon ID to check SPI works
				ChipSelectGpioPin.Write(GpioPinValue.Low);
				Device.TransferSequential(writeBuffer, readBuffer);
				ChipSelectGpioPin.Write(GpioPinValue.High);
				Debug.WriteLine("Register RegVer 0x{0:x2} - Value 0X{1:x2} - Bits {2}", writeBuffer[0], readBuffer[0], Convert.ToString(readBuffer[0], 2).PadLeft(8, '0'));

				Thread.Sleep(10000);
			}
		}
	}
}

