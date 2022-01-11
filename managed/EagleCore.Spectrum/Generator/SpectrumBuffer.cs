using System;
using System.Collections.Generic;
using System.Text;

namespace EagleCore.Spectrum.Generator
{
    unsafe class SpectrumBuffer<T> where T : unmanaged
    {
        public SpectrumBuffer(long count)
        {
            this.count = count;
            buffer = NativeFunctions.fftw_malloc(SizeInBytes);
        }

        private long count;
        private IntPtr buffer;

        public long Count => count;
        public long SizeInBytes => Count * sizeof(T);
        public IntPtr Handle => GetSaferHandle();
        public T* Pointer => (T*)Handle.ToPointer();

        public static implicit operator T*(SpectrumBuffer<T> ctx)
        {
            return ctx.Pointer;
        }

        private IntPtr GetSaferHandle()
        {
            if (buffer == IntPtr.Zero)
                throw new ObjectDisposedException(GetType().Name);
            return buffer;
        }

        public void Dispose()
        {
            if (buffer != IntPtr.Zero)
            {
                NativeFunctions.fftw_free(buffer);
                buffer = IntPtr.Zero;
            }
        }
    }
}
