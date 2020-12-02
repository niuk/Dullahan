using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Org.BouncyCastle.Crypto.Tls;

namespace Dullahan.Network {
    public class DatagramTransportImplementation : DatagramTransport {
        public EndPoint RemoteEndPoint => remoteEndPoint;

        private readonly BlockingCollection<(byte[], int)> incoming = new BlockingCollection<(byte[], int)>();
        private readonly BlockingCollection<(byte[], int)> outgoing = new BlockingCollection<(byte[], int)>();
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly SemaphoreSlim socketMutex = new SemaphoreSlim(1);
        private readonly Thread sendingThread;
        private readonly Thread receivingThread;
        private readonly EndPoint localEndPoint;
        private EndPoint remoteEndPoint;
        private Socket socket;

        public DatagramTransportImplementation(EndPoint localEndPoint, EndPoint remoteEndPoint) {
            this.localEndPoint = localEndPoint;
            this.remoteEndPoint = remoteEndPoint;

            OpenSocket();

            sendingThread = new Thread(() => {
                while (!cancellationTokenSource.IsCancellationRequested) {
                    var (buffer, size) = outgoing.Take(cancellationTokenSource.Token);
                    try {
                        // operation can only be canceled by closing the socket
                        // see https://stackoverflow.com/questions/4662553/how-to-abort-sockets-beginreceive
                        int sentBytes = socket.SendTo(buffer, 0, size, SocketFlags.None, this.remoteEndPoint);
                        //Console.WriteLine($"\t\tSent {sentBytes} to {this.remoteEndPoint} via UDP: {BitConverter.ToString(buffer, 0, sentBytes)}");
                    } catch (ObjectDisposedException e) when (e.ObjectName == typeof(Socket).FullName && !cancellationTokenSource.IsCancellationRequested) {
                        // reopen socket if socket was closed unintentionally
                        OpenSocket();

                        // try to send the same buffer again
                        outgoing.Add((buffer, size));
                        buffer = null;
                    } finally {
                        if (buffer != null) {
                            ArrayPool<byte>.Shared.Return(buffer);
                        }
                    }
                }
            });
            sendingThread.Start();

            receivingThread = new Thread(() => {
                while (!cancellationTokenSource.IsCancellationRequested) {
                    int limit = GetReceiveLimit();
                    var buffer = ArrayPool<byte>.Shared.Rent(limit);
                    try {
                        // operation can only be canceled by closing the socket
                        // see https://stackoverflow.com/questions/4662553/how-to-abort-sockets-beginreceive
                        int size = socket.ReceiveFrom(buffer, 0, limit, SocketFlags.None, ref this.remoteEndPoint);
                        //Console.WriteLine($"\t\tReceived {receivedBytes} from {this.remoteEndPoint} via UDP: {BitConverter.ToString(buffer, 0, receivedBytes)}");

                        incoming.Add((buffer, size), cancellationTokenSource.Token);
                        buffer = null;
                    } catch (ObjectDisposedException e) when (e.ObjectName == typeof(Socket).FullName && !cancellationTokenSource.IsCancellationRequested) {
                        // reopen socket if socket was closed unintentionally
                        OpenSocket();
                    } finally {
                        if (buffer != null) {
                            ArrayPool<byte>.Shared.Return(buffer);
                        }
                    }
                }
            });
            receivingThread.Start();
        }

        public void Close() {
            cancellationTokenSource.Cancel();
            try {
                // must close the socket first to interrupt threads waiting on SendTo or ReceiveFrom
                socket.Close();
                sendingThread.Join();
                receivingThread.Join();
            } finally {
                cancellationTokenSource.Dispose();
            }
        }

        public int GetReceiveLimit() {
            return 1024;
        }

        public int GetSendLimit() {
            return 1024;
        }

        public int Receive(byte[] buf, int off, int len, int waitMillis) {
            // must use TryTake instead of Take because timeouts must return -1 instead of throwing exceptions
            if (incoming.TryTake(out (byte[], int) datagram, waitMillis)) {
                try {
                    if (datagram.Item2 <= len) {
                        Array.Copy(datagram.Item1, 0, buf, off, datagram.Item2);
                        return datagram.Item2;
                    } else {
                        return -1;
                    }
                } finally {
                    ArrayPool<byte>.Shared.Return(datagram.Item1);
                }
            } else {
                return -1;
            }
        }

        public void Send(byte[] buf, int off, int len) {
            if (len > GetSendLimit()) {
                throw new InvalidOperationException($"length ({len}) exceeds send limit ({GetSendLimit()})");
            }

            var buffer = ArrayPool<byte>.Shared.Rent(len);
            try {
                Array.Copy(buf, off, buffer, 0, len);
                outgoing.Add((buffer, len), cancellationTokenSource.Token);
                buffer = null;
            } finally {
                if (buffer != null) {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }

        private void OpenSocket() {
            socketMutex.Wait(cancellationTokenSource.Token);
            try {
                socket?.Close();
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.Bind(localEndPoint);
            } finally {
                socketMutex.Release();
            }
        }
    }
}
