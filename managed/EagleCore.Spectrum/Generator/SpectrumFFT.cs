using EagleWeb.Common.Radio;
using System;
using System.Collections.Generic;
using System.Text;

namespace EagleCore.Spectrum.Generator
{
    unsafe class SpectrumFFT : IDisposable
    {
        public SpectrumFFT(int fft_size)
        {
            //Set
            this.fft_size = fft_size;

            //Allocate
            fft_in = new SpectrumBuffer<EagleComplex>(fft_size);
            fft_out = new SpectrumBuffer<EagleComplex>(fft_size);
            power = new SpectrumBuffer<float>(fft_size);
            window = new SpectrumBuffer<float>(fft_size);

            //Create the FFT window
            coswindow(window, fft_size, 0.44959f, 0.49364f, 0.05677f);

            //Create plan
            plan = NativeFunctions.fftwf_plan_dft_1d(fft_size, fft_in, fft_out, NativeFunctions.FFTW_FORWARD, NativeFunctions.FFTW_ESTIMATE);
        }

        public void Dispose()
        {
            //Dispose of buffers
            fft_in.Dispose();
            fft_out.Dispose();
            power.Dispose();
            window.Dispose();

            //Destroy plan
            if (plan != IntPtr.Zero)
            {
                NativeFunctions.fftwf_destroy_plan(plan);
                plan = IntPtr.Zero;
            }
        }

        /* BUFFERS */

        private SpectrumBuffer<EagleComplex> fft_in;
        private SpectrumBuffer<EagleComplex> fft_out;
        private SpectrumBuffer<float> power;
        private SpectrumBuffer<float> window;

        /* MISC */

        private int fft_size;
        private IntPtr plan;

        /* CORE */

        public int Size => fft_size;
        public EagleComplex* Input => fft_in;
        public float* Output => power;

        public void ComputeBlock()
        {
            //Apply windowing function
            apply_window(fft_in, window, fft_size);

            //Compute the FFT
            NativeFunctions.fftwf_execute(plan);

            //Offset the spectrum to center
            offset_spectrum(fft_out, fft_size);

            //Calculate magnitude squared
            volk_32fc_magnitude_squared_32f_generic(this.power, fft_out, fft_size);

            //Scale to dB
            //https://github.com/gnuradio/gnuradio/blob/1a0be2e6b54496a8136a64d86e372ab219c6559b/gr-utils/plot_tools/plot_fft_base.py#L88
            float* power = this.power;
            for (int i = 0; i < fft_size; i++)
                power[i] = 20 * MathF.Log10(MathF.Abs((power[i] + 1e-15f) / fft_size));
        }

        /* UTILS */

        //https://github.com/gnuradio/gnuradio/blob/master/gr-fft/lib/window.cc
        private static void coswindow(float* taps, int ntaps, float c0, float c1, float c2)
        {
            float M = (ntaps - 1);

            for (int n = 0; n < ntaps; n++)
                taps[n] = c0 - c1 * MathF.Cos((2.0f * MathF.PI * n) / M) +
                          c2 * MathF.Cos((4.0f * MathF.PI * n) / M);
        }

        private static void offset_spectrum(EagleComplex* buffer, int count)
        {
            count /= 2;
            EagleComplex* left = buffer;
            EagleComplex* right = buffer + count;
            EagleComplex temp;
            for (int i = 0; i < count; i++)
            {
                temp = *left;
                *left++ = *right;
                *right++ = temp;
            }
        }

        private static void apply_window(EagleComplex* buffer, float* taps, int count)
        {
            float* dst = (float*)buffer;
            for (int i = 0; i < count; i++)
            {
                *dst++ *= *taps;
                *dst++ *= *taps++;
            }
        }

        //https://github.com/gnuradio/volk/blob/237a6fc9242ea8c48d2bbd417a6ea14feaf7314a/kernels/volk/volk_32fc_magnitude_squared_32f.h
        private static void volk_32fc_magnitude_squared_32f_generic(float* magnitudeVector, EagleComplex* complexVector, int num_points)
        {
            float* complexVectorPtr = (float*)complexVector;
            float* magnitudeVectorPtr = magnitudeVector;
            int number = 0;
            for (number = 0; number < num_points; number++)
            {
                float real = *complexVectorPtr++;
                float imag = *complexVectorPtr++;
                *magnitudeVectorPtr++ = (real * real) + (imag * imag);
            }
        }
    }
}
