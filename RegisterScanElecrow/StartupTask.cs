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
namespace devMobile.IoT.Rfm9x.RegisterScanElecrow
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Diagnostics;
	using System.Threading.Tasks;
	using Windows.ApplicationModel.Background;
	using Windows.Devices.Enumeration;
	using Windows.Devices.Gpio;
	using Windows.Devices.Spi;


	public sealed class Rfm9XDevice : IDisposable
	{
		private SpiDevice rfm9XLoraModem;
		private GpioPin chipSelectGpioPin;

		public Rfm9XDevice(int chipSelectPin)
		{
			SpiController spiController = SpiController.GetDefaultAsync().AsTask().GetAwaiter().GetResult();
			GpioController gpioController = GpioController.GetDefault();
			chipSelectGpioPin = gpioController.OpenPin(chipSelectPin);
			chipSelectGpioPin.SetDriveMode(GpioPinDriveMode.Output);
			chipSelectGpioPin.Write(GpioPinValue.High);

			var settings = new SpiConnectionSettings(chipSelectPin)
			{
				ClockFrequency = 500000,
				Mode = SpiMode.Mode1,
			};
			rfm9XLoraModem = spiController.GetDevice(settings);
		}

		public Rfm9XDevice(byte chipSelectLine, string dummy)
		{
			SpiController spiController = SpiController.GetDefaultAsync().AsTask().GetAwaiter().GetResult();

			var settings = new SpiConnectionSettings(chipSelectLine)
			{
				ClockFrequency = 500000,
				Mode = SpiMode.Mode3,
			};
			rfm9XLoraModem = spiController.GetDevice(settings);
		}


		public Byte RegisterReadByte(byte registerAddress)
		{
			byte[] writeBuffer = new byte[] { registerAddress };
			byte[] readBuffer = new byte[1];
			Debug.Assert(rfm9XLoraModem != null);

			if (chipSelectGpioPin != null)
			{
				chipSelectGpioPin.Write(GpioPinValue.Low);
			}
			rfm9XLoraModem.Write(writeBuffer);
			rfm9XLoraModem.Read(readBuffer);
			//rfm9XLoraModem.TransferSequential(writeBuffer, readBuffer);
			if (chipSelectGpioPin != null)
			{
				chipSelectGpioPin.Write(GpioPinValue.High);
			}

			return readBuffer[0];
		}

		public void Dispose()
		{
			if (chipSelectGpioPin != null)
			{
				chipSelectGpioPin.Dispose();
			}
			if (rfm9XLoraModem!=null)
			{
				rfm9XLoraModem.Dispose();
			}
		}
		
	}

	/*

					for (byte registerIndex = 0; registerIndex <= 0x42; registerIndex++)
				{
					byte registerValue = rfm9XDevice.RegisterReadByte(registerIndex);

	Debug.WriteLine("Register 0x{0:x2} - Value 0X{1:x2} - Bits {2}", registerIndex, registerValue, Convert.ToString(registerValue, 2).PadLeft(8, '0'));
				}
				*/


	public sealed class StartupTask : IBackgroundTask
	{
		//private const int ChipSelectLine = 7;
		//private Rfm9XDevice rfm9XDevice = new Rfm9XDevice(ChipSelectLine);

		public void Run(IBackgroundTaskInstance taskInstance)
		{
			GpioController gpioController = GpioController.GetDefault();
			// Reset pin configuration then strobe briefly to factory reset
			GpioPin resetGpioPin = gpioController.OpenPin(25);
			resetGpioPin.SetDriveMode(GpioPinDriveMode.Output);
			resetGpioPin.Write(GpioPinValue.Low);
			Task.Delay(10);
			resetGpioPin.Write(GpioPinValue.High);
			Task.Delay(10);
			
			for (int pinNumber = 0; pinNumber < 28; pinNumber++)
			{
				try
				{
					using (Rfm9XDevice rfm9XDevice = new Rfm9XDevice(pinNumber))
					{
						byte version = rfm9XDevice.RegisterReadByte(0x42);

						Debug.WriteLine("Pin {0} Vesion 0X{1:x2} - Bits {2}", pinNumber, version, Convert.ToString(version, 2).PadLeft(8, '0'));

						Task.Delay(500).GetAwaiter().GetResult();
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine("-------Pin open {0} failed {1}", pinNumber, ex.Message);
				}
			}
			
			for (byte chipSelectLine = 0; chipSelectLine < 2; chipSelectLine++)
			{
				try
				{
					using (Rfm9XDevice rfm9XDevice = new Rfm9XDevice(chipSelectLine, ""))
					{
						byte version = rfm9XDevice.RegisterReadByte(0x42);

						Debug.WriteLine("Pin {0} Vesion 0X{1:x2} - Bits {2}", chipSelectLine, version, Convert.ToString(version, 2).PadLeft(8, '0'));

						Task.Delay(500).GetAwaiter().GetResult();
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine("-------Pin open {0} failed {1}", chipSelectLine, ex.Message);
				}
			}
		}
	}
}


/*
GpioPin led = gpioController.OpenPin(pinNumber);
led.SetDriveMode(GpioPinDriveMode.Output);
Debug.WriteLine("+++++++Pin open {0}", pinNumber);

for (int flashcount = 0; flashcount < 10; flashcount++)
{
	if (led.Read() == GpioPinValue.High)
	{
		led.Write(GpioPinValue.Low);
	}
	else
	{
		led.Write(GpioPinValue.High);
	}
	Task.Delay(500).GetAwaiter().GetResult();
}
*/
