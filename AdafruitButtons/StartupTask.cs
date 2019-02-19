/*
    Copyright ® 2019 Feb devMobile Software, All Rights Reserved
 
    MIT License

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE

	 Adafruit documentation page
	 https://learn.adafruit.com/adafruit-radio-bonnets/pinouts

    Button 1: GPIO 5 
    Button 2: GPIO 6
    Button 3: GPIO 12 

 */
namespace devMobile.IoT.Rfm9x.AdafruitButtons
{
	using System;
	using System.Diagnostics;
	using Windows.ApplicationModel.Background;
	using Windows.Devices.Gpio;

	public sealed class StartupTask : IBackgroundTask
    {
		private BackgroundTaskDeferral backgroundTaskDeferral = null;
		private GpioPin InterruptGpioPin1 = null;
		private GpioPin InterruptGpioPin2 = null;
		private GpioPin InterruptGpioPin3 = null;
		private const int InterruptPinNumber1 = 5;
		private const int InterruptPinNumber2 = 6;
		private const int InterruptPinNumber3 = 12;

		public void Run(IBackgroundTaskInstance taskInstance)
        {
			Debug.WriteLine("Application startup");

			try
			{
				GpioController gpioController = GpioController.GetDefault();

				InterruptGpioPin1 = gpioController.OpenPin(InterruptPinNumber1);
				InterruptGpioPin1.SetDriveMode(GpioPinDriveMode.InputPullUp);
				InterruptGpioPin1.ValueChanged += InterruptGpioPin_ValueChanged; ;

				InterruptGpioPin2 = gpioController.OpenPin(InterruptPinNumber2);
				InterruptGpioPin2.SetDriveMode(GpioPinDriveMode.InputPullUp);
				InterruptGpioPin2.ValueChanged += InterruptGpioPin_ValueChanged; ;

				InterruptGpioPin3 = gpioController.OpenPin(InterruptPinNumber3);
				InterruptGpioPin3.SetDriveMode(GpioPinDriveMode.InputPullUp);
				InterruptGpioPin3.ValueChanged += InterruptGpioPin_ValueChanged; ;

				Debug.WriteLine("Digital Input Interrupt configuration success");
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Digital Input Interrupt configuration failed " + ex.Message);
				return;
			}

			//enable task to continue running in background
			backgroundTaskDeferral = taskInstance.GetDeferral();
		}

		private void InterruptGpioPin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
		{
			Debug.WriteLine($"Digital Input Interrupt {sender.PinNumber} triggered {args.Edge}");
		}
	}
}
