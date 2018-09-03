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
namespace devMobile.IoT.Rfm9x.LoRaDeviceClient
{
	using System;
	using System.Diagnostics;
	using System.Text;
	using System.Threading.Tasks;

	using devMobile.IoT.Rfm9x;
	using Windows.ApplicationModel.Background;

	public sealed class StartupTask : IBackgroundTask
    {
		private byte NessageCount = Byte.MaxValue;
#if DRAGINO
		private const byte ChipSelectLine = 25;
		private const byte ResetLine = 17;
		private const byte InterruptLine = 4;
		private Rfm9XDevice rfm9XDevice = new Rfm9XDevice(ChipSelectPin.CS0, ChipSelectLine, ResetLine, InterruptLine);
#endif
#if M2M
		private const byte ChipSelectLine = 25;
		private const byte ResetLine = 17;
		private const byte InterruptLine = 4;
		private Rfm9XDevice rfm9XDevice = new Rfm9XDevice(ChipSelectPin.CS0, ChipSelectLine, ResetLine, InterruptLine);
#endif
#if ELECROW 
		private const byte ResetLine = 22;
		private const byte InterruptLine = 25;
		private Rfm9XDevice rfm9XDevice = new Rfm9XDevice(ChipSelectPin.CS1, ResetLine, InterruptLine);
#endif
#if ELECTRONIC_TRICKS
		private const byte ResetLine = 22;
		private const byte InterruptLine = 25;
		private Rfm9XDevice rfm9XDevice = new Rfm9XDevice(ChipSelectPin.CS0, 22, 25);
#endif

		public void Run(IBackgroundTaskInstance taskInstance)
		{
			rfm9XDevice.Initialise(915000000.0, paBoost: true, rxPayloadCrcOn : true);

#if DEBUG
			rfm9XDevice.RegisterDump();
#endif

			rfm9XDevice.OnReceive += Rfm9XDevice_OnReceive;
#if ADDRESSED_MESSAGES_PAYLOAD
			rfm9XDevice.Receive(UTF8Encoding.UTF8.GetBytes(Environment.MachineName));
#else
			rfm9XDevice.Receive();
#endif
			rfm9XDevice.OnTransmit += Rfm9XDevice_OnTransmit;

			Task.Delay(10000).Wait();

			while (true)
			{
				string messageText = string.Format("Hello from {0} ! {1}", Environment.MachineName, NessageCount);
				NessageCount -= 1;

				byte[] messageBytes = UTF8Encoding.UTF8.GetBytes(messageText);
				Debug.WriteLine("{0:HH:mm:ss}-TX {1} byte message {2}", DateTime.Now, messageBytes.Length, messageText);
#if ADDRESSED_MESSAGES_PAYLOAD
				this.rfm9XDevice.Send(UTF8Encoding.UTF8.GetBytes("AddressGoesHere"), messageBytes);
#else
				this.rfm9XDevice.Send(messageBytes);
#endif
				Task.Delay(10000).Wait();
			}
		}

		private void Rfm9XDevice_OnReceive(object sender, Rfm9XDevice.OnDataReceivedEventArgs e)
		{
			try
			{
				string messageText = UTF8Encoding.UTF8.GetString(e.Data);

#if ADDRESSED_MESSAGES_PAYLOAD
				string addressText = UTF8Encoding.UTF8.GetString(e.Address);

				Debug.WriteLine(@"{0:HH:mm:ss}-RX From {1} PacketSnr {2:0.0} Packet RSSI {3}dBm RSSI {4}dBm = {5} byte message ""{6}""", DateTime.Now, addressText, e.PacketSnr, e.PacketRssi, e.Rssi, e.Data.Length, messageText);
#else
				Debug.WriteLine(@"{0:HH:mm:ss}-RX PacketSnr {1:0.0} Packet RSSI {2}dBm RSSI {3}dBm = {4} byte message ""{5}""", DateTime.Now, e.PacketSnr, e.PacketRssi, e.Rssi, e.Data.Length, messageText);
#endif
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}
		}

		private void Rfm9XDevice_OnTransmit(object sender, Rfm9XDevice.OnDataTransmitedEventArgs e)
		{
			Debug.WriteLine("{0:HH:mm:ss}-TX Done", DateTime.Now);
		}
	}
}

