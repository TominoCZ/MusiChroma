using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Assets
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 2)]
    public class WaveFormatExtensible : WaveFormat
    {
        short wValidBitsPerSample; // bits of precision, or is wSamplesPerBlock if wBitsPerSample==0
        int dwChannelMask; // which channels are present in stream
        Guid subFormat;

        /// <summary>
        /// Parameterless constructor for marshalling
        /// </summary>
        WaveFormatExtensible()
        {
        }

        /// <summary>
        /// Creates a new WaveFormatExtensible for PCM or IEEE
        /// </summary>
        public WaveFormatExtensible(int rate, int bits, int channels)
            : base(rate, bits, channels)
        {
            waveFormatTag = WaveFormatEncoding.Extensible;
            ExtraSize = 22;
            wValidBitsPerSample = (short)bits;
            for (int n = 0; n < channels; n++)
            {
                dwChannelMask |= (1 << n);
            }
            if (bits == 32)
            {
                // KSDATAFORMAT_SUBTYPE_IEEE_FLOAT
                subFormat = AudioMediaSubtypes.MEDIASUBTYPE_IEEE_FLOAT;
            }
            else
            {
                // KSDATAFORMAT_SUBTYPE_PCM
                subFormat = AudioMediaSubtypes.MEDIASUBTYPE_PCM;
            }

        }

        /// <summary>
        /// WaveFormatExtensible for PCM or floating point can be awkward to work with
        /// This creates a regular WaveFormat structure representing the same audio format
        /// Returns the WaveFormat unchanged for non PCM or IEEE float
        /// </summary>
        /// <returns></returns>
        public WaveFormat ToStandardWaveFormat()
        {
            if (subFormat == AudioMediaSubtypes.MEDIASUBTYPE_IEEE_FLOAT && BitsPerSample == 32)
                return CreateIeeeFloatWaveFormat(SampleRate, Channels);
            if (subFormat == AudioMediaSubtypes.MEDIASUBTYPE_PCM)
                return new WaveFormat(SampleRate, BitsPerSample, Channels);
            return this;
            //throw new InvalidOperationException("Not a recognised PCM or IEEE float format");
        }

        /// <summary>
        /// SubFormat (may be one of AudioMediaSubtypes)
        /// </summary>
        public Guid SubFormat { get { return subFormat; } }

        /// <summary>
        /// Serialize
        /// </summary>
        /// <param name="writer"></param>
        public override void Serialize(System.IO.BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(wValidBitsPerSample);
            writer.Write(dwChannelMask);
            byte[] guid = subFormat.ToByteArray();
            writer.Write(guid, 0, guid.Length);
        }

        /// <summary>
        /// String representation
        /// </summary>
        public override string ToString()
        {
            return String.Format("{0} wBitsPerSample:{1} dwChannelMask:{2} subFormat:{3} extraSize:{4}",
                base.ToString(),
                wValidBitsPerSample,
                dwChannelMask,
                subFormat,
                ExtraSize);
        }
    }
}
