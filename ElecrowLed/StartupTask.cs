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
namespace devMobile.IoT.Rfm9x.ElecrowLed
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
			GpioPin yellowLedPin = gpioController.OpenPin(23);
			yellowLedPin.SetDriveMode(GpioPinDriveMode.Output);

			while (true)
			{
				if (yellowLedPin.Read() == GpioPinValue.High)
				{
					yellowLedPin.Write(GpioPinValue.Low);
				}
				else
				{
					yellowLedPin.Write(GpioPinValue.High);
				}
				Thread.Sleep(500);
			}
		}
	}
}
