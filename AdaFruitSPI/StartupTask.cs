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

	 CS : CE1
	 RST : GPIO25
	 IRQ : GPIO22 (DIO0)
	 Unused : GPIO23 (DIO1)
	 Unused : GPIO24 (DIO2)
 */
namespace devMobile.IoT.Rfm9x.AdafruitSPI
{
	using System;
	using System.Diagnostics;
	using System.Threading;
	using Windows.ApplicationModel.Background;
	using Windows.Devices.Spi;

	public sealed class StartupTask : IBackgroundTask
	{
		public void Run(IBackgroundTaskInstance taskInstance)
		{
			SpiController spiController = SpiController.GetDefaultAsync().AsTask().GetAwaiter().GetResult();
			var settings = new SpiConnectionSettings(1)
			{
				ClockFrequency = 500000,
				Mode = SpiMode.Mode0,   // From SemTech docs pg 80 CPOL=0, CPHA=0
			};

			SpiDevice Device = spiController.GetDevice(settings);

			while (true)
			{
				byte[] writeBuffer = new byte[] { 0x42 }; // RegVersion
				byte[] readBuffer = new byte[1];

				Device.TransferSequential(writeBuffer, readBuffer);

				byte registerValue = readBuffer[0];
				Debug.WriteLine("Register 0x{0:x2} - Value 0X{1:x2} - Bits {2}", 0x42, registerValue, Convert.ToString(registerValue, 2).PadLeft(8, '0'));

				Thread.Sleep(10000);
			}
		}
	}
}
