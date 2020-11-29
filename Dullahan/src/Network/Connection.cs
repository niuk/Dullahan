using Org.BouncyCastle.Crypto.Tls;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;

namespace Dullahan.Network {
    public class Connection : IDisposable {
        private const int HEADER_SIZE = 4;

        private class Blob {
            private readonly ArrayPool<byte> arrayPool;

            public bool Complete => hasLeftEnd && hasRightEnd;
            private bool hasLeftEnd;
            private bool hasRightEnd;
             
            public byte[] buffer { get; private set; }
            public int start { get; private set; }
            public int size { get; private set; }

            public Blob(ArrayPool<byte> arrayPool, bool isLeftEnd, bool isRightEnd, byte[] buffer, int start, int size) {
                this.arrayPool = arrayPool;

                hasLeftEnd = isLeftEnd;
                hasRightEnd = isRightEnd;

                this.buffer = arrayPool.Rent(1024);
                Array.Copy(buffer, start, this.buffer, 0, size);
                this.start = 0;
                this.size = size;
            }

            public void AppendLeft(byte[] buffer, int start, int size, bool isLeftEnd) {
                if (hasLeftEnd && isLeftEnd) {
                    throw new InvalidOperationException("This blob already has a left end.");
                }

                if (this.buffer.Length < this.size + size) {
                    Expand();
                }

                this.size += size;
                this.start -= size;
                if (this.start < 0) {
                    this.start = this.buffer.Length + (this.start % this.buffer.Length);
                }

                int count = Math.Min(this.buffer.Length - this.start, size);
                Array.Copy(buffer, start, this.buffer, this.start, count);
                Array.Copy(buffer, start + count, this.buffer, (this.start + count) % this.buffer.Length, size - count);

                hasLeftEnd = isLeftEnd;
            }

            public void AppendRight(byte[] buffer, int start, int size, bool isRightEnd) {
                if (hasRightEnd && isRightEnd) {
                    throw new InvalidOperationException("This blob already has a right end.");
                }

                if (this.buffer.Length < this.size + size) {
                    Expand();
                }

                int end = (this.start + this.size) % this.buffer.Length;
                this.size += size;
                int count = Math.Min(this.buffer.Length - end, size);
                Array.Copy(buffer, start, this.buffer, end, count);
                Array.Copy(buffer, start + count, this.buffer, (end + count) % this.buffer.Length, size - count);

                hasRightEnd = isRightEnd;
            }

            private void Expand() {
                var newBuffer = arrayPool.Rent(buffer.Length * 2);
                int count = Math.Min(buffer.Length - start, size);
                Array.Copy(buffer, start, newBuffer, 0, count);
                start = (start + count) % buffer.Length;
                size -= count;
                Array.Copy(buffer, start, newBuffer, count, size);
                arrayPool.Return(buffer);
                buffer = newBuffer;
                start = 0;
                size += count;
            }
        }

        private readonly Dictionary<int, Blob> blobsByLeftestNumber = new Dictionary<int, Blob>();
        private readonly Dictionary<int, Blob> blobsByRightestNumber = new Dictionary<int, Blob>();
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly Thread thread;
        private readonly ArrayPool<byte> arrayPool = ArrayPool<byte>.Create();
        private DatagramTransportImplementation datagramTransport;
        private DtlsTransport dtlsTransport;
        private bool disposedValue = false;
        private uint nextSequenceNumber = 0;

        public bool Connected => dtlsTransport != null;

        public Connection(
            Func<DatagramTransportImplementation> getDatagramTransport,
            Func<DatagramTransport, DtlsTransport> getDtlsTransport,
            Action<byte[], int, int> onMessageReceived,
            CancellationToken cancellationToken
        ) {
            thread = new Thread(() => {
                using (var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancellationTokenSource.Token)) {
                    datagramTransport = getDatagramTransport();
                    try {
                        while (!linkedCancellationTokenSource.IsCancellationRequested) {
                            try {
                                dtlsTransport = getDtlsTransport(datagramTransport);
                                Console.WriteLine($"Connected to {datagramTransport.RemoteEndPoint}");
                                break;
                            } catch (Exception e) {
                                // ignore
                                Console.WriteLine(e);
                            }
                        }

                        try {
                            var buffer = new byte[dtlsTransport.GetReceiveLimit()];
                            while (!linkedCancellationTokenSource.IsCancellationRequested) {
                                int length = dtlsTransport.Receive(buffer, 0, buffer.Length, 1000);
                                Console.WriteLine($"Received {length} bytes from {datagramTransport.RemoteEndPoint} via DTLS");
                                if (length > 0) {
                                    int header = buffer[0] | buffer[1] << 8 | (buffer[2] << 16) | (buffer[3] << 24);
                                    bool isLeftEnd = (header & 0x80000000) != 0;
                                    bool isRightEnd = (header & 0x40000000) != 0;
                                    int size = (header & 0x3ff00000) >> 20;
                                    int number = header & 0x000fffff;

                                    if (!isRightEnd && blobsByLeftestNumber.TryGetValue(number + 1, out Blob blob)) {
                                        blobsByLeftestNumber.Remove(number + 1);
                                        blob.AppendLeft(buffer, HEADER_SIZE, size, isLeftEnd);
                                        if (blob.Complete) {
                                            onMessageReceived(blob.buffer, blob.start, blob.size);
                                        } else {
                                            blobsByLeftestNumber.Add(number, blob);
                                        }
                                    } else if (!isLeftEnd && blobsByRightestNumber.TryGetValue(number - 1, out blob)) {
                                        blobsByRightestNumber.Remove(number - 1);
                                        blob.AppendRight(buffer, HEADER_SIZE, size, isRightEnd);
                                        if (blob.Complete) {
                                            onMessageReceived(blob.buffer, blob.start, blob.size);
                                        } else {
                                            blobsByRightestNumber.Add(number, blob);
                                        }
                                    } else {
                                        blob = new Blob(arrayPool, isLeftEnd, isRightEnd, buffer, HEADER_SIZE, size);
                                        if (blob.Complete) {
                                            onMessageReceived(blob.buffer, blob.start, blob.size);
                                        } else {
                                            blobsByLeftestNumber.Add(number, blob);
                                            blobsByRightestNumber.Add(number, blob);
                                        }
                                    }
                                }
                            }
                        } finally {
                            dtlsTransport.Close();
                        }
                    } finally {
                        datagramTransport.Close();
                    }
                }
            });
            thread.Start();
        }

        public void Send(byte[] buffer, int offset, int length) {
            int fragmentSizeLimit = dtlsTransport.GetSendLimit() - HEADER_SIZE;
            int fragmentCount = length / fragmentSizeLimit + (length % fragmentSizeLimit > 0 ? 1 : 0);
            for (int i = 0; i < fragmentCount; ++i) {
                bool isLeftEnd = i == 0;
                uint header = isLeftEnd ? 0x80000000 : 0;

                bool isRightEnd = i == fragmentCount - 1;
                header |= isRightEnd ? (uint)0x40000000 : 0;

                int fragmentSize = isRightEnd ? length % fragmentSizeLimit : fragmentSizeLimit;
                if (fragmentSize > fragmentSizeLimit) { throw new InvalidOperationException($"Size of fragment ({fragmentSize}) must fit 10 bits (maximum of {fragmentSizeLimit})."); }
                header |= (uint)(fragmentSize << 20 & 0x3ff00000);

                uint sequenceNumber = nextSequenceNumber;
                nextSequenceNumber = (nextSequenceNumber + 1) % 0x000fffff;
                header |= sequenceNumber;

                var dtlsBuffer = arrayPool.Rent(dtlsTransport.GetSendLimit());
                dtlsBuffer[0] = (byte)(header & 0xff);
                dtlsBuffer[1] = (byte)((header >> 8) & 0xff);
                dtlsBuffer[2] = (byte)((header >> 16) & 0xff);
                dtlsBuffer[3] = (byte)((header >> 24) & 0xff);
                Array.Copy(buffer, offset, dtlsBuffer, HEADER_SIZE, fragmentSize);
                dtlsTransport.Send(dtlsBuffer, 0, HEADER_SIZE + fragmentSize);
                Console.WriteLine($"Sent {HEADER_SIZE + fragmentSize} bytes to {datagramTransport.RemoteEndPoint} via DTLS");
            }
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    cancellationTokenSource.Cancel();
                    try {
                        thread.Join();
                    } finally {
                        cancellationTokenSource.Dispose();
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
