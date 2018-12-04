using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ExceptionLibrary{
    [Serializable]
    public class ElectionException : ApplicationException {

        public ElectionException() {
        }

        public ElectionException(string message): base(message) {
        }

        public ElectionException(string message, Exception inner): base(message, inner) {
        }

        public ElectionException(SerializationInfo info, StreamingContext context): base(info, context) {

        }

    }
}
