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
namespace devMobile.IoT.Rfm9x.RegisterScan
{
	using System;
	using System.Diagnostics;
	using System.Threading.Tasks;
	using Windows.ApplicationModel.Background;
	using Windows.Devices.Gpio;
	using Windows.Devices.Spi;

	public sealed class Rfm9XDevice
	{
		private SpiDevice rfm9XLoraModem;
		private GpioPin chipSelectGpioPin;

		public Rfm9XDevice(int chipSelectPin)
		{
			SpiController spiController = SpiController.GetDefaultAsync().AsTask().GetAwaiter().GetResult();
			var settings = new SpiConnectionSettings(0)
			{
				ClockFrequency = 500000,
				Mode = SpiMode.Mode0,
			};

			GpioController gpioController = GpioController.GetDefault();
			chipSelectGpioPin = gpioController.OpenPin(chipSelectPin);
			chipSelectGpioPin.SetDriveMode(GpioPinDriveMode.Output);
			chipSelectGpioPin.Write(GpioPinValue.High);

			rfm9XLoraModem = spiController.GetDevice(settings);
		}

		public Byte RegisterReadByte(byte registerAddress)
		{
			byte[] writeBuffer = new byte[] { registerAddress };
			byte[] readBuffer = new byte[1];
			Debug.Assert(rfm9XLoraModem != null);

			chipSelectGpioPin.Write(GpioPinValue.Low);
			rfm9XLoraModem.Write(writeBuffer);
			rfm9XLoraModem.Read(readBuffer);
			chipSelectGpioPin.Write(GpioPinValue.High);

			return readBuffer[0];
		}
	}


	public sealed class StartupTask : IBackgroundTask
	{
		private const int ChipSelectLine = 25;
		private Rfm9XDevice rfm9XDevice = new Rfm9XDevice(ChipSelectLine);

		public void Run(IBackgroundTaskInstance taskInstance)
		{
			while (true)
			{
				for (byte registerIndex = 0; registerIndex <= 0x42; registerIndex++)
				{
					byte registerValue = rfm9XDevice.RegisterReadByte(registerIndex);

					Debug.WriteLine("Register 0x{0:x2} - Value 0X{1:x2} - Bits {2}", registerIndex, registerValue, Convert.ToString(registerValue, 2).PadLeft(8, '0'));
				}

				Task.Delay(10000).Wait();
			}
		}
	}
}
