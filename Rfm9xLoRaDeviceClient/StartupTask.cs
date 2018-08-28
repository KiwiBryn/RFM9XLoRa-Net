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
		private const byte ChipSelectLine = 25;
		private const byte ResetLine = 17;
		private const byte InterruptLine = 4;
		private Rfm9XDevice rfm9XDevice = new Rfm9XDevice(ChipSelectPin.CS0, ChipSelectLine, ResetLine, InterruptLine); // Dragino/M2m shields
		//private Rfm9XDevice rfm9XDevice = new Rfm9XDevice(ChipSelectPin.CS1, ResetLine, InterruptLine); // Elecrow/Electronic tricks shields
		private byte NessageCount = Byte.MaxValue;

		public void Run(IBackgroundTaskInstance taskInstance)
		{
			rfm9XDevice.Initialise(Rfm9XDevice.RegOpModeMode.ReceiveContinuous, 915000000.0, paBoost: true);

			rfm9XDevice.RegisterDump();

			rfm9XDevice.OnReceive += Rfm9XDevice_OnReceive;
			rfm9XDevice.OnTransmit += Rfm9XDevice_OnTransmit;

			while (true)
			{
				string messageText = "Hello W10 IoT Core LoRa! " + NessageCount.ToString();
				NessageCount -= 1;
				byte[] messageBytes = UTF8Encoding.UTF8.GetBytes(messageText);
				Debug.WriteLine("{0}-Sending {1} bytes message {2}", DateTime.Now.ToShortTimeString(), messageBytes.Length, messageText);
				this.rfm9XDevice.SendMessage(messageBytes);

				Debug.Write(".");
				Task.Delay(10000).Wait();
			}
		}

		private void Rfm9XDevice_OnReceive(object sender, Rfm9XDevice.OnDataReceivedEventArgs e)
		{
			try
			{
				string messageText = UTF8Encoding.UTF8.GetString(e.Data);
				Debug.WriteLine("{0}-Received {1} byte message {2}", DateTime.Now.ToShortTimeString(), e.Data.Length, messageText);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}
		}

		private void Rfm9XDevice_OnTransmit(object sender, Rfm9XDevice.OnDataTransmitedEventArgs e)
		{
			Debug.WriteLine("{0}-Transmit-Done", DateTime.Now.ToShortTimeString());
		}
	}
}

