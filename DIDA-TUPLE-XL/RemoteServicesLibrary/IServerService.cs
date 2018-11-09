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
        ArrayList Read(ArrayList tuple, string clientUrl, long nonce);

        ArrayList TakeRead(ArrayList tuple, string clientUrl, long nonce);

        void TakeRemove(ArrayList tuple, string clientUrl, long nonce);

        void Write(ArrayList tuple, string clientUrl, long nonce);
    }
}
