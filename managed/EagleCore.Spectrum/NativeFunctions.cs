using EagleWeb.Common.Radio;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace EagleCore.Spectrum
{
    static unsafe class NativeFunctions
    {
        private const string FFTW_NAME = "libfftw3f";

        public const int FFTW_FORWARD = -1;
        public const uint FFTW_ESTIMATE = 64;

        [DllImport(FFTW_NAME)]
        public static extern IntPtr fftw_malloc(long size);

        [DllImport(FFTW_NAME)]
        public static extern void fftw_free(IntPtr buffer);

        [DllImport(FFTW_NAME)]
        public static extern IntPtr fftwf_plan_dft_1d(int size, EagleComplex* input, EagleComplex* output, int sign, uint flags);

        [DllImport(FFTW_NAME)]
        public static extern void fftwf_execute(IntPtr plan);

        [DllImport(FFTW_NAME)]
        public static extern void fftwf_destroy_plan(IntPtr plan);
    }
}
