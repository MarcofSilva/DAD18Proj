using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientLibrary
{
    interface ITupleSpaceAPI
    {
        void Write(ArrayList Tuple);

        void Read(ArrayList Tuple);

        void Take(ArrayList Tuple);
    }
}
