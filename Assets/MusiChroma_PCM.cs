using System;
using System.Collections.Generic;
using System.Diagnostics;
using Accord.Math;
using CSCore.CoreAudioAPI;
using CSCore.SoundIn;
using UnityEngine;
using ThreadPriority = System.Threading.ThreadPriority;
using Vector3 = UnityEngine.Vector3;

namespace Assets
{
    public class MusiChroma_PCM : MonoBehaviour
    {
        public Color StaticColor;
        public float ColorFadeSpeedMultiplier = 1;
        public float HeightMultiplier = 1;
        public int Points = 100;

        private int BUFFERSIZE = (int)Math.Pow(2, 11); // must be a multiple of 2

        private WasapiLoopbackCapture capture;

        private BufferedWaveProvider bwp;
        private LineRenderer lr;

        private TimeSpan timeSpan;
        private Stopwatch stopWatch;

        double angle;

        void Start()
        {
            Application.runInBackground = true;

            timeSpan = TimeSpan.FromMilliseconds(5);
            stopWatch = new Stopwatch();
            stopWatch.Start();

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
            bwp = new BufferedWaveProvider(new WaveFormat(device.DeviceFormat.SampleRate, device.DeviceFormat.BitsPerSample, device.DeviceFormat.Channels));
        }

        public Vector3[] getWaves()
        {
            try
            {
                // read the bytes from the stream
                int frameSize = BUFFERSIZE;
                var frames = new byte[frameSize];

                bwp.Read(frames, 0, frameSize);
                bwp.ClearBuffer();

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

                List<Vector3> avg = new List<Vector3>();

                if (Points > Ys.Length)
                    return null;

                int length = Ys.Count(d => d != 0);

                int step = length / Points;

                float height = Camera.main.orthographicSize * 2.0f;
                float width = height * Screen.width / Screen.height;

                for (int i = 0; i < length; i++)
                {
                    if (i % step == 0)
                    {
                        Vector3 vec = new Vector3();

                        //AVG
                        int added = 0;

                        for (int j = 0; j < step && i + j < length; j++)
                        {
                            vec.y += Ys[i + j] / 44100f * HeightMultiplier;
                            vec.z += ((i + j) / (float)length) * width - width / 2;

                            added++;
                        }

                        vec /= added;

                        avg.Add(vec);
                    }
                }

                return avg.ToArray();
            }
            catch
            {
            }

            return null;
        }

        private void dataAvailable(object sender, DataAvailableEventArgs e)
        {
            bwp.AddSamples(e.Data, 0, e.ByteCount);
        }

        void Update()
        {
            if (stopWatch.Elapsed >= timeSpan)
            {
                var waves = getWaves();

                if (waves != null)
                {
                    lr.positionCount = waves.Length;
                    lr.SetPositions(waves);
                }

                if (ColorFadeSpeedMultiplier > 0)
                {
                    angle += 2 * ColorFadeSpeedMultiplier;

                    if (angle > 360)
                        angle = 0;

                    lr.material.color = Hue(angle);
                }
                else
                    lr.material.color = StaticColor;

                stopWatch.Stop();
                stopWatch.Reset();
                stopWatch.Start();
            }
        }

        void OnApplicationQuit()
        {
            try
            {
                capture.Stop();
                capture.Dispose();
            }
            catch { }
        }

        Color Hue(double angle)
        {
            double rad = Math.PI / 180 * angle;

            float red = (float)(Math.Sin(rad) * 127 + 128);
            float green = (float)(Math.Sin((rad + 2 * Math.PI / 3)) * 127 + 128);
            float blue = (float)(Math.Sin((rad + 4 * Math.PI / 3)) * 127 + 128);

            return new Color(red / 255f, green / 255f, blue / 255f);
        }

        double random(System.Random r, double min, double max)
        {
            return min + r.NextDouble() * (max - min);
        }
    }
}