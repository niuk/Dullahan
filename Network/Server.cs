using System;
using System.Collections.Generic;

namespace Dullahan.Network {
    public class Server {
        public Dictionary<Guid, Connection> connectionsById = new Dictionary<Guid, Connection>();

        public void PropagateState() {
            foreach (var connection in connectionsById.Values) {
                connection.PropagateState();
            }
        }
    }
}