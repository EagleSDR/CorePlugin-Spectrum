using EagleWeb.Common.IO.Sockets;
using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace EagleCore.Spectrum.Web
{
    class SpectrumClient
    {
        public SpectrumClient(IEagleSocketClient client)
        {
            this.client = client;
            client.OnReceiveJson += Client_OnReceiveJson;
        }

        private IEagleSocketClient client;

        private void Client_OnReceiveJson(IEagleSocketClient client, JObject data)
        {
            throw new NotImplementedException();
        }
    }
}
