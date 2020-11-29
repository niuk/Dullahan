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

        private readonly BlockingCollection<(byte[], int)> datagrams = new BlockingCollection<(byte[], int)>();
        private readonly ArrayPool<byte> arrayPool = ArrayPool<byte>.Create();
        private readonly object mutex = new object();
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly Thread thread;
        private readonly EndPoint localEndPoint;
        private EndPoint remoteEndPoint;
        private Socket socket;

        public DatagramTransportImplementation(EndPoint localEndPoint, EndPoint remoteEndPoint) {
            this.localEndPoint = localEndPoint;
            this.remoteEndPoint = remoteEndPoint;

            OpenSocket();

            thread = new Thread(() => {
                using (var semaphore = new SemaphoreSlim(1)) {
                    while (!cancellationTokenSource.IsCancellationRequested) {
                        semaphore.Wait(cancellationTokenSource.Token); // wait until previous receive is done

                        int limit = GetReceiveLimit();
                        var buffer = arrayPool.Rent(limit);
                        try {
                            socket.BeginReceiveFrom(buffer, 0, limit, SocketFlags.None, ref this.remoteEndPoint, new AsyncCallback(result => {
                                try {
                                    datagrams.Add((buffer, socket.EndReceiveFrom(result, ref this.remoteEndPoint)), cancellationTokenSource.Token);
                                } catch (ObjectDisposedException e) {
                                    if (e.ObjectName == typeof(Socket).FullName) {
                                        OpenSocket();
                                    }
                                }

                                semaphore.Release(); // start another receive
                            }), null);
                        } catch (ObjectDisposedException e) {
                            if (e.ObjectName == typeof(Socket).FullName) {
                                OpenSocket();
                            }
                        }
                    }
                }
            });
            thread.Start();
        }

        public void Close() {
            cancellationTokenSource.Cancel();
            socket.Close();
            thread.Join();
        }

        public int GetReceiveLimit() {
            return 1024;
        }

        public int GetSendLimit() {
            return 1024;
        }

        public int Receive(byte[] buf, int off, int len, int waitMillis) {
            if (datagrams.TryTake(out (byte[], int) datagram, waitMillis)) {
                int length = Math.Min(len - off, datagram.Item2);
                Array.Copy(datagram.Item1, 0, buf, off, length);
                arrayPool.Return(datagram.Item1);
                return length;
            } else {
                return 0;
            }
        }

        public void Send(byte[] buf, int off, int len) {
            int bytesSent = 0;
            do {
                try {
                    bytesSent += socket.SendTo(buf, off + bytesSent, len - bytesSent, SocketFlags.None, remoteEndPoint);
                } catch (ObjectDisposedException e) {
                    if (e.ObjectName == typeof(Socket).FullName) {
                        OpenSocket();
                    }
                }
            } while (bytesSent < len);
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
