using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Assets
{
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public class Gsm610WaveFormat : WaveFormat
    {
        private readonly short samplesPerBlock;

        /// <summary>
        /// Creates a GSM 610 WaveFormat
        /// For now hardcoded to 13kbps
        /// </summary>
        public Gsm610WaveFormat()
        {
            waveFormatTag = WaveFormatEncoding.Gsm610;
            Channels = 1;
            AverageBytesPerSecond = 1625;
            BitsPerSample = 0; // must be zero
            BlockAlign = 65;
            SampleRate = 8000;

            ExtraSize = 2;
            samplesPerBlock = 320;
        }

        /// <summary>
        /// Samples per block
        /// </summary>
        public short SamplesPerBlock { get { return samplesPerBlock; } }

        /// <summary>
        /// Writes this structure to a BinaryWriter
        /// </summary>
        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(samplesPerBlock);
        }
    }
}
