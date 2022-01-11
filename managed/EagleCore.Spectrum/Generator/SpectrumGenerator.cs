using EagleWeb.Common.Radio;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace EagleCore.Spectrum.Generator
{
    /// <summary>
    /// Computes samples and creates an FFT.
    /// </summary>
    abstract unsafe class SpectrumGenerator : IDisposable
    {
        public SpectrumGenerator(int fft_size)
        {
            //Set up
            this.fft_size = fft_size;

            //Create the buffer
            buffer = new SpectrumBuffer<EagleComplex>(fft_size);

            //Create FFT
            fft = new SpectrumFFT(fft_size);

            //Start up thread
            fftThread = new Thread(WorkerThread);
            fftThread.Name = "FFT Worker Thread";
            fftThread.Start();
        }

        public void Dispose()
        {
            //Signal and wait for thread to exit
            isExiting = true;
            fftWaiter.Set();
            fftThread.Join();

            //Dispose of the FFT and buffer
            buffer.Dispose();
            fft.Dispose();
        }

        /* SETTINGS */

        private int fft_size;
        private int block_size;
        private float fps;
        private float sample_rate;

        /* MISC */

        private int buffer_usage;
        private SpectrumBuffer<EagleComplex> buffer;
        private SpectrumFFT fft;
        private ManualResetEvent fftWaiter = new ManualResetEvent(false);
        private Thread fftThread;
        private volatile bool isExiting = false;

        /* GETTERS/SETTERS */

        public int FftSize => fft_size;

        public float Fps
        {
            get => fps;
            set
            {
                if (fps < 1 || fps > 120)
                    throw new ArgumentOutOfRangeException();
                fps = value;
                Configure();
            }
        }

        public float SampleRate
        {
            get => sample_rate;
            set
            {
                sample_rate = value;
                Configure();
            }
        }

        /* ABSTRACT */

        protected abstract void FrameEmitted(float* frame);
        protected abstract void FrameDropped();

        /* CORE */

        public void Input(EagleComplex* input, int count)
        {
            //If the block size is 0, this is invalid; refuse
            if (block_size == 0)
                return;

            //Enter loop
            while (count > 0)
            {
                //If our buffer position is less than 0, consume but don't write until we reach 0
                if (buffer_usage < 0)
                {
                    //Consume
                    int add = Math.Min(count, -buffer_usage);
                    buffer_usage += add;
                    input += add;
                    count -= add;

                    //If our buffer usage is still less than zero, do nothing
                    if (buffer_usage < 0)
                        return;
                }

                //Get the number we'll consume to not read too much AND read up to a block boundry
                int consumed = Math.Min(fft_size - buffer_usage, count);

                //Write to the buffer and consume
                Memcpy(buffer.Pointer + buffer_usage, input, consumed);
                buffer_usage += consumed;
                input += consumed;
                count -= consumed;

                //Check if it's time to submit a block
                if (buffer_usage == fft_size)
                    PushBlock();
            }
        }

        private void PushBlock()
        {
            //Check if it is already set
            if (fftWaiter.WaitOne(0))
            {
                //Push block to the worker thread
                Memcpy(fft.Input, buffer, fft_size);

                //Signal
                fftWaiter.Set();
            } else
            {
                //Still processing. This is a dropped frame...
                FrameDropped();
            }

            //Update; this may or may not make it negative
            buffer_usage -= block_size;

            //Move everything in the buffer block_size samples down, keeping buffer_usage
            if (buffer_usage > 0)
                Memcpy(buffer, buffer.Pointer + block_size, buffer_usage);
        }

        private void Configure()
        {
            //Set
            if (sample_rate > 0 && fps > 0)
                block_size = (int)(sample_rate / fps);
            else
                block_size = 0;

            //Reset
            buffer_usage = fft_size - block_size;
        }

        private void WorkerThread()
        {
            while (!isExiting)
            {
                //Wait for an event
                fftWaiter.WaitOne();

                //Check if exiting
                if (isExiting)
                    break;

                //Compute
                fft.ComputeBlock();

                //Send
                FrameEmitted(fft.Output);

                //Reset
                fftWaiter.Reset();
            }
        }

        /* UTILS */

        private static void Memcpy<T>(T* dst, T* src, long count) where T : unmanaged
        {
            Buffer.MemoryCopy(src, dst, count * sizeof(T), count * sizeof(T));
        }
    }
}
