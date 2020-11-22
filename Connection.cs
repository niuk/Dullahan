﻿using System;

namespace Dullahan {
    partial class Server {
        public class Connection {
            public readonly Guid id = Guid.NewGuid();
            public readonly Server server;

            public Connection(Server server) {
                this.server = server;
                server.connectionsById.Add(id, this);
            }

            public void PropagateState() {
            }
        }
    }
}