using ClassLibrary;
using RemoteServicesLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client {
    public class ClientService : IClientService{

        private TupleSpaceAPI _clientAPI;

        public ClientService(TupleSpaceAPI clientAPI) {
            _clientAPI = clientAPI;
        }

        public TupleClass TakeRemove(List<TupleClass> tupleSubset) {
            throw new NotImplementedException();
        }
    }
}
