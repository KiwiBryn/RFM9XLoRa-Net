﻿//---------------------------------------------------------------------------------
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
namespace devMobile.IoT.Rfm9x.EnumsAndMasks
{
	using System;
	using System.Diagnostics;
	using System.Text;
	using System.Threading.Tasks;
	using Windows.ApplicationModel.Background;
	using Windows.Devices.Gpio;

	sealed class Rfm9XDevice
	{
		// Registers from SemTech SX127X Datasheet
		enum Registers : byte
		{
			MinValue = RegOpMode,
			RegFifo = 0x0,
			RegOpMode = 0x01,
			//Reserved 0x02-0x06 
			RegFrMsb = 0x06,
			RegFrMid = 0x7,
			RegFrLsb = 0x08,
			RegPAConfig = 0x09,
			//RegPARamp = 0x0A, // not inlcuded as FSK/OOK functionality
			RegOcp = 0x0B,
			RegLna = 0x0C,
			RegFifoAddrPtr = 0x0D,
			//RegFifoTxBaseAddr = 0x0E
			RegFifoRxCurrent =0x10,
			RegIrqFlagsMask = 0x11,
			RegIrqFlags = 0x12,
			// RegRxNdBytes = 0x13
			// RegRxHeaderCnValueMsb=0x14
			// RegRxHeaderCnValueLsb=0x15
			// RegRxPacketCntValueMsb=0x16
			// RegRxPacketCntValueMsb=0x17
			// RegModemStat=0x18
			// RegPktSnrVale=0x19
			// RegPktRssiValue=0x1A
			// RegRssiValue=0x1B
			// RegHopChannel=0x1C
			RegModemConfig1 = 0x1D,
			RegModemConfig2 = 0x1E,
			RegSymbTimeout = 0x1F,
			RegPreambleMsb = 0x20,
			RegPreambleLsb = 0x21,
			RegPayloadLength = 0x22,
			RegMaxPayloadLength = 0x23,
			RegHopPeriod = 0x24,
			// RegFifiRxByteAddr = 0x25
			RegModemConfig3 = 0x26,
			RegPpmCorrection = 0x27,
			// RegFeiMsb = 0x28
			// RegFeiMid = 0x29
			// RegFeiLsb = 0x2A
			// Reserved 0x2B
			// RegRssiWideband = 0x2C
			// Reserved 0x2D-0x30
			RegDetectOptimize = 0x31,
			// Reserved 0x32
			RegInvertIQ = 0x33,
			// Reserved 0x34-0x36
			RegDetectionThreshold = 0x37,
			// Reserved 0x38
			RegSyncWord = 0x39,
			RegDioMapping1 = 0x40,
			RegVersion = 0x42,

			MaxValue = RegVersion,
		}

		// RegOpMode mode flags
		private const byte RegOpModeLongRangeModeLoRa = 0b10000000;
		private const byte RegOpModeLongRangeModeFskOok = 0b00000000;
		private const byte RegOpModeLongRangeModeDefault = RegOpModeLongRangeModeFskOok;

		private const byte RegOpModeAcessSharedRegLoRa = 0b00000000;
		private const byte RegOpModeAcessSharedRegFsk = 0b01000000;
		private const byte RegOpModeAcessSharedRegDefault = RegOpModeAcessSharedRegLoRa;

		private const byte RegOpModeLowFrequencyModeOnHighFrequency = 0b00000000;
		private const byte RegOpModeLowFrequencyModeOnLowFrequency = 0b00001000;
		private const byte RegOpModeLowFrequencyModeOnDefault = RegOpModeLowFrequencyModeOnLowFrequency;

		[Flags]
		public enum RegOpModeMode : byte
		{
			Sleep = 0b00000000,
			StandBy = 0b00000001,
			FrequencySynthesisTX = 0b00000010,
			Transmit = 0b00000011,
			FrequencySynthesisRX = 0b00000100,
			ReceiveContinuous = 0b00000101,
			ReceiveSingle = 0b00000110,
			ChannelActivityDetection = 0b00000111,
		};

		// Frequency configuration magic numbers from Semtech SX127X specs
		private const double RH_RF95_FXOSC = 32000000.0;
		private const double RH_RF95_FSTEP = RH_RF95_FXOSC / 524288.0;

		// RegFrMsb, RegFrMid, RegFrLsb
		private const double FrequencyDefault = 434000000.0;

		// RegPAConfig
		private const Byte RegPAConfigPASelectRFO = 0b00000000;
		private const Byte RegPAConfigPASelectPABoost = 0b10000000;
		private const Byte RegPAConfigPASelectDefault = RegPAConfigPASelectRFO;

		private const byte RegPAConfigMaxPowerMin = 0b00000000;
		private const byte RegPAConfigMaxPowerMax = 0b01110000;
		private const byte RegPAConfigMaxPowerDefault = 0b01000000;

		private const byte RegPAConfigOutputPowerMin = 0b00000000;
		private const byte RegPAConfigOutputPowerMax = 0b00001111;
		private const byte RegPAConfigOutputPowerDefault = 0b00001111;

		// RegPaRamp appears to be for FSK only ?

		// RegOcp
		private const byte RegOcpOn =  0b00100000;
		private const byte RegOcpOff = 0b00000000;
		private const byte RegOcpDefault = RegOcpOn;

		private const byte RegOcpOcpTrimMin = 0b00000000;
		private const byte RegOcpOcpTrimMax = 0b00011111;
		private const byte RegOcpOcpTrimDefault = 0b00001011;

		// RegLna
		[Flags]
		public enum RegLnaLnaGain : byte
		{
			G1 = 0b00000001,
			G2 = 0b00000010,
			G3 = 0b00000011,
			G4 = 0b00000100,
			G5 = 0b00000101,
			G6 = 0b00000110
		}
		// Note the gain is backwards less = more
		private const RegLnaLnaGain LnaGainDefault = RegLnaLnaGain.G1;

		private const byte RegLnaLnaBoostLfOn = 0b00011000;
		private const byte RegLnaLnaBoostLfOff = 0b00000000;
		private const byte RegLnaLnaBoostLfDefault = RegLnaLnaBoostLfOff;

		private const byte RegLnaLnaBoostHfOn = 0b00000011;
		private const byte RegLnaLnaBoostHfOff = 0b00000000;
		private const byte RegLnaLnaBoostHfDefault = RegLnaLnaBoostHfOff;

		[Flags]
		enum RegIrqFlagsMask : byte
		{
			RxTimeoutMask = 0b10000000,
			RxDoneMask = 0b01000000,
			PayLoadCrcErrorMask = 0b00100000,
			ValidHeadrerMask = 0b00010000,
			TxDoneMask = 0b00001000,
			CadDoneMask = 0b00000100,
			FhssChangeChannelMask = 0b00000010,
			CadDetectedMask = 0b00000001,
		}

		[Flags]
		enum RegIrqFlags : byte
		{
			RxTimeout = 0b10000000,
			RxDone = 0b01000000,
			PayLoadCrcError = 0b00100000,
			ValidHeadrer = 0b00010000,
			TxDone = 0b00001000,
			CadDone = 0b00000100,
			FhssChangeChannel = 0b00000010,
			CadDetected = 0b00000001,
			ClearAll = 0b11111111,
		}

		// RegModemConfig1
		public enum RegModemConfigBandwidth : byte
		{
			_7_8KHz =	0b00000000,
			_10_4KHz =	0b00010000,
			_15_6KHz =	0b00100000,
			_20_8KHz =	0b00110000,
			_31_25KHz = 0b01000000,
			_41_7KHz =	0b01010000,
			_62_5KHz =	0b01100000,
			_125KHz =	0b01110000,
			_250KHz =	0b10000000,
			_500KHz =	0b10010000
		}
		private const RegModemConfigBandwidth RegModemConfigBandwidthDefault = RegModemConfigBandwidth._125KHz;

		public enum RegModemConfigCodingRate
		{
			_4of5 = 0b00000010,
			_4of6 = 0b00000100,
			_4of7 = 0b00000110,
			_4of8 = 0b00001000,
		}
		private const RegModemConfigCodingRate RegModemConfigCodingRateDefault = RegModemConfigCodingRate._4of5;

