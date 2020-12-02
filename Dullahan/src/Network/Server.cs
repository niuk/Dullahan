using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace Dullahan.Network {
    public class Server<TServerState, TClientState> : IDisposable {
        public TServerState serverState {
            set {
                foreach (var client in clientsByPort.Values) {
                    client.localState = value;
                }
            }
        }

        public TClientState this[int port] => clientsByPort[port].remoteState;

        private readonly Dictionary<int, Client<TServerState, TClientState>> clientsByPort = new Dictionary<int, Client<TServerState, TClientState>>();
        private bool disposedValue;

        public Server(
            Func<BinaryReader, TServerState> readServerState,
            Action<TClientState, BinaryWriter> writeClientState,
            IDiffer<(BinaryWriter, TServerState), BinaryReader> serverStateDiffer,
            IDiffer<(BinaryWriter, TClientState), BinaryReader> clientStateDiffer,
            int portStart,
            int capacity,
            TimeSpan sendRate
        ) {
            for (int i = 0; i < capacity; ++i) {
                // can't use `i` directly because it gets overwritten during iteration
                int port = portStart + i;
                clientsByPort.Add(port, new Client<TServerState, TClientState>(
                    readServerState,
                    writeClientState,
                    serverStateDiffer,
                    clientStateDiffer,
                    new IPEndPoint(IPAddress.Any, port),
                    new IPEndPoint(IPAddress.Any, 0),
                    sendRate));
            }
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    foreach (var client in clientsByPort.Values) {
                        client.Dispose();
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}