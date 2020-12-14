using System;
using System.Collections.Generic;
using System.Net;

namespace Dullahan.Network {
    public class Server<TServerState, TClientState> : IDisposable {
        public int serverTick {
            set {
                foreach (var client in clientsByPort.Values) {
                    client.LocalTick = value;
                }
            }
        }

        public bool GetClientConnected(int port) {
            return clientsByPort[port].Connected;
        }

        public TClientState GetClientState(int port) {
            int tick = clientsByPort[port].AckingRemoteTick;
            if (tick >= 0) {
                clientsByPort[port].RemoteStatesByTick.TryGetValue(tick, out TClientState clientState);
                return clientState;
            } else {
                return default;
            }
        }

        public int GetClientTick(int port) {
            return clientsByPort[port].AckingRemoteTick;
        }

        public long GetTotalBytesSent(int port) {
            return clientsByPort[port].totalBytesSent;
        }

        public long GetTotalBytesReceived(int port) {
            return clientsByPort[port].totalBytesReceived;
        }

        private readonly Dictionary<int, Client<TServerState, TClientState>> clientsByPort = new Dictionary<int, Client<TServerState, TClientState>>();
        private bool disposedValue;

        public Server(
            TClientState initialClientState,
            IReadOnlyDictionary<int, TServerState> serverStatesByTick,
            IDiffer<TServerState> serverStateDiffer,
            IDiffer<TClientState> clientStateDiffer,
            int portStart,
            int capacity,
            TimeSpan sendInterval
        ) {
            for (int port = portStart; port < portStart + capacity; ++port) {
                clientsByPort.Add(port, new Client<TServerState, TClientState>(
                    initialClientState,
                    serverStatesByTick,
                    serverStateDiffer,
                    clientStateDiffer,
                    new IPEndPoint(IPAddress.Any, port),
                    new IPEndPoint(IPAddress.Any, 0),
                    sendInterval));
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