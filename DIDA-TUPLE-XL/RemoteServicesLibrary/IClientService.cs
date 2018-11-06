﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteServicesLibrary {
    public interface IClientService {
        //TODO check methods missing or to correct
        void WriteResponse(ArrayList ack);

        void ReadResponse(ArrayList ack, List<ArrayList> tuple);

        void TakeResponse(ArrayList ack, List<ArrayList> tuple);
    }
}
