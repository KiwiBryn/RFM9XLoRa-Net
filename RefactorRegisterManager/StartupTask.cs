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
namespace devMobile.IoT.Rfm9x.RefactorRegisterManager
{
	using System;
	using System.Diagnostics;
	using System.Text;
	using System.Threading.Tasks;
	using Windows.ApplicationModel.Background;
	using Windows.Devices.Gpio;

	sealed class Rfm9XDevice
	{
		private GpioPin InterruptGpioPin = null;
		public RegisterManager RegisterManager = null; // Future refactor this will be made private

		public Rfm9XDevice(byte chipSelectPin, byte resetPin, byte interruptPin)
		{
			RegisterManager = new RegisterManager(chipSelectPin);

			// Setup the reset and interrupt pins
			GpioController gpioController = GpioController.GetDefault();

			// Reset pin configuration then strobe briefly to factory reset
			GpioPin resetGpioPin = gpioController.OpenPin(resetPin);
			resetGpioPin.SetDriveMode(GpioPinDriveMode.Output);
			resetGpioPin.Write(GpioPinValue.Low);
			Task.Delay(10);
			resetGpioPin.Write(GpioPinValue.High);
			Task.Delay(10);

			// Interrupt pin for RX message, TX done etc. notifications
			InterruptGpioPin = gpioController.OpenPin(interruptPin);
			resetGpioPin.SetDriveMode(GpioPinDriveMode.Input);

			InterruptGpioPin.ValueChanged += InterruptGpioPin_ValueChanged;
		}

		public void RegisterDump()
		{
			RegisterManager.Dump(0x0, 0x40);
		}

		private void InterruptGpioPin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
		{
			if (args.Edge != GpioPinEdge.RisingEdge)
			{
				return;
			}

			byte IrqFlags = this.RegisterManager.ReadByte(0x12); // RegIrqFlags
			Debug.WriteLine(string.Format("RegIrqFlags {0}", Convert.ToString(IrqFlags, 2).PadLeft(8, '0')));

			if ((IrqFlags & 0b01000000) == 0b01000000)  // RxDone 
			{
				Debug.WriteLine("Receive-Message");
				byte currentFifoAddress = this.RegisterManager.ReadByte(0x10); // RegFifiRxCurrent
				this.RegisterManager.WriteByte(0x0d, currentFifoAddress); // RegFifoAddrPtr

				byte numberOfBytes = this.RegisterManager.ReadByte(0x13); // RegRxNbBytes

				// Allocate buffer for message
				byte[] messageBytes = new byte[numberOfBytes];

				for (int i = 0; i < numberOfBytes; i++)
				{
					messageBytes[i] = this.RegisterManager.ReadByte(0x00); // RegFifo
				}

				string messageText = UTF8Encoding.UTF8.GetString(messageBytes);
				Debug.WriteLine("Received {0} byte message {1}", messageBytes.Length, messageText);
			}

			if ((IrqFlags & 0b00001000) == 0b00001000)  // TxDone
			{
				this.RegisterManager.WriteByte(0x01, 0b10000101); // RegOpMode set LoRa & RxContinuous
				Debug.WriteLine("Transmit-Done");
			}

			this.RegisterManager.WriteByte(0x40, 0b00000000); // RegDioMapping1 0b00000000 DI0 RxReady & TxReady
			this.RegisterManager.WriteByte(0x12, 0xff);// RegIrqFlags
		}
	}

	public sealed class StartupTask : IBackgroundTask
	{
		private const byte ChipSelectLine = 25;
		private const byte ResetLine = 17;
		private const byte InterruptLine = 4;
		private Rfm9XDevice rfm9XDevice = new Rfm9XDevice(ChipSelectLine, ResetLine, InterruptLine);
		private byte NessageCount = Byte.MaxValue;

		public void Run(IBackgroundTaskInstance taskInstance)
		{
			// Put device into LoRa + Sleep mode
			rfm9XDevice.RegisterManager.WriteByte(0x01, 0b10000000); // RegOpMode 

			// Set the frequency to 915MHz
			byte[] frequencyWriteBytes = { 0xE4, 0xC0, 0x00 }; // RegFrMsb, RegFrMid, RegFrLsb
			rfm9XDevice.RegisterManager.Write(0x06, frequencyWriteBytes);

			rfm9XDevice.RegisterManager.WriteByte(0x0F, 0x0); // RegFifoRxBaseAddress 

			// More power PA Boost
			rfm9XDevice.RegisterManager.WriteByte(0x09, 0b10000000); // RegPaConfig

			rfm9XDevice.RegisterManager.WriteByte(0x40, 0b01000000); // RegDioMapping1 0b00000000 DI0 RxReady & TxReady

			rfm9XDevice.RegisterManager.WriteByte(0x01, 0b10000101); // RegOpMode set LoRa & RxContinuous

			rfm9XDevice.RegisterDump();

			while (true)
			{
				rfm9XDevice.RegisterManager.WriteByte(0x0E, 0x0); // RegFifoTxBaseAddress 

				// Set the Register Fifo address pointer
				rfm9XDevice.RegisterManager.WriteByte(0x0D, 0x0); // RegFifoAddrPtr 

				string messageText = "W10 IoT Core LoRa! " + NessageCount.ToString();
				NessageCount -= 1;

				// load the message into the fifo
				byte[] messageBytes = UTF8Encoding.UTF8.GetBytes(messageText);
				foreach (byte b in messageBytes)
				{
					rfm9XDevice.RegisterManager.WriteByte(0x0, b); // RegFifo 
				}

				// Set the length of the message in the fifo
				rfm9XDevice.RegisterManager.WriteByte(0x22, (byte)messageBytes.Length); // RegPayloadLength
				rfm9XDevice.RegisterManager.WriteByte(0x40, 0b01000000); // RegDioMapping1 0b00000000 DI0 RxReady & TxReady
				rfm9XDevice.RegisterManager.WriteByte(0x01, 0b10000011); // RegOpMode 

				Debug.WriteLine("Sending {0} bytes message {1}", messageBytes.Length, messageText);

				Task.Delay(10000).Wait();
			}
		}
	}
}