		public enum RegModemConfigImplicitHeaderModeOn
		{
			ExplicitHeaderMode = 0b00000000,
			ImplicitHeaderMode = 0b00000001,
		}
		private const RegModemConfigImplicitHeaderModeOn RegModemConfigImplicitHeaderModeOnDefault = RegModemConfigImplicitHeaderModeOn.ExplicitHeaderMode;

		// RegModemConfig2
		public enum RegModemConfig2SpreadingFactor : byte
		{
			_64ChipsPerSymbol = 0b01100000,
			_128ChipsPerSymbol = 0b01110000,
			_256ChipsPerSymbol = 0b10000000,
			_512ChipsPerSymbol = 0b10010000,
			_1024ChipsPerSymbol = 0b10100000,
			_2048ChipsPerSymbol = 0b10110000,
			_4096ChipsPerSymbol = 0b11000000,
		}
		private const RegModemConfig2SpreadingFactor RegModemConfig2SpreadingFactorDefault = RegModemConfig2SpreadingFactor._128ChipsPerSymbol;

		private const byte RegModemConfig2TxContinuousModeOn = 0b00001000;
		private const byte RegModemConfig2TxContinuousModeOff = 0b00000000;
		private const byte RegModemConfig2TxContinuousModeDefault = RegModemConfig2TxContinuousModeOff;

		private const byte RegModemConfig2RxPayloadCrcOn = 0b00000100;
		private const byte RegModemConfig2RxPayloadCrcOff = 0b00000000;
		private const byte RegModemConfig2RxPayloadCrcDefault = RegModemConfig2RxPayloadCrcOff;

		// RegModemConfig2 for MSb RegSymbTimeoutLsb for LSB
		private const byte SymbolTimeoutDefault = 0x64;

		// RegReambleMsb & RegReambleLsb
		private const ushort PreambleLengthDefault = 0x08;

		// RegPayloadLength
		private const byte PayloadLengthDefault = 0x01;

		// RegMaxPayloadLength
		private const byte PayloadMaxLengthDefault = 0xff;

		// RegHopPeriod
		private const byte FreqHoppingPeriodDefault = 0x0;

		// RegModemConfig3
		private const byte RegModemConfig3LowDataRateOptimizeOn = 0b00001000;
		private const byte RegModemConfig3LowDataRateOptimizeOff = 0b00000000;
		private const byte RegModemConfig3LowDataRateOptimizeDefault = RegModemConfig3LowDataRateOptimizeOff;

		private const byte RegModemConfig3AgcAutoOn = 0b00000100;
		private const byte RegModemConfig3AgcAutoOff = 0b00000000;
		private const byte RegModemConfig3AgcAutoDefault = RegModemConfig3AgcAutoOff;

		// RegPpmCorrection
		private const byte ppmCorrectionDefault = 0x0;

		// RegDetectOptimize
		public enum RegDetectOptimizeDectionOptimize
		{
			_SF7toSF12 = 0x03,
			_SF6 = 0x05,
		};
		private const RegDetectOptimizeDectionOptimize RegDetectOptimizeDectionOptimizeDefault = RegDetectOptimizeDectionOptimize._SF7toSF12;

		// RegInvertId
		private const byte InvertIqOn = 0b01000000;
		private const byte InvertIqOff = 0b00000000;
		private const byte InvertIqDefault = InvertIqOn;

		// RegDetectionThreshold
		public enum RegisterDetectionThreshold
		{
			_SF7toSF12 = 0x0A,
			_SF6 = 0x0c,
		}
		private const RegisterDetectionThreshold RegisterDetectionThresholdDefault = RegisterDetectionThreshold._SF7toSF12;

		// RegSyncWord Syncword default for public networks
		private const byte RegSyncWordDefault = 0x12;

		// The Semtech ID Relating to the Silicon revision
		private const byte RegVersionValueExpected = 0x12;

		// Hardware configuration support
		private RegOpModeMode RegOpModeModeCurrent = RegOpModeMode.Sleep;
		private double Frequency = FrequencyDefault;
		private GpioPin InterruptGpioPin = null;
		private GpioPin ResetGpioPin = null;
		private RegisterManager RegisterManager = null;



