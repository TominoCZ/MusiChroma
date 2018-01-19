using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Assets
{
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public class AdpcmWaveFormat : WaveFormat
    {
        public short SamplesPerBlock;
        public short NumCoeff;
        // 7 pairs of coefficients
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 14)]
        public short[] Coefficients;

        /// <summary>
        /// Empty constructor needed for marshalling from a pointer
        /// </summary>
        AdpcmWaveFormat() : this(8000, 1)
        {
        }

        /// <summary>
        /// Microsoft ADPCM  
        /// </summary>
        /// <param name="SampleRate">Sample Rate</param>
        /// <param name="channels">Channels</param>
        public AdpcmWaveFormat(int SampleRate, int channels) :
            base(SampleRate, 0, channels)
        {
            this.waveFormatTag = WaveFormatEncoding.Adpcm;

            // TODO: validate SampleRate, BitsPerSample
            this.ExtraSize = 32;
            switch (this.SampleRate)
            {
                case 8000:
                case 11025:
                    BlockAlign = 256;
                    break;
                case 22050:
                    BlockAlign = 512;
                    break;
                case 44100:
                default:
                    BlockAlign = 1024;
                    break;
            }

            this.BitsPerSample = 4;
            this.SamplesPerBlock = (short)((((BlockAlign - (7 * channels)) * 8) / (BitsPerSample * channels)) + 2);
            this.AverageBytesPerSecond =
                ((this.SampleRate * BlockAlign) / SamplesPerBlock);

            // samplesPerBlock = BlockAlign - (7 * channels)) * (2 / channels) + 2;


            NumCoeff = 7;
            Coefficients = new short[14] {
                256,0,512,-256,0,0,192,64,240,0,460,-208,392,-232
            };
        }

        /// <summary>
        /// Serializes this wave format
        /// </summary>
        /// <param name="writer">Binary writer</param>
        public override void Serialize(System.IO.BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(SamplesPerBlock);
            writer.Write(NumCoeff);
            foreach (short coefficient in Coefficients)
            {
                writer.Write(coefficient);
            }
        }

        /// <summary>
        /// String Description of this WaveFormat
        /// </summary>
        public override string ToString()
        {
            return String.Format("Microsoft ADPCM {0} Hz {1} channels {2} bits per sample {3} samples per block",
                this.SampleRate, this.Channels, this.BitsPerSample, this.SamplesPerBlock);
        }
    }
}
