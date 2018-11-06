using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteServicesLibrary
{
    public interface IServerService
    {
        void Read(ArrayList tuple, string clientUrl, long nonce);

        void Take(ArrayList tuple, string clientUrl, long nonce);

        void Write(ArrayList tuple, string clientUrl, long nonce);
    }
}