		// Constructor for shields with chip select connected to CS0/CS1 e.g. Elecrow/Electronic tricks
		public Rfm9XDevice(ChipSelectPin chipSelectPin, int resetPinNumber, int interruptPinNumber)
		{
			RegisterManager = new RegisterManager(chipSelectPin);

			// Check that SX127X chip is present
			Byte regVersionValue = RegisterManager.ReadByte((byte)Registers.RegVersion);
			if (regVersionValue != RegVersionValueExpected)
			{
				throw new ApplicationException("Semtech SX127X not found");
			}

			GpioController gpioController = GpioController.GetDefault();

			// Setup the reset pin
			ResetGpioPin = gpioController.OpenPin(resetPinNumber);
			ResetGpioPin.SetDriveMode(GpioPinDriveMode.Output);
			ResetGpioPin.Write(GpioPinValue.High);

			// Interrupt pin for RX message, TX done etc. notifications
			InterruptGpioPin = gpioController.OpenPin(interruptPinNumber);
			InterruptGpioPin.SetDriveMode(GpioPinDriveMode.Input);

			InterruptGpioPin.ValueChanged += InterruptGpioPin_ValueChanged;
		}

		// Constructor for shields with chip select not connected to CS0/CS1 (but needs to be configured anyway) e.g. Dragino
		public Rfm9XDevice(ChipSelectPin chipSelectPin, int chipSelectPinNumber, int resetPinNumber, int interruptPinNumber)
		{
			RegisterManager = new RegisterManager(chipSelectPin, chipSelectPinNumber);

			// Check that SX127X chip is present
			Byte regVersionValue = RegisterManager.ReadByte((byte)Registers.RegVersion);
			if (regVersionValue != RegVersionValueExpected)
			{
				throw new ApplicationException("Semtech SX127X not found");	
			}

			GpioController gpioController = GpioController.GetDefault();

			// Setup the reset pin
			ResetGpioPin = gpioController.OpenPin(resetPinNumber);
			ResetGpioPin.SetDriveMode(GpioPinDriveMode.Output);
			ResetGpioPin.Write(GpioPinValue.High);

			// Interrupt pin for RX message, TX done etc. notifications
			InterruptGpioPin = gpioController.OpenPin(interruptPinNumber);
			InterruptGpioPin.SetDriveMode(GpioPinDriveMode.Input);

			InterruptGpioPin.ValueChanged += InterruptGpioPin_ValueChanged;
		}

		public void RegisterDump()
		{
			RegisterManager.Dump((byte)Registers.MinValue, (byte)Registers.MaxValue);
		}

		public void SetMode( RegOpModeMode mode )
		{
			byte regOpModeValue;

			regOpModeValue = RegOpModeLongRangeModeLoRa;
			regOpModeValue |= RegOpModeAcessSharedRegLoRa;
			regOpModeValue |= RegOpModeLowFrequencyModeOnHighFrequency;
			regOpModeValue |= (byte)mode;
			RegisterManager.WriteByte((byte)Registers.RegOpMode, regOpModeValue);
		}

