﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassLibrary;

namespace RemoteServicesLibrary
{
    public interface IServerService
    {
        void Write(TupleClass tuple, string clientUrl, long nonce);

        TupleClass Read(TupleClass tuple, string clientUrl, long nonce);

        List<TupleClass> TakeRead(TupleClass tuple, string clientUrl);

        void TakeRemove(TupleClass tuple, string clientUrl, long nonce);
    }
}
