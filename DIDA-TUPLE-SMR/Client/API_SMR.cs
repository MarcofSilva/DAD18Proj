using RemoteServicesLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClassLibrary;
using ExceptionLibrary;

namespace Client {
    class API_SMR : TupleSpaceAPI {

        private const int defaultPort = 8085;
        private TcpChannel channel;
        private List<IServerService> _view;
        private bool frozen = false;
        private string url;

        public API_SMR(string URL) {
            _view = prepareForRemoting(ref channel, URL);
            _view = getView(_view);
            url = URL;
        }

        public override void write(TupleClass tuple) {
            checkFrozen();
            _view = getView(_view);
            try {
                _view[0].write(tuple,url, nonce);
                nonce++;
            }
            catch (ElectionException) {
                Thread.Sleep(500);
                write(tuple);
            }
            catch (SocketException) {
                write(tuple);
            }
        }
        public override TupleClass read(TupleClass tuple) {
            checkFrozen();
            _view = getView(_view);
            try {
                TupleClass res = _view[0].read(tuple, url, nonce);
                nonce++;
                if(res.tuple.Count == 0) {
                    Thread.Sleep(500);
                    return read(tuple);
                }
                return res;
            }
            catch (ElectionException) {
                Thread.Sleep(500);
                return read(tuple);
            }
            catch (SocketException) {
                return read(tuple);
            }
        }

        public override TupleClass take(TupleClass tuple) {
            checkFrozen();
            _view = getView(_view);
            try {
                TupleClass res = _view[0].take(tuple, url, nonce);
                nonce++;
                if (res.tuple.Count == 0) {
                    Thread.Sleep(500);
                    return take(tuple);
                }
                return res;
            }
            catch (ElectionException) {
                Thread.Sleep(500);
                return take(tuple);
            }
            catch (SocketException) {
                return take(tuple);
            }
        }

        public override void freeze() {
            frozen = true;
        }

        public override void unfreeze() {
            Console.WriteLine("Unfreezing...");
            lock (this) {
                Monitor.PulseAll(this);
            }
            frozen = false;
        }

        public void checkFrozen() {
            if (frozen) {
                Console.WriteLine("Cant do anything, im frozen");
                lock (this) {
                    while (frozen) {
                        Console.WriteLine("Waiting...");
                        Monitor.Wait(this);
                    }
                }
            }
        }
    }
}
