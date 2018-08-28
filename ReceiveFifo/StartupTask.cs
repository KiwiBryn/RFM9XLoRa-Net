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
namespace devMobile.IoT.Rfm9x.ReceiveFifo
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
		private const byte RegisterAddressReadMask = 0X7f;
		private const byte RegisterAddressWriteMask = 0x80;

		public Rfm9XDevice(byte chipSelectPin, byte resetPin)
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

			Rfm9XLoraModem = spiController.GetDevice(settings);
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
		private const int ChipSelectLine = 25;
		private const int ResetLine = 17;
		private Rfm9XDevice rfm9XDevice = new Rfm9XDevice(ChipSelectLine, ResetLine);

		public void Run(IBackgroundTaskInstance taskInstance)
		{
			// Put device into LoRa + Sleep mode
			rfm9XDevice.RegisterWriteByte(0x01, 0b10000000); // RegOpMode 

			// Set the frequency to 915MHz
			byte[] frequencyWriteBytes = { 0xE4, 0xC0, 0x00 }; // RegFrMsb, RegFrMid, RegFrLsb
			rfm9XDevice.RegisterWrite(0x06, frequencyWriteBytes);

			rfm9XDevice.RegisterWriteByte(0x0F, 0x0); // RegFifoRxBaseAddress 

			rfm9XDevice.RegisterWriteByte(0x01, 0b10000101); // RegOpMode set LoRa & RxContinuous

			while (true)
			{
				// Wait until a packet is received, no timeouts in PoC
				Debug.WriteLine(DateTime.Now.ToString("hh:mm:ss") + " Receive-Wait");
				byte IrqFlags = rfm9XDevice.RegisterReadByte(0x12); // RegIrqFlags
				while ((IrqFlags & 0b01000000) == 0)  // wait until RxDone cleared
				{
					Task.Delay(20).Wait();
					IrqFlags = rfm9XDevice.RegisterReadByte(0x12); // RegIrqFlags
					//Debug.Write(".");
				}
				Debug.WriteLine("");

				byte regHopChannel = rfm9XDevice.RegisterReadByte(0x1C);
				//Debug.WriteLine(string.Format("regHopChannel {0}", Convert.ToString((byte)regHopChannel, 2).PadLeft(8, '0')));
				if ((regHopChannel & 0b01000000) == 0b000000000)
				{
					Debug.WriteLine(DateTime.Now.ToString("hh:mm:ss") + " No CRC" );
					rfm9XDevice.RegisterWriteByte(0x12, 0xff); // RegIrqFlags clear all the bits
					continue; // so nasty
				}

				if ((IrqFlags & 0b00100000) == 0b00100000)
				{
					Debug.WriteLine(DateTime.Now.ToString("hh:mm:ss") + " CRC error" );
					rfm9XDevice.RegisterWriteByte(0x12, 0xff); // RegIrqFlags clear all the bits
					continue; // so nasty
				}

				byte currentFifoAddress = rfm9XDevice.RegisterReadByte(0x10); // RegFifiRxCurrent
				rfm9XDevice.RegisterWriteByte(0x0d, currentFifoAddress); // RegFifoAddrPtr* 
				byte numberOfBytes = rfm9XDevice.RegisterReadByte(0x13); // RegRxNbBytes

				// Allocate buffer for message
				byte[] messageBytes = new byte[numberOfBytes];

				for (int i = 0; i < numberOfBytes; i++)
				{
					messageBytes[i] = rfm9XDevice.RegisterReadByte(0x00); // RegFifo
				}
				rfm9XDevice.RegisterWriteByte(0x12, 0xff); // RegIrqFlags clear all the bits

				string messageText = UTF8Encoding.UTF8.GetString(messageBytes);
				Debug.WriteLine(DateTime.Now.ToString("hh:mm:ss") + " Received {0} byte message {1}",  messageBytes.Length, messageText);
				Debug.WriteLine(string.Format("RegIrqFlags {0}", Convert.ToString((byte)IrqFlags, 2).PadLeft(8, '0')));
				Debug.WriteLine("Receive-Done");
			}
		}
	}
}

