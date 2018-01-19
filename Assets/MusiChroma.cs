using System;
using System.Collections.Generic;
using System.Timers;
using Accord.Math;
using CSCore.CoreAudioAPI;
using CSCore.SoundIn;
using UnityEngine;
using ThreadPriority = System.Threading.ThreadPriority;
using Vector3 = UnityEngine.Vector3;

namespace Assets
{
    public class MusiChroma : MonoBehaviour
    {
        public GameObject Object;

        public float MaxWidth = 3;

        public int Points = 100;

        public int BarStart = 18;
        public int BarEnd = 30;

        public float Tolerance = 0.18f;

        private WasapiLoopbackCapture capture;

        private LineRenderer lr;
        private AudioSource audioSource;
        
        private int BUFFERSIZE = 2048;//(int)Math.Pow(2, 11); // must be a multiple of 2

        BufferedWaveProvider bwp;

        Timer captureTimer = new Timer();

        System.Random r = new System.Random();

        int pending;

        float lastY = 0;

        int angle;

        Vector3 startVec;
        float multiplier = 1;

        WaveFormat NFormat;

        Vector3[] vec;

        void Start()
        {
            Application.runInBackground = true;

            captureTimer.Interval = 15;
            captureTimer.Elapsed += T_Elapsed;
            captureTimer.Enabled = true;

            var enu = new MMDeviceEnumerator();

            var device = enu.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            capture = new WasapiLoopbackCapture(0, device.DeviceFormat, ThreadPriority.AboveNormal)
            {
                Device = device
            };

            capture.DataAvailable += dataAvailable;
            capture.Initialize();
            capture.Start();

            lr = GetComponent<LineRenderer>();
            audioSource = GetComponent<AudioSource>();

            var t = Object.GetComponent<Transform>();
            startVec = new Vector3(t.localScale.x, t.localScale.y, t.localScale.z);

            bwp = new BufferedWaveProvider(NFormat = new WaveFormat(device.DeviceFormat.SampleRate, device.DeviceFormat.BitsPerSample, device.DeviceFormat.Channels));
        }

        private void T_Elapsed(object sender, ElapsedEventArgs e)
        {
            captureTimer.Enabled = false;

            /* var v = getWaves();

             if (v != null)
             {
                 vec = v;

                 //check for frequency occurence changes
                 float y = 0;
                 int total = 0;

                 try
                 {
                     for (int i = BarStart; i < BarEnd && i < v.Length; i++)
                     {
                         y += v[i].y;
                         total++;
                     }
                 }
                 catch { }

                 y /= total;

                 if (Math.Abs(y - lastY) >= Tolerance)
                     pending++;

                 lastY = y;
             }*/
        }

        public double[] FFT(double[] data)
        {
            double[] fft = new double[data.Length]; // this is where we will store the output (fft)
            Accord.Compat.Complex[] fftComplex = new Accord.Compat.Complex[data.Length]; // the FFT function requires complex format
            for (int i = 0; i < data.Length; i++)
            {
                fftComplex[i] = new Accord.Compat.Complex(data[i], 0.0); // make it complex format (imaginary = 0)
            }
            FourierTransform.FFT(fftComplex, FourierTransform.Direction.Forward);
            for (int i = 0; i < data.Length; i++)
            {
                fft[i] = fftComplex[i].Magnitude; // back to double
                                                  // fft[i] = Math.Log10(fft[i]); // convert to dB
            }
            return fft;
            //todo: this could be much faster by reusing variables
        }

