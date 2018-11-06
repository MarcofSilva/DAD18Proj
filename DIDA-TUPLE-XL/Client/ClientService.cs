using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RemoteServicesLibrary;


namespace Client {
    public class ClientService : MarshalByRefObject, IClientService {
        public void Acknowledge(ArrayList tuple) {
            throw new NotImplementedException();
        }

        public void ReadResponse(ArrayList tuple) {
            throw new NotImplementedException();
        }

        public void TakeResponse(ArrayList tuple) {
            throw new NotImplementedException();
        }
    }
}
