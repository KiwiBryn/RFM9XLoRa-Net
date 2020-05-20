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
namespace devMobile.IoT.Rfm9x.TransmitInterrupt
{
	using System;
	using System.Diagnostics;
	using System.Text;
	using System.Runtime.InteropServices.WindowsRuntime;
	using System.Threading.Tasks;
	using Windows.ApplicationModel.Background;
	using Windows.Devices.Spi;
	using Windows.Devices.Gpio;

	public sealed class Rfm9XDevice
	{
		private SpiDevice Rfm9XLoraModem = null;
		private GpioPin ChipSelectGpioPin = null;
		private GpioPin InterruptGpioPin = null;
		private const byte RegisterAddressReadMask = 0X7f;
		private const byte RegisterAddressWriteMask = 0x80;

		public Rfm9XDevice(byte chipSelectPin, byte resetPin, byte interruptPin)
		{
			SpiController spiController = SpiController.GetDefaultAsync().AsTask().GetAwaiter().GetResult();
			var settings = new SpiConnectionSettings(0)
			{
				ClockFrequency = 500000,
				Mode = SpiMode.Mode0,
			};

			// Chip select pin configuration
			GpioController gpioController = GpioController.GetDefault();
			ChipSelectGpioPin = gpioController.OpenPin(chipSelectPin);
			ChipSelectGpioPin.SetDriveMode(GpioPinDriveMode.Output);
			ChipSelectGpioPin.Write(GpioPinValue.High);

			// Reset pin configuration to do factory reset
			GpioPin resetGpioPin = gpioController.OpenPin(resetPin);
			resetGpioPin.SetDriveMode(GpioPinDriveMode.Output);
			resetGpioPin.Write(GpioPinValue.Low);
			Task.Delay(10);
			resetGpioPin.Write(GpioPinValue.High);
			Task.Delay(10);

			// Interrupt pin for RX message & TX done notification 
			InterruptGpioPin = gpioController.OpenPin(interruptPin);
			InterruptGpioPin.SetDriveMode(GpioPinDriveMode.Input);

			InterruptGpioPin.ValueChanged += InterruptGpioPin_ValueChanged;

			Rfm9XLoraModem = spiController.GetDevice(settings);
		}

		private void InterruptGpioPin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
		{
			if (args.Edge != GpioPinEdge.RisingEdge)
			{
				return;
			}

			byte IrqFlags = this.RegisterReadByte(0x12); // RegIrqFlags
			Debug.WriteLine(string.Format("RegIrqFlags {0}", Convert.ToString(IrqFlags, 2).PadLeft(8, '0')));

			if ((IrqFlags & 0b00001000) == 0b00001000)  // TxDone
			{
				Debug.WriteLine("Transmit-Done");
			}

			this.RegisterWriteByte(0x12, 0xff);// RegIrqFlags
		}

		public Byte RegisterReadByte(byte address)
		{
			byte[] writeBuffer = new byte[] { address &= RegisterAddressReadMask };
			byte[] readBuffer = new byte[1];
			Debug.Assert(Rfm9XLoraModem != null);

			ChipSelectGpioPin.Write(GpioPinValue.Low);
			Rfm9XLoraModem.Write(writeBuffer);
			Rfm9XLoraModem.Read(readBuffer);
			ChipSelectGpioPin.Write(GpioPinValue.High);

			return readBuffer[0];
		}

		public ushort RegisterReadWord(byte address)
		{
			byte[] writeBuffer = new byte[] { address &= RegisterAddressReadMask };
			byte[] readBuffer = new byte[2];
			Debug.Assert(Rfm9XLoraModem != null);

			ChipSelectGpioPin.Write(GpioPinValue.Low);
			Rfm9XLoraModem.Write(writeBuffer);
			Rfm9XLoraModem.Read(readBuffer);
			ChipSelectGpioPin.Write(GpioPinValue.High);

			return (ushort)(readBuffer[1] + (readBuffer[0] << 8));
		}

		public byte[] RegisterRead(byte address, int length)
		{
			byte[] writeBuffer = new byte[] { address &= RegisterAddressReadMask };
			byte[] readBuffer = new byte[length];
			Debug.Assert(Rfm9XLoraModem != null);

			ChipSelectGpioPin.Write(GpioPinValue.Low);
			Rfm9XLoraModem.Write(writeBuffer);
			Rfm9XLoraModem.Read(readBuffer);
			ChipSelectGpioPin.Write(GpioPinValue.High);

			return readBuffer;
		}

