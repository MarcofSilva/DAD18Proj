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
        void Read(ArrayList tuple);

        void Take(ArrayList tuple);

        void Write(ArrayList tuple);
    }
}
