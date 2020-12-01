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
        private readonly ArrayPool<byte> arrayPool = ArrayPool<byte>.Create();
        private readonly object mutex = new object();
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
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
                using (var semaphore = new SemaphoreSlim(1)) {
                    while (!cancellationTokenSource.IsCancellationRequested) {
                        semaphore.Wait(cancellationTokenSource.Token); // wait until previous send is done
                        var (buffer, size) = outgoing.Take(cancellationTokenSource.Token);
                        try {
                            socket.BeginSendTo(buffer, 0, size, SocketFlags.None, this.remoteEndPoint, new AsyncCallback(result => {
                                try {
                                    int sentBytes = socket.EndSendTo(result);
                                    //Console.WriteLine($"\t\tSent {sentBytes} to {this.remoteEndPoint} via UDP: {BitConverter.ToString(buffer, 0, sentBytes)}");
                                    arrayPool.Return(buffer);
                                } catch (ObjectDisposedException e) when (e.ObjectName == typeof(Socket).FullName) {
                                    OpenSocket();
                                    outgoing.Add((buffer, size));
                                } finally {
                                    semaphore.Release(); // start another send
                                }
                            }), null);
                        } catch (ObjectDisposedException e) when (e.ObjectName == typeof(Socket).FullName) {
                            OpenSocket();
                            outgoing.Add((buffer, size));
                        }
                    }
                }
            });
            sendingThread.Start();

            receivingThread = new Thread(() => {
                using (var semaphore = new SemaphoreSlim(1)) {
                    while (!cancellationTokenSource.IsCancellationRequested) {
                        semaphore.Wait(cancellationTokenSource.Token); // wait until previous receive is done
                        int limit = GetReceiveLimit();
                        var buffer = arrayPool.Rent(limit);
                        try {
                            socket.BeginReceiveFrom(buffer, 0, limit, SocketFlags.None, ref this.remoteEndPoint, new AsyncCallback(result => {
                                try {
                                    int receivedBytes = socket.EndReceiveFrom(result, ref this.remoteEndPoint);
                                    //Console.WriteLine($"\t\tReceived {receivedBytes} from {this.remoteEndPoint} via UDP: {BitConverter.ToString(buffer, 0, receivedBytes)}");
                                    incoming.Add((buffer, receivedBytes), cancellationTokenSource.Token);
                                } catch (ObjectDisposedException e) when (e.ObjectName == typeof(Socket).FullName) {
                                    OpenSocket();
                                } finally {
                                    semaphore.Release(); // start another receive
                                }
                            }), null);
                        } catch (ObjectDisposedException e) when (e.ObjectName == typeof(Socket).FullName) {
                            OpenSocket();
                        }
                    }
                }
            });
            receivingThread.Start();
        }

        public void Close() {
            cancellationTokenSource.Cancel();
            socket.Close();
            sendingThread.Join();
            receivingThread.Join();
        }

        public int GetReceiveLimit() {
            return 1024;
        }

        public int GetSendLimit() {
            return 1024;
        }

        public int Receive(byte[] buf, int off, int len, int waitMillis) {
            // must use TryTake instead of Take because timeouts must return 0 instead of throwing exceptions
            if (incoming.TryTake(out (byte[], int) datagram, waitMillis)) {
                int size = Math.Min(len - off, datagram.Item2);
                Array.Copy(datagram.Item1, 0, buf, off, size);
                arrayPool.Return(datagram.Item1);
                return size;
            } else {
                return -1;
            }
        }

        public void Send(byte[] buf, int off, int len) {
            if (len > GetSendLimit()) {
                Console.WriteLine($"Discarded {len} bytes when sending via UDP.");
                return;
            }

            var buffer = arrayPool.Rent(len);
            Array.Copy(buf, off, buffer, 0, len);
            outgoing.Add((buffer, len), cancellationTokenSource.Token);
        }

        private void OpenSocket() {
            lock (mutex) {
                socket?.Close();
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.Bind(localEndPoint);
            }
        }
    }
}
