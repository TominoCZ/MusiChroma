using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace Assets
{
    /// <summary>
    /// Represents a Wave file format
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 2)]
    public class WaveFormat
    {
        /// <summary>format type</summary>
        public WaveFormatEncoding waveFormatTag;
        /// <summary>number of Channels</summary>
        public short Channels;
        /// <summary>sample rate</summary>
        public int SampleRate;
        /// <summary>for buffer estimation</summary>
        public int AverageBytesPerSecond;
        /// <summary>block size of data</summary>
        public short BlockAlign;
        /// <summary>number of bits per sample of mono data</summary>
        public short BitsPerSample;
        /// <summary>number of following bytes</summary>
        public short ExtraSize;

        /// <summary>
        /// Creates a new PCM 44.1Khz stereo 16 bit format
        /// </summary>
        public WaveFormat() : this(44100, 16, 2)
        {

        }

        /// <summary>
        /// Creates a new 16 bit wave format with the specified sample
        /// rate and channel count
        /// </summary>
        /// <param name="SampleRate">Sample Rate</param>
        /// <param name="Channels">Number of Channels</param>
        public WaveFormat(int SampleRate, int Channels)
            : this(SampleRate, 16, Channels)
        {
        }

        /// <summary>
        /// Gets the size of a wave buffer equivalent to the latency in milliseconds.
        /// </summary>
        /// <param name="milliseconds">The milliseconds.</param>
        /// <returns></returns>
        public int ConvertLatencyToByteSize(int milliseconds)
        {
            int bytes = (int)((AverageBytesPerSecond / 1000.0) * milliseconds);
            if ((bytes % BlockAlign) != 0)
            {
                // Return the upper BlockAligned
                bytes = bytes + BlockAlign - (bytes % BlockAlign);
            }
            return bytes;
        }

        /// <summary>
        /// Creates a WaveFormat with custom members
        /// </summary>
        /// <param name="tag">The encoding</param>
        /// <param name="SampleRate">Sample Rate</param>
        /// <param name="Channels">Number of Channels</param>
        /// <param name="AverageBytesPerSecond">Average Bytes Per Second</param>
        /// <param name="BlockAlign">Block Align</param>
        /// <param name="BitsPerSample">Bits Per Sample</param>
        /// <returns></returns>
        public static WaveFormat CreateCustomFormat(WaveFormatEncoding tag, int SampleRate, int Channels, int AverageBytesPerSecond, int BlockAlign, int BitsPerSample)
        {
            WaveFormat waveFormat = new WaveFormat();
            waveFormat.waveFormatTag = tag;
            waveFormat.Channels = (short)Channels;
            waveFormat.SampleRate = SampleRate;
            waveFormat.AverageBytesPerSecond = AverageBytesPerSecond;
            waveFormat.BlockAlign = (short)BlockAlign;
            waveFormat.BitsPerSample = (short)BitsPerSample;
            waveFormat.ExtraSize = 0;
            return waveFormat;
        }

        /// <summary>
        /// Creates an A-law wave format
        /// </summary>
        /// <param name="SampleRate">Sample Rate</param>
        /// <param name="Channels">Number of Channels</param>
        /// <returns>Wave Format</returns>
        public static WaveFormat CreateALawFormat(int SampleRate, int Channels)
        {
            return CreateCustomFormat(WaveFormatEncoding.ALaw, SampleRate, Channels, SampleRate * Channels, Channels, 8);
        }

        /// <summary>
        /// Creates a Mu-law wave format
        /// </summary>
        /// <param name="SampleRate">Sample Rate</param>
        /// <param name="Channels">Number of Channels</param>
        /// <returns>Wave Format</returns>
        public static WaveFormat CreateMuLawFormat(int SampleRate, int Channels)
        {
            return CreateCustomFormat(WaveFormatEncoding.MuLaw, SampleRate, Channels, SampleRate * Channels, Channels, 8);
        }

        /// <summary>
        /// Creates a new PCM format with the specified sample rate, bit depth and Channels
        /// </summary>
        public WaveFormat(int rate, int bits, int Channels)
        {
            if (Channels < 1)
            {
                throw new ArgumentOutOfRangeException(Channels.ToString(), "Channels must be 1 or greater");
            }
            // minimum 16 bytes, sometimes 18 for PCM
            waveFormatTag = WaveFormatEncoding.Pcm;
            this.Channels = (short)Channels;
            SampleRate = rate;
            BitsPerSample = (short)bits;
            ExtraSize = 0;

            BlockAlign = (short)(Channels * (bits / 8));
            AverageBytesPerSecond = this.SampleRate * this.BlockAlign;
        }

        /// <summary>
        /// Creates a new 32 bit IEEE floating point wave format
        /// </summary>
        /// <param name="SampleRate">sample rate</param>
        /// <param name="Channels">number of Channels</param>
        public static WaveFormat CreateIeeeFloatWaveFormat(int SampleRate, int Channels)
        {
            var wf = new WaveFormat();
            wf.waveFormatTag = WaveFormatEncoding.IeeeFloat;
            wf.Channels = (short)Channels;
            wf.BitsPerSample = 32;
            wf.SampleRate = SampleRate;
            wf.BlockAlign = (short)(4 * Channels);
            wf.AverageBytesPerSecond = SampleRate * wf.BlockAlign;
            wf.ExtraSize = 0;
            return wf;
        }

        /// <summary>
        /// Helper function to marshal WaveFormat to an IntPtr
        /// </summary>
        /// <param name="format">WaveFormat</param>
        /// <returns>IntPtr to WaveFormat structure (needs to be freed by callee)</returns>
        public static IntPtr MarshalToPtr(WaveFormat format)
        {
            int formatSize = Marshal.SizeOf(format);
            IntPtr formatPointer = Marshal.AllocHGlobal(formatSize);
            Marshal.StructureToPtr(format, formatPointer, false);
            return formatPointer;
        }

        /// <summary>
        /// Reads in a WaveFormat (with extra data) from a fmt chunk (chunk identifier and
        /// length should already have been read)
        /// </summary>
        /// <param name="br">Binary reader</param>
        /// <param name="formatChunkLength">Format chunk length</param>
        /// <returns>A WaveFormatExtraData</returns>
        public static WaveFormat FromFormatChunk(BinaryReader br, int formatChunkLength)
        {
            var waveFormat = new WaveFormatExtraData();
            waveFormat.ReadWaveFormat(br, formatChunkLength);
            waveFormat.ReadExtraData(br);
            return waveFormat;
        }

        private void ReadWaveFormat(BinaryReader br, int formatChunkLength)
        {
            if (formatChunkLength < 16)
                throw new InvalidDataException("Invalid WaveFormat Structure");
            waveFormatTag = (WaveFormatEncoding)br.ReadUInt16();
            Channels = br.ReadInt16();
            SampleRate = br.ReadInt32();
            AverageBytesPerSecond = br.ReadInt32();
            BlockAlign = br.ReadInt16();
            BitsPerSample = br.ReadInt16();
            if (formatChunkLength > 16)
            {
                ExtraSize = br.ReadInt16();
                if (ExtraSize != formatChunkLength - 18)
                {
                    Debug.Log("Format chunk mismatch");
                    ExtraSize = (short)(formatChunkLength - 18);
                }
            }
        }

        /// <summary>
        /// Reads a new WaveFormat object from a stream
        /// </summary>
        /// <param name="br">A binary reader that wraps the stream</param>
        public WaveFormat(BinaryReader br)
        {
            int formatChunkLength = br.ReadInt32();
            ReadWaveFormat(br, formatChunkLength);
        }

        /// <summary>
        /// Reports this WaveFormat as a string
        /// </summary>
        /// <returns>String describing the wave format</returns>
        public override string ToString()
        {
            switch (waveFormatTag)
            {
                case WaveFormatEncoding.Pcm:
                case WaveFormatEncoding.Extensible:
                    // extensible just has some extra bits after the PCM header
                    return BitsPerSample + "bit PCM: " + SampleRate / 1000 + "kHz " + Channels + " Channels";
                default:
                    return waveFormatTag.ToString();
            }
        }

        /// <summary>
        /// Compares with another WaveFormat object
        /// </summary>
        /// <param name="obj">Object to compare to</param>
        /// <returns>True if the objects are the same</returns>
        public override bool Equals(object obj)
        {
            var other = obj as WaveFormat;
            if (other != null)
            {
                return waveFormatTag == other.waveFormatTag &&
                    Channels == other.Channels &&
                    SampleRate == other.SampleRate &&
                    AverageBytesPerSecond == other.AverageBytesPerSecond &&
                    BlockAlign == other.BlockAlign &&
                    BitsPerSample == other.BitsPerSample;
            }
            return false;
        }

        /// <summary>
        /// Provides a Hashcode for this WaveFormat
        /// </summary>
        /// <returns>A hashcode</returns>
        public override int GetHashCode()
        {
            return (int)waveFormatTag ^
                (int)Channels ^
                SampleRate ^
                AverageBytesPerSecond ^
                (int)BlockAlign ^
                (int)BitsPerSample;
        }

        /// <summary>
        /// Returns the encoding type used
        /// </summary>
        public WaveFormatEncoding Encoding { get { return waveFormatTag; } }

        /// <summary>
        /// Writes this WaveFormat object to a stream
        /// </summary>
        /// <param name="writer">the output stream</param>
        public virtual void Serialize(BinaryWriter writer)
        {
            writer.Write((int)(18 + ExtraSize)); // wave format length
            writer.Write((short)Encoding);
            writer.Write((short)Channels);
            writer.Write((int)SampleRate);
            writer.Write((int)AverageBytesPerSecond);
            writer.Write((short)BlockAlign);
            writer.Write((short)BitsPerSample);
            writer.Write((short)ExtraSize);
        }
    }
}
