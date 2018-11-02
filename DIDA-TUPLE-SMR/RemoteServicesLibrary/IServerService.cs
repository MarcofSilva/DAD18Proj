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
        void Read(ArrayList tuple, string clientUrl);

        void Take(ArrayList tuple, string clientUrl);

        void Write(ArrayList tuple, string clientUrl);
    }
}