        public Vector3[] getWaves()
        {
            try
            {
                //return new Vector3[] { };

                // read the bytes from the stream
                int frameSize = BUFFERSIZE;
                var frames = new byte[frameSize];
                bwp.Read(frames, 0, frameSize);
                bwp.ClearBuffer();
                //MemoryStream s = new MemoryStream();
                //WaveFileWriter wfw = new WaveFileWriter(s, NFormat);

                int SAMPLE_RESOLUTION = 16;
                int BYTES_PER_POINT = SAMPLE_RESOLUTION / 8;
                Int32[] vals = new Int32[frames.Length / BYTES_PER_POINT];
                float[] Ys = new float[frames.Length / BYTES_PER_POINT];

                for (int i = 0; i < vals.Length; i++)
                {
                    // bit shift the byte buffer into the right variable format
                    byte hByte = frames[i * 2 + 1];
                    byte lByte = frames[i * 2 + 0];
                    vals[i] = (short)((hByte << 8) | lByte);
                    Ys[i] = vals[i];
                }

                List<Vector3> v = new List<Vector3>();

                int length = Ys.Count(d => d != 0);

                for (int i = 0; i < length; i++)
                {
                    float f = Ys[i];

                    float x = (i / (float)length) * MaxWidth - MaxWidth / 2;
                    float y = f / 44100f;


                    v.Add(new Vector3(0, y, x));
                }

                List<Vector3> avg = new List<Vector3>();

                int step = v.Count / Points;

                if (Points < v.Count)
                {
                    for (int i = 0; i < v.Count; i += step)
                    {
                        Vector3 vec = new Vector3();

                        int added = 0;

                        for (int j = 0; j < step && i + j < v.Count; j++)
                        {
                            vec += v[i + j];
                            added++;
                        }

                        vec /= added;

                        avg.Add(vec);
                    }
                }

                return avg.ToArray();

                /*
                //WAV w = new WAV(y, NFormat);
                //TOTO - FIX, ALSO FIND CORRECT FREQUENCY
                AudioClip clip = AudioClip.Create("test", Ys.Length, NFormat.Channels, BYTES_PER_POINT * 1000, false);
                clip.SetData(Ys, 0);

                Debug.Log("about to play clip");
                audioSource.clip = clip;
                float[] specData = new float[256];

                for (int i = 0; i < specData.Length; i++)
                {
                    specData[i] = -1;
                }
                audioSource.Play();
                Debug.Log("started playing clip");
                audioSource.GetSpectrumData(specData, 1, FFTWindow.BlackmanHarris);

                Debug.Log("spectrum data aquired");

                List<Vector3> v = new List<Vector3>();

                for (int i = 0; i < specData.Length; i++)
                {
                    float f = specData[i];

                    if (f != -1)
                    {
                        v.Add(new Vector3(0, f, i / 256f * MaxWidth));
                    }
                }
                audioSource.Stop();

                return v.ToArray();
                */










                /*
                if (frames.Length == 0) return null;
                if (frames[frameSize - 2] == 0) return null;

                // convert it to int32 manually (and a double for scottplot)
                int SAMPLE_RESOLUTION = 16;
                int BYTES_PER_POINT = SAMPLE_RESOLUTION / 8;
                Int32[] vals = new Int32[frames.Length / BYTES_PER_POINT];
                double[] Ys = new double[frames.Length / BYTES_PER_POINT];
                double[] Xs = new double[frames.Length / BYTES_PER_POINT];
                double[] Ys2 = new double[frames.Length / BYTES_PER_POINT];
                double[] Xs2 = new double[frames.Length / BYTES_PER_POINT];
                for (int i = 0; i < vals.Length; i++)
                {
                    // bit shift the byte buffer into the right variable format
                    byte hByte = frames[i * 2 + 1];
                    byte lByte = frames[i * 2 + 0];
                    vals[i] = (short)((hByte << 8) | lByte);
                    Xs[i] = i;
                    Ys[i] = vals[i];
                    Xs2[i] = (double)i / Ys.Length * RATE / 1000.0; // units are in kHz
                }

                // update scottplot (PCM, time domain)
                // scottPlotUC1.Xs = Xs;
                //scottPlotUC1.Ys = Ys;

                Ys2 = FFT(Ys);

                // Xs = Xs2.Take(Xs2.Length / 2).ToArray();
                // Ys = Ys2.Take(Ys2.Length / 2).ToArray();

                Xs = new double[Xs2.Length / 2];
                Ys = new double[Ys2.Length / 2];

                for (int i = 0; i < Xs2.Length / 2; i++)
                {
                    Xs[i / 2] += Xs2[i / 2];
                    Ys[i / 2] += Ys2[i / 2];
                }

                List<Vector3> l = new List<Vector3>();

                for (int i = 0; i < Xs.Length; i++)
                {
                    float X = (float)Xs[i] * 5;
                    float Y = (float)Ys[i] / 100;

                    if (X == 0 && Y == 0)
                        break;

                    l.Add(new Vector3(0, Y * 0.5f, X));
                }

                bwp.ClearBuffer();

                return l.ToArray();

                //update scottplot (FFT, frequency domain)

                //scottPlotUC2.Xs = Xs2.Take(Xs2.Length / 2).ToArray();
                //scottPlotUC2.Ys = Ys2.Take(Ys2.Length / 2).ToArray();
                */
            }
            catch
            {
            }

            return null;
        }

