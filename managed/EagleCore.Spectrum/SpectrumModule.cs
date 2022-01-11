using EagleCore.Spectrum.Source;
using EagleCore.Spectrum.Source.Implementations;
using EagleWeb.Common;
using EagleWeb.Common.IO;
using EagleWeb.Common.Misc;
using EagleWeb.Common.Plugin;
using EagleWeb.Common.Radio;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace EagleCore.Spectrum
{
    public class SpectrumModule : EagleObjectPlugin
    {
        public SpectrumModule(IEagleObjectPluginContext context) : base(context)
        {
            
        }

        private SpectrumSourceManager manager = new SpectrumSourceManager();

        public override void PluginInit()
        {
            //Bind to radio
            new SpectrumSourcePort(manager, Context.Radio)
                .Bind(Context.Radio.PortInput);
            Context.Radio.OnSessionCreated += Radio_OnSessionCreated;
            Context.Radio.OnSessionRemoved += Radio_OnSessionRemoved;
        }

        private void Radio_OnSessionCreated(IEagleRadio radio, IEagleRadioSession session)
        {
            //Bind to system endpoints
            new SpectrumSourcePort(manager, session)
                .Bind(session.PortVFO);
            new SpectrumSourcePort(manager, session)
                .Bind(session.PortIF);
            new SpectrumSourcePort(manager, session)
                .Bind(session.PortAudio);
        }

        private void Radio_OnSessionRemoved(IEagleRadio radio, IEagleRadioSession session)
        {
            //Remove all children of this that we registered earlier
            manager.UnregisterChildren(session);
        }
    }
}
