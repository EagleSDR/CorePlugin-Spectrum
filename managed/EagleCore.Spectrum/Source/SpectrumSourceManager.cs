using EagleWeb.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace EagleCore.Spectrum.Source
{
    class SpectrumSourceManager
    {
        public SpectrumSourceManager()
        {

        }

        private long nextId = 1;
        private List<SpectrumSourceBase> sources = new List<SpectrumSourceBase>();

        public long Register(SpectrumSourceBase source)
        {
            //Get ID
            long id = nextId++;

            //Add
            lock (sources)
                sources.Add(source);

            return id;
        }

        public void Unregister(SpectrumSourceBase source)
        {
            //Remove
            lock (sources)
                sources.Remove(source);
        }

        public void UnregisterChildren(IEagleObject parent)
        {
            lock (sources)
            {
                //Search for matches
                List<SpectrumSourceBase> matches = new List<SpectrumSourceBase>();
                foreach (var c in sources)
                {
                    if (c.Parent == parent)
                        matches.Add(c);
                }

                //Remove all
                foreach (var m in matches)
                    m.Remove();
            }
        }
    }
}