		public void Initialise(RegOpModeMode modeAfterInitialise, // RegOpMode
			double frequency = FrequencyDefault, // RegFrMsb, RegFrMid, RegFrLsb
			bool paBoost = false, byte maxPower = RegPAConfigMaxPowerDefault, byte outputPower = RegPAConfigOutputPowerDefault, // RegPaConfig
			bool ocpOn = true, byte ocpTrim = RegOcpOcpTrimDefault, // RegOcp
			RegLnaLnaGain lnaGain = LnaGainDefault, bool lnaBoostLF = false, bool lnaBoostHf = false, // RegLna
			RegModemConfigBandwidth bandwidth = RegModemConfigBandwidthDefault, RegModemConfigCodingRate codingRate = RegModemConfigCodingRateDefault, RegModemConfigImplicitHeaderModeOn implicitHeaderModeOn = RegModemConfigImplicitHeaderModeOnDefault, //RegModemConfig1
         RegModemConfig2SpreadingFactor spreadingFactor = RegModemConfig2SpreadingFactorDefault, bool txContinuousMode = false, bool rxPayloadCrcOn = false,
			ushort symbolTimeout = SymbolTimeoutDefault,
			ushort preambleLength = PreambleLengthDefault,
			byte payloadLength = PayloadLengthDefault, 
			byte payloadMaxLength = PayloadMaxLengthDefault,
			byte freqHoppingPeriod = FreqHoppingPeriodDefault,
			bool lowDataRateOptimize = false, bool agcAutoOn = false,
			byte ppmCorrection = ppmCorrectionDefault,
			RegDetectOptimizeDectionOptimize detectionOptimize=RegDetectOptimizeDectionOptimizeDefault,
         bool invertIQ = false,
			RegisterDetectionThreshold detectionThreshold = RegisterDetectionThresholdDefault,
         byte syncWord = RegSyncWordDefault )
		{
			Frequency = frequency; // Store this away for RSSI adjustments
			RegOpModeModeCurrent = modeAfterInitialise;

			// Strobe Reset pin briefly to factory reset SX127X chip
			ResetGpioPin.Write(GpioPinValue.Low);
			Task.Delay(10);
			ResetGpioPin.Write(GpioPinValue.High);
			Task.Delay(10);

			// Put the device into sleep mode so registers can be changed
			SetMode(RegOpModeMode.Sleep);

			// Configure RF Carrier frequency 
			if (frequency != FrequencyDefault)
			{
				byte[] bytes = BitConverter.GetBytes((long)(frequency / RH_RF95_FSTEP));
				RegisterManager.WriteByte((byte)Registers.RegFrMsb, bytes[2]);
				RegisterManager.WriteByte((byte)Registers.RegFrMid, bytes[1]);
				RegisterManager.WriteByte((byte)Registers.RegFrLsb, bytes[0]);
			}

			// Set RegPaConfig if any of the associated settings are nto the default values
			if ((paBoost!= false) || ( maxPower != RegPAConfigMaxPowerDefault ) || ( outputPower != RegPAConfigOutputPowerDefault ))
			{
				byte regPaConfigValue = maxPower;
				if (paBoost)
				{
					regPaConfigValue |= RegPAConfigPASelectPABoost;
				}
				regPaConfigValue |= outputPower;
				RegisterManager.WriteByte((byte)Registers.RegPAConfig, regPaConfigValue);
			}

			// Set RegOcp if any of the settings not defaults
			if ((ocpOn != true) || (ocpTrim != RegOcpOcpTrimDefault))
			{
				byte regOcpValue = ocpTrim;
				if (ocpOn)
				{
					regOcpValue |= RegOcpOn;
				}
				RegisterManager.WriteByte((byte)Registers.RegOcp, regOcpValue);
			}

			// Set RegLna if any of the settings not defaults
			if (( lnaGain != LnaGainDefault) || ( lnaBoostLF != false ) || ( lnaBoostHf != false))
			{
				byte regLnaValue = (byte)lnaGain;
				if (lnaBoostLF)
				{
					regLnaValue |= RegLnaLnaBoostLfOn;
				}
				if (lnaBoostHf)
				{
					regLnaValue |= RegLnaLnaBoostHfOn;
				}
				RegisterManager.WriteByte((byte)Registers.RegLna, regLnaValue);
			}

			// Set regModemConfig1 if any of the settings not defaults
			if (( bandwidth != RegModemConfigBandwidthDefault ) || ( codingRate != RegModemConfigCodingRateDefault ) || ( implicitHeaderModeOn != RegModemConfigImplicitHeaderModeOnDefault))
			{
				byte regModemConfig1Value = (byte)bandwidth;
				regModemConfig1Value |= (byte)codingRate;
				regModemConfig1Value |= (byte)implicitHeaderModeOn;
				RegisterManager.WriteByte((byte)Registers.RegModemConfig1, regModemConfig1Value);
			}

			// Set regModemConfig2 if any of the settings not defaults
			if (( spreadingFactor != RegModemConfig2SpreadingFactorDefault ) || ( txContinuousMode != false ) | ( rxPayloadCrcOn != false ) || (symbolTimeout != SymbolTimeoutDefault))
			{
				byte RegModemConfig2Value = (byte)spreadingFactor;
				if (txContinuousMode)
				{
					RegModemConfig2Value |= RegModemConfig2TxContinuousModeOn;
				}
				if (rxPayloadCrcOn)
				{
					RegModemConfig2Value |= RegModemConfig2RxPayloadCrcOn;
				}
				// Get the MSB of SymbolTimeout
				byte[] symbolTimeoutBytes = BitConverter.GetBytes(symbolTimeout);
				RegModemConfig2Value |= symbolTimeoutBytes[1];
				RegisterManager.WriteByte((byte)Registers.RegModemConfig2, RegModemConfig2Value);
			}

			// RegModemConfig2.SymbTimout + RegSymbTimeoutLsb
			if (symbolTimeout != SymbolTimeoutDefault)
			{
				// Get the LSB of SymbolTimeout
				byte[] symbolTimeoutBytes = BitConverter.GetBytes(symbolTimeout);
				RegisterManager.WriteByte((byte)Registers.RegSymbTimeout, symbolTimeoutBytes[0]);
			}

			// RegPreambleMsb + RegPreambleLsb
			if ( preambleLength != PreambleLengthDefault)
			{
				byte[] premableBytes = BitConverter.GetBytes(preambleLength);
				RegisterManager.WriteByte((byte)Registers.RegPreambleMsb, premableBytes[1]); 
				RegisterManager.WriteByte((byte)Registers.RegPreambleLsb, premableBytes[0]);
			}

			// RegPayloadLength
			if ( payloadLength != PayloadLengthDefault)
			{
				RegisterManager.WriteByte((byte)Registers.RegPayloadLength, payloadLength);
			}

			// RegMaxPayloadLength
			if ( payloadMaxLength != PayloadMaxLengthDefault)
			{
				RegisterManager.WriteByte((byte)Registers.RegMaxPayloadLength, payloadMaxLength);
			}

			// RegHopPeriod
			if ( freqHoppingPeriod != FreqHoppingPeriodDefault)
			{
				RegisterManager.WriteByte((byte)Registers.RegHopPeriod, freqHoppingPeriod);
			}

			// RegModemConfig3
			if (( lowDataRateOptimize != false ) || ( agcAutoOn != false))
			{
				byte regModemConfig3Value = 0;
				if (lowDataRateOptimize)
				{
					regModemConfig3Value |= RegModemConfig3LowDataRateOptimizeOn;
				}
				if (agcAutoOn)
				{
					regModemConfig3Value |= RegModemConfig3AgcAutoOn;
				}
				RegisterManager.WriteByte((byte)Registers.RegModemConfig3, regModemConfig3Value);
			}

			// RegPpmCorrection
			if ( ppmCorrection != ppmCorrectionDefault)
			{
				RegisterManager.WriteByte((byte)Registers.RegPpmCorrection, ppmCorrection);
			}

			// RegDetectOptimize
			if ( detectionOptimize != RegDetectOptimizeDectionOptimizeDefault)
			{
				RegisterManager.WriteByte((byte)Registers.RegDetectOptimize, (byte)detectionOptimize);
			}

			// RegInvertIQ
			if ( invertIQ != false)
			{
				RegisterManager.WriteByte((byte)Registers.RegInvertIQ, (byte)InvertIqOn);
			}

			// RegDetectionThreshold
			if ( detectionThreshold != RegisterDetectionThresholdDefault)
			{
				RegisterManager.WriteByte((byte)Registers.RegDetectionThreshold, (byte)detectionThreshold);
			}

			// RegSyncWordDefault 
			if (syncWord != RegSyncWordDefault)
			{
				RegisterManager.WriteByte((byte)Registers.RegSyncWord, syncWord);
			}

			// TODO revist this split & move to onReceive function
			RegisterManager.WriteByte(0x40, 0b00000000); // RegDioMapping1 0b00000000 DI0 RxReady & TxReady

			// Configure RegOpMode before returning
			SetMode(modeAfterInitialise);
		}

