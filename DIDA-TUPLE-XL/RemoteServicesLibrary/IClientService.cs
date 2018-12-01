using ClassLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteServicesLibrary {
    public interface IClientService {

        TupleClass TakeRemove(List<TupleClass> tupleSubset);

        void Freeze();

        void Unfreeze();
    }
}
