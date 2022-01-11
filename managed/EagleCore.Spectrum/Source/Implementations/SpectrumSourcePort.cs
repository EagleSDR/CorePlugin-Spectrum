using EagleWeb.Common;
using EagleWeb.Common.Radio;
using System;
using System.Collections.Generic;
using System.Text;

namespace EagleCore.Spectrum.Source.Implementations
{
    unsafe class SpectrumSourcePort : SpectrumSourceBase 
    {
        public SpectrumSourcePort(SpectrumSourceManager manager, IEagleObject parent) : base(manager, parent)
        {
            
        }

        private float sampleRate;
        private bool isComplex;

        public void Bind<T>(IEagleRadioPort<T> port) where T : unmanaged
        {
            //Bind depending on the type
            bool success = BindHelper<T, EagleComplex>(port, Port_OnOutput) ||
                BindHelper<T, float>(port, Port_OnOutput) ||
                BindHelper<T, EagleStereoPair>(port, Port_OnOutput);

            //If the type wasn't any of those, just abort
            if (!success)
                return;

            //Bind simple
            port.OnSampleRateChanged += Port_OnSampleRateChanged;

            //Set args
            sampleRate = port.SampleRate;
            isComplex = typeof(T) == typeof(EagleComplex);
        }

        public override float SampleRate => sampleRate;
        public override bool IsComplex => isComplex;

        private void Port_OnSampleRateChanged<T>(IEagleRadioPort<T> port, float sampleRate) where T : unmanaged
        {
            this.sampleRate = sampleRate;
            NotifySampleRateChanged();
        }

        private void Port_OnOutput(IEagleRadioPort<EagleComplex> port, EagleComplex* buffer, int count)
        {
            NotifySamplesReceived(buffer, count);
        }

        private void Port_OnOutput(IEagleRadioPort<float> port, float* buffer, int count)
        {
            NotifySamplesReceived(buffer, count);
        }

        private void Port_OnOutput(IEagleRadioPort<EagleStereoPair> port, EagleStereoPair* buffer, int count)
        {
            NotifySamplesReceived(buffer, count);
        }

        private static bool BindHelper<RealType, CheckType>(IEagleRadioPort<RealType> port, IEagleRadioPort_Output<CheckType> binding) where CheckType : unmanaged where RealType : unmanaged
        {
            //Check if this type matches
            if (typeof(CheckType) != typeof(RealType))
                return false;

            //Bind
            (port as IEagleRadioPort<CheckType>).OnOutput += binding;

            return true;
        }
    }
}