		private void InterruptGpioPin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
		{
			if (args.Edge != GpioPinEdge.RisingEdge)
			{
				return;
			}

			RegIrqFlagsMask IrqFlags = (RegIrqFlagsMask)this.RegisterManager.ReadByte((byte)Registers.RegIrqFlags);
			Debug.WriteLine(string.Format("RegIrqFlags {0}", Convert.ToString((byte)IrqFlags, 2).PadLeft(8, '0')));

			if ((IrqFlags & RegIrqFlagsMask.RxDoneMask) == RegIrqFlagsMask.RxDoneMask)  // RxDone 
			{
				Debug.WriteLine("RX-Done");
				byte currentFifoAddress = this.RegisterManager.ReadByte((byte)Registers.RegFifoRxCurrent); // RegFifoRxCurrent
				this.RegisterManager.WriteByte((byte)Registers.RegFifoAddrPtr, currentFifoAddress); // RegFifoAddrPtr

				byte numberOfBytes = this.RegisterManager.ReadByte(0x13); // RegRxNbBytes

				// Allocate buffer for message
				byte[] messageBytes = new byte[numberOfBytes];

				for (int i = 0; i < numberOfBytes; i++)
				{
					messageBytes[i] = this.RegisterManager.ReadByte((byte)Registers.RegFifo); 
				}

				string messageText = UTF8Encoding.UTF8.GetString(messageBytes);
				Debug.WriteLine("Received {0} byte message {1}", messageBytes.Length, messageText);
			}

			if ((IrqFlags & RegIrqFlagsMask.TxDoneMask) == RegIrqFlagsMask.TxDoneMask) 
			{
				Debug.WriteLine("TX-Done");
				SetMode(RegOpModeModeCurrent);
			}

			// TODO need to set this based on RX/TX mode handlers. Set based on OnTransmit, onReceive != null
			this.RegisterManager.WriteByte((byte)Registers.RegDioMapping1, 0b00000000); // RegDioMapping1 0b00000000 DI0 RxReady & TxReady
			this.RegisterManager.WriteByte((byte)Registers.RegIrqFlags, (byte)RegIrqFlags.ClearAll);
		}