		public void RegisterWriteByte(byte address, byte value)
		{
			byte[] writeBuffer = new byte[] { address |= RegisterAddressWriteMask, value };
			Debug.Assert(Rfm9XLoraModem != null);

			ChipSelectGpioPin.Write(GpioPinValue.Low);
			Rfm9XLoraModem.Write(writeBuffer);
			ChipSelectGpioPin.Write(GpioPinValue.High);
		}

		public void RegisterWriteWord(byte address, ushort value)
		{
			byte[] valueBytes = BitConverter.GetBytes(value);
			byte[] writeBuffer = new byte[] { address |= RegisterAddressWriteMask, valueBytes[0], valueBytes[1] };
			Debug.Assert(Rfm9XLoraModem != null);

			ChipSelectGpioPin.Write(GpioPinValue.Low);
			Rfm9XLoraModem.Write(writeBuffer);
			ChipSelectGpioPin.Write(GpioPinValue.High);
		}

		public void RegisterWrite(byte address, [ReadOnlyArray()] byte[] bytes)
		{
			byte[] writeBuffer = new byte[1 + bytes.Length];
			Debug.Assert(Rfm9XLoraModem != null);

			Array.Copy(bytes, 0, writeBuffer, 1, bytes.Length);
			writeBuffer[0] = address |= RegisterAddressWriteMask;

			ChipSelectGpioPin.Write(GpioPinValue.Low);
			Rfm9XLoraModem.Write(writeBuffer);
			ChipSelectGpioPin.Write(GpioPinValue.High);
		}

		public void RegisterDump()
		{
			Debug.WriteLine("Register dump");
			for (byte registerIndex = 0; registerIndex <= 0x42; registerIndex++)
			{
				byte registerValue = this.RegisterReadByte(registerIndex);

				Debug.WriteLine("Register 0x{0:x2} - Value 0X{1:x2} - Bits {2}", registerIndex, registerValue, Convert.ToString(registerValue, 2).PadLeft(8, '0'));
			}
		}
	}


	public sealed class StartupTask : IBackgroundTask
	{
		private const byte ChipSelectLine = 25;
		private const byte ResetLine = 17;
		private const byte InterruptLine = 4;
		private Rfm9XDevice rfm9XDevice = new Rfm9XDevice(ChipSelectLine, ResetLine, InterruptLine);

		public void Run(IBackgroundTaskInstance taskInstance)
		{
			// Put device into LoRa + Sleep mode
			rfm9XDevice.RegisterWriteByte(0x01, 0b10000000); // RegOpMode 

			// Set the frequency to 915MHz
			byte[] frequencyWriteBytes = { 0xE4, 0xC0, 0x00 }; // RegFrMsb, RegFrMid, RegFrLsb
			rfm9XDevice.RegisterWrite(0x06, frequencyWriteBytes);

			// More power PA Boost
			rfm9XDevice.RegisterWriteByte(0x09, 0b10000000); // RegPaConfig

			// Interrupt on TxDone
			rfm9XDevice.RegisterWriteByte(0x40, 0b01000000); // RegDioMapping1 0b00000000 DI0 TxDone

			while (true)
			{
				// Set the Register Fifo address pointer
				rfm9XDevice.RegisterWriteByte(0x0E, 0x00); // RegFifoTxBaseAddress 

				// Set the Register Fifo address pointer
				rfm9XDevice.RegisterWriteByte(0x0D, 0x0); // RegFifoAddrPtr 

				string messageText = "Hello LoRa!";

				// load the message into the fifo
				byte[] messageBytes = UTF8Encoding.UTF8.GetBytes(messageText);
				foreach (byte b in messageBytes)
				{
					rfm9XDevice.RegisterWriteByte(0x0, b); // RegFifo 
				}

				// Set the length of the message in the fifo
				rfm9XDevice.RegisterWriteByte(0x22, (byte)messageBytes.Length); // RegPayloadLength
				Debug.WriteLine("Sending {0} bytes message {1}", messageBytes.Length, messageText);
				rfm9XDevice.RegisterWriteByte(0x01, 0b10000011); // RegOpMode 

				Task.Delay(10000).Wait();
			}
		}
	}
}

