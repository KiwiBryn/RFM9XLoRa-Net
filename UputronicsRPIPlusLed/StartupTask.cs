//---------------------------------------------------------------------------------
// Copyright (c) September 2018, devMobile Software
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
namespace devMobile.IoT.Rfm9x.UputronicsRPIPlusLed
{
	using System;
	using System.Threading;
	using Windows.ApplicationModel.Background;
	using Windows.Devices.Gpio;

	public sealed class StartupTask : IBackgroundTask
	{
		public void Run(IBackgroundTaskInstance taskInstance)
		{
			GpioController gpioController = GpioController.GetDefault();

			GpioPin ce01LedPin = gpioController.OpenPin(5);
			ce01LedPin.SetDriveMode(GpioPinDriveMode.Output);
			ce01LedPin.Write(GpioPinValue.Low);

			GpioPin ceo2LedPin = gpioController.OpenPin(21);
			ceo2LedPin.SetDriveMode(GpioPinDriveMode.Output);
			ceo2LedPin.Write(GpioPinValue.High);

			GpioPin lanLedPin = gpioController.OpenPin(6);
			lanLedPin.SetDriveMode(GpioPinDriveMode.Output);
			lanLedPin.Write(GpioPinValue.Low);

			GpioPin internetLedPin = gpioController.OpenPin(13);
			internetLedPin.SetDriveMode(GpioPinDriveMode.Output);
			internetLedPin.Write(GpioPinValue.High);

			while (true)
			{
				if (ce01LedPin.Read() == GpioPinValue.High)
				{
					ce01LedPin.Write(GpioPinValue.Low);
				}
				else
				{
					ce01LedPin.Write(GpioPinValue.High);
				}

				if (ceo2LedPin.Read() == GpioPinValue.High)
				{
					ceo2LedPin.Write(GpioPinValue.Low);
				}
				else
				{
					ceo2LedPin.Write(GpioPinValue.High);
				}

				if (lanLedPin.Read() == GpioPinValue.High)
				{
					lanLedPin.Write(GpioPinValue.Low);
				}
				else
				{
					lanLedPin.Write(GpioPinValue.High);
				}

				if (internetLedPin.Read() == GpioPinValue.High)
				{
					internetLedPin.Write(GpioPinValue.Low);
				}
				else
				{
					internetLedPin.Write(GpioPinValue.High);
				}

				Thread.Sleep(500);
			}
		}
	}
}