		public void SendMessage(byte[] messageBytes)
		{
			this.RegisterManager.WriteByte(0x0E, 0x0); // RegFifoTxBaseAddress 

			// Set the Register Fifo address pointer
			this.RegisterManager.WriteByte((byte)Registers.RegFifoAddrPtr, 0x0); 

			foreach (byte b in messageBytes)
			{
				this.RegisterManager.WriteByte((byte)Registers.RegFifo, b);
			}

			// Set the length of the message in the fifo
			this.RegisterManager.WriteByte((byte)Registers.RegPayloadLength, (byte)messageBytes.Length); // RegPayloadLength

			// TODO need to set this based on RX/TX mode handlers. Set based on OnTransmit, onReceive != null
			this.RegisterManager.WriteByte((byte)Registers.RegDioMapping1, 0b01000000); // RegDioMapping1 0b00000000 DI0 RxReady & TxReady

			SetMode(RegOpModeMode.Transmit);
		}
	}


	public sealed class StartupTask : IBackgroundTask
	{
		private const byte ChipSelectLine = 25;
		private const byte ResetLine = 17;
		private const byte InterruptLine = 4;
		private Rfm9XDevice rfm9XDevice = new Rfm9XDevice( ChipSelectPin.CS0, ChipSelectLine, ResetLine, InterruptLine); // Dragino/M2m shields
		//private Rfm9XDevice rfm9XDevice = new Rfm9XDevice(ChipSelectPin.CS1, ResetLine, InterruptLine); // Elecrow/Electronic tricks shields
		private byte messageCount = Byte.MaxValue;

		public void Run(IBackgroundTaskInstance taskInstance)
		{
			rfm9XDevice.Initialise(Rfm9XDevice.RegOpModeMode.ReceiveContinuous, 915000000.0, paBoost:true);

			rfm9XDevice.RegisterDump();

			while (true)
			{
				string messageText = "Hello W10 IoT Core LoRa! " + messageCount.ToString();
				messageCount -= 1;
				byte[] messageBytes = UTF8Encoding.UTF8.GetBytes(messageText);
				Debug.WriteLine("Sending {0} bytes message {1}", messageBytes.Length, messageText);
				this.rfm9XDevice.SendMessage(messageBytes);

				Task.Delay(10000).Wait();
			}
		}
	}
}
