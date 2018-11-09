using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RemoteServicesLibrary;


namespace Client {
    public class ClientService : MarshalByRefObject, IClientService {
        public void WriteResponse(ArrayList ack) {
            Console.WriteLine("sucessfull write " + ack[0]);
        }

        public void ReadResponse(ArrayList ack, List<ArrayList> tuple) {
            Console.WriteLine("sucessfull read " + ack[0]);
            if (tuple.Count ==0) {
                Console.WriteLine("devolveu vazio");
            }
            else {
                Console.WriteLine("tuple " + tuple[0][0].ToString());
            }

        }

        public void TakeResponse(ArrayList ack, List<ArrayList> tuple) {
            Console.WriteLine("sucessfull take " + ack[0]);
            Console.WriteLine("tuple " + tuple[0][0].ToString());
        }
    }
}
