using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassLibrary;

namespace RemoteServicesLibrary
{
    public interface IServerService{

        List<TupleClass> read(TupleClass tuple, string clientUrl, long nonce);

        TupleClass take(TupleClass tuple, string clientUrl, long nonce);

        void write(TupleClass tuple, string clientUrl, long nonce);

        void Freeze();

        void Unfreeze();
    }
}
