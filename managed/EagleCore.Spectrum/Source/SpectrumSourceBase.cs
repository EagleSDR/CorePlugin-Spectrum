using EagleCore.Spectrum.Generator;
using EagleWeb.Common;
using EagleWeb.Common.Radio;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace EagleCore.Spectrum.Source
{
    /// <summary>
    /// Sources allow binding onto to get FFT data. Multiple sources can bind onto it.
    /// </summary>
    abstract unsafe class SpectrumSourceBase
    {
        public SpectrumSourceBase(SpectrumSourceManager manager, IEagleObject parent, string propertyName)
        {
            //Set
            this.manager = manager;
            this.parent = parent;
            this.propertyName = propertyName;

            //Register
            id = manager.Register(this);
        }

        private SpectrumSourceManager manager;
        private IEagleObject parent;
        private string propertyName;
        private long id;

        private List<ISpectrumSourceBinding> bindings = new List<ISpectrumSourceBinding>();
        private volatile bool enabled = false;
        private volatile bool removed = false;

        private SpectrumGeneratorImpl processor; // may be null
        private SpectrumBuffer<EagleComplex> conversion; // may be null

        public abstract float SampleRate { get; }
        public abstract bool IsComplex { get; }

        public IEagleObject Parent => parent;
        public long Id => id;

        /// <summary>
        /// Creates a listing with display information and an identifer
        /// </summary>
        /// <param name="msg"></param>
        public virtual JObject CreateListing(JObject msg)
        {
            msg["index"] = id;
            msg["id"] = CreateIdentifier(new JObject());
            return msg;
        }

        /// <summary>
        /// Creates an identifier that will be sent on the network that can then uniquely identify us, even through server reboots.
        /// </summary>
        /// <param name="msg"></param>
        public JObject CreateIdentifier(JObject msg)
        {
            msg["parent_name"] = parent.GetType().FullName;
            msg["parent_guid"] = parent.Guid;
            msg["name"] = propertyName;
            return msg;
        }

        /// <summary>
        /// Checks if the identifier matches our data.
        /// </summary>
        /// <param name="msg">The identifier to check.</param>
        /// <param name="coarse">If true, don't match the specific information. Just try to get as close as we can.</param>
        /// <returns></returns>
        public bool MatchIdentifier(JObject msg, bool coarse)
        {
            //Match ones for both coarse and fine
            if (!MatchIdentifierHelper(msg, "parent_name", parent.GetType().FullName))
                return false;
            if (!MatchIdentifierHelper(msg, "name", propertyName))
                return false;

            //Match fine
            if (!coarse && !MatchIdentifierHelper(msg, "parent_guid", parent.Guid))
                return false;

            return true;
        }

        private static bool MatchIdentifierHelper(JObject msg, string key, string challenge)
        {
            return msg.TryGetValue(key, out JToken value) && value.Type == JTokenType.String && (string)value == challenge;
        }

        /// <summary>
        /// Permenently removes this source
        /// </summary>
        public void Remove()
        {
            //Unregister
            manager.Unregister(this);
            removed = true;

            //Disable if it is
            lock(this)
            {
                enabled = false;
                if (processor != null)
                    processor.Dispose();
            }
        }

        public void Bind(ISpectrumSourceBinding binding)
        {
            lock (bindings)
            {
                //Add
                bindings.Add(binding);

                //Fire if this was the first
                if (bindings.Count == 1 && !enabled && !removed)
                    FirstClientAdded();
            }
        }

        public void Unbind(ISpectrumSourceBinding binding)
        {
            lock (bindings)
            {
                //Remove
                bool removed = bindings.Remove(binding);

                //Fire if this was the last
                if (bindings.Count == 0 && removed && !this.removed)
                    LastClientRemoved();
            }
        }

        private void FirstClientAdded()
        {
            //Create processor if needed
            if (processor != null)
                processor = new SpectrumGeneratorImpl(this, 16384);

            //Change state
            enabled = true;
        }

        private void LastClientRemoved()
        {
            //Change state
            enabled = false;
        }

        private EagleComplex* GetConversionBuffer(int count)
        {
            //If there already is a conversion buffer and it's <= the size, use it
            if (conversion != null && conversion.Count <= count)
                return conversion;

            //Destroy existing buffer, if any
            if (conversion != null)
                conversion.Dispose();

            //Create
            conversion = new SpectrumBuffer<EagleComplex>(count);
            return conversion;
        }

        protected void NotifySampleRateChanged()
        {
            //Send event to clients
            lock (bindings)
            {
                foreach (var b in bindings)
                    b.SampleRateChanged(SampleRate);
            }
        }

        protected void NotifySamplesReceived(EagleComplex* samples, int count)
        {
            //Send samples to processor...if available
            lock(this)
            {
                if (processor != null && enabled)
                    processor.Input(samples, count);
            }
        }

        protected void NotifySamplesReceived(float* samples, int count)
        {
            //Get conversion buffer
            var buffer = GetConversionBuffer(count);

            //Convert
            for (int i = 0; i < count; i++)
                buffer[i] = new EagleComplex(samples[i], 0);

            //Run
            NotifySamplesReceived(buffer, count);
        }

        protected void NotifySamplesReceived(EagleStereoPair* samples, int count)
        {
            //Get conversion buffer
            var buffer = GetConversionBuffer(count);

            //Convert
            for (int i = 0; i < count; i++)
                buffer[i] = new EagleComplex(samples[i].Average, 0);

            //Run
            NotifySamplesReceived(buffer, count);
        }

        class SpectrumGeneratorImpl : SpectrumGenerator
        {
            public SpectrumGeneratorImpl(SpectrumSourceBase manager, int fft_size) : base(fft_size)
            {
                this.manager = manager;
            }

            private SpectrumSourceBase manager;

            protected override void FrameDropped()
            {
                //Send event to clients
                lock (manager.bindings)
                {
                    foreach (var b in manager.bindings)
                        b.FrameDropped();
                }
            }

            protected override void FrameEmitted(float* frame)
            {
                //Send event to clients
                lock (manager.bindings)
                {
                    foreach (var b in manager.bindings)
                        b.FrameEmitted(frame, FftSize);
                }
            }
        }
    }
}
