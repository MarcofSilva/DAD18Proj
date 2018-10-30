using RemoteServicesLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;

namespace ClientLibrary
{
    public class TupleSpaceAPI
    {
        public TupleSpaceAPI()
        {
            TcpChannel _channel = new TcpChannel(); //Port can't be 10000 (PCS) neither 10001 (Puppet Master)
            ChannelServices.RegisterChannel(_channel, false);

            IServerService _servRemoteObject = (IServerService)Activator.GetObject(typeof(IServerService), "tcp://" + serverIPAddress + ":8086/ServService");

            _nickname = txtBox_Nickname.Text;
            try
            {
                _servRemoteObject.register(_nickname, "tcp://" + myIPAddress + ":" + txtBox_Port.Text + "/" + myRemoteObjectName);
            }
            catch (SocketException)
            {
                MessageBox.Show("Could not locate server");
            }
        }

        

            
        void Write(ArrayList Tuple)
        {

        }

        void Read(ArrayList Tuple)
        {

        }

        void Take(ArrayList Tuple)
        {

        }
    }
}
