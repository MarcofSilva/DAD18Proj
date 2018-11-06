using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RemoteServicesLibrary;


namespace Client {
    class ClientService : MarshalByRefObject, IClientService {
        public void WriteResponse(ArrayList ack) {
            throw new NotImplementedException();
        }

        public void ReadResponse(ArrayList ack, List<ArrayList> tuple) {
            throw new NotImplementedException();
        }

        public void TakeResponse(ArrayList ack, List<ArrayList> tuple) {
            throw new NotImplementedException();
        }
    }
}