        private void dataAvailable(object sender, DataAvailableEventArgs e)
        {
            bwp.AddSamples(e.Data, 0, e.ByteCount);

            #region unused
            /*
                        Debug.Log("creatign a wav object");
                        var wav = new WAV(e.Data);
                        Debug.Log(wav);
            
                        AudioSource audio = new AudioSource();
            
                        Debug.Log("creating clip");
                        AudioClip audioClip = AudioClip.Create("testSound", wav.SampleCount, 1, wav.Frequency, false, false);
                        Debug.Log("setting clip data");
                        audioClip.SetData(wav.LeftChannel, 0);
                        audio.clip = audioClip;
                        Debug.Log("playing clip");
                        audio.Play();
            
                        FFTWindow win = FFTWindow.Rectangular;
            
                        
                        Debug.Log("getting spectrum data");
                        audio.GetSpectrumData(buffer, 1, win);
                        audio.Stop();
                        
                       

            float[] buffer = new float[e.ByteCount / 2];

            for (int i = 0; i < e.ByteCount; i += 2)
            {
                var sample = e.Data[i];
                buffer[i / 2] = sample / 32768f;
            }


            processBuffer(buffer);

            /*
            for (int i = 0; i < e.ByteCount; i += 2)
            {
                short sample = (short)((e.Data[i + 1] << 8) |
                                       e.Data[i + 0]);
                buffer[i / 2] = sample;
                //buffer[i / 2] = sample / 32768f; //IEEE 32 floating number 
            }*/

            //float[] buffer = new float[e.ByteCount];

            // for (int index = 0; index < e.ByteCount; ++index)
            // buffer[index] = (e.Data[index] - 128) * (1f / 128f);

            //SampleConverter.Convert(e.Data, buffer);

            //processBuffer(buffer);*/
            #endregion
        }

        void Update()
        {
            lastY = Mathf.Clamp(lastY - 0.001f, 0, float.MaxValue);

            multiplier = Mathf.Clamp(multiplier * 0.975f, 1, 2);
            Object.transform.localScale = startVec * multiplier;

            if (!captureTimer.Enabled)//vec != null)
            {
                var waves = getWaves();

                if (waves != null)
                {
                    lr.positionCount = waves.Length;
                    lr.SetPositions(waves);
                }

                captureTimer.Enabled = true;
            }

            //TODO MERGE
            if (pending > 0)
            {
                Debug.Log("Change! " + r.Next());
                var mr = Object.GetComponent<MeshRenderer>();
                mr.materials[0].color = Hue(angle);

                multiplier = 1.25f;

                if (angle >= 360)
                    angle = 0;
                else
                    angle += 30;

                pending--;
            }
        }

        void OnDestroy()
        {
            capture.Stop();
            capture.Dispose();
        }

        Color Hue(double angle)
        {
            double rad = Math.PI / 180;

            float red = (float)(Math.Sin(angle * rad) * 127 + 128);
            float green = (float)(Math.Sin((angle * rad + 2 * Math.PI / 3)) * 127 + 128);
            float blue = (float)(Math.Sin((angle * rad + 4 * Math.PI / 3)) * 127 + 128);

            return new Color(red / 255f, green / 255f, blue / 255f);
        }

        double random(System.Random r, double min, double max)
        {
            return min + r.NextDouble() * (max - min);
        }
    }

    class Average
    {
        private List<double> samples;
        public int Samples = 2;

        public Average()
        {
            samples = new List<double>();
        }

        public bool IsReady()
        {
            return samples.Count >= Samples;
        }

        public void Add(double d)
        {
            samples.Add(d);
        }

        public double GetAverage()
        {
            double total = 0;

            foreach (var d in samples)
                total += d;

            samples.Clear();

            return total / Samples;
        }
    }
}