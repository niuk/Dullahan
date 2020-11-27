using System;
using System.Net;
using System.Net.Sockets;

using Org.BouncyCastle.Crypto.Tls;

namespace Dullahan.Network {
    public class DatagramTransportImplementation : DatagramTransport {
        private readonly object mutex = new object();
        private readonly Socket socket;
        private readonly byte[] buffer; // a circular buffer
        private int length;
        private IAsyncResult asyncResult;

        // this property will hold sender's endpoint upon receiving data
        public EndPoint RemoteEndPoint => remoteEndPoint;
        private EndPoint remoteEndPoint;

        public DatagramTransportImplementation(EndPoint localEndPoint, EndPoint remoteEndPoint) {
            this.remoteEndPoint = remoteEndPoint;
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(localEndPoint);
            buffer = new byte[GetReceiveLimit()];
            length = 0;
            asyncResult = BeginReceiveFrom();
        }

        public void Close() {
            socket.Close();
        }

        public int GetReceiveLimit() {
            return 1024;
        }

        public int GetSendLimit() {
            return 1024;
        }

        private IAsyncResult BeginReceiveFrom() {
            return socket.BeginReceiveFrom(
                buffer,
                length,
                buffer.Length - length,
                SocketFlags.None,
                ref remoteEndPoint,
                new AsyncCallback(_result => {
                    lock (mutex) {
                        length += ((Socket)_result.AsyncState).EndReceiveFrom(_result, ref remoteEndPoint);
                    }
                }),
                socket);
        }

        public int Receive(byte[] buf, int off, int len, int waitMillis) {
            int bytesReceived = 0;
            void drain() {
                int count = Math.Min(length, len);
                Array.Copy(buffer, 0, buf, off, count);
                off += count;
                len -= count;
                length -= count;
                Array.Copy(buffer, count, buffer, 0, length);
                bytesReceived += count;
            }

            lock (mutex) {
                // make as much contiguous room in our buffer as we can
                drain();

                if (asyncResult.IsCompleted) {
                    // no asynchronous receive currently in progress; start one
                    asyncResult = BeginReceiveFrom();
                }
            }

            // wait for the current asynchronous receive to finish
            asyncResult.AsyncWaitHandle.WaitOne(waitMillis);

            drain();

            //Console.WriteLine($"Received {bytesReceived} bytes from {remoteEndPoint}.");
            return bytesReceived;
        }

        public void Send(byte[] buf, int off, int len) {
            int bytesSent = 0;
            do {
                bytesSent += socket.SendTo(buf, off + bytesSent, len - bytesSent, SocketFlags.None, remoteEndPoint);
                //Console.WriteLine($"Sent {bytesSent} bytes to {remoteEndPoint}.");
            } while (bytesSent < len);
        }
    }
}
