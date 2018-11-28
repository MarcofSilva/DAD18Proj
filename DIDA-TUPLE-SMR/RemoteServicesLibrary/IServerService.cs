﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassLibrary

namespace RemoteServicesLibrary
{
    public interface IServerService{

        List<TupleClass> Read(TupleClass tuple, string clientUrl, long nonce);

        List<TupleClass> Take(TupleClass tuple, string clientUrl, long nonce);

        void Write(TupleClass tuple, string clientUrl, long nonce);
    }
}
