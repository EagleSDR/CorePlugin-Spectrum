using System;
using System.Collections.Generic;
using System.Text;

namespace EagleCore.Spectrum.Source
{
    /// <summary>
    /// Accepts events from a spectrum source
    /// </summary>
    unsafe interface ISpectrumSourceBinding
    {
        void SampleRateChanged(float sampleRate);
        void FrameEmitted(float* frame, int size);
        void FrameDropped();
        void SourceRemoved();
    }
}
