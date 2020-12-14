using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Security;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;

namespace Dullahan.Network {
    public class Connection : IDisposable {
        private struct Blob {
            public bool hasLeftEnd;
            public bool hasRightEnd;
            public int leftNumber;
            public int rightNumber;
            public bool Complete => hasLeftEnd && hasRightEnd;

            public Blob(bool hasLeftEnd, bool hasRightEnd, int leftNumber, int rightNumber) {
                this.hasLeftEnd = hasLeftEnd;
                this.hasRightEnd = hasRightEnd;
                this.leftNumber = leftNumber;
                this.rightNumber = rightNumber;
            }
        }

        private const int anchorRange = 128;

        private readonly Dictionary<int, (byte[], int)> fragmentsByNumber = new Dictionary<int, (byte[], int)>();
        private readonly Dictionary<int, Blob> blobsByLeftNumber = new Dictionary<int, Blob>();
        private readonly Dictionary<int, Blob> blobsByRightNumber = new Dictionary<int, Blob>();
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly Thread receiveThread;
        private DtlsTransport dtlsTransport;
        private bool disposedValue = false;
        private uint nextNumber = 0;
        private int? anchorNumber = null;

        public bool Connected => dtlsTransport != null;

        public Connection(
            EndPoint localEndPoint,
            EndPoint remoteEndPoint,
            Action<byte[], int, int> onMessageReceived
        ) {
            receiveThread = new Thread(() => {
                var datagramTransport = new DatagramTransportImplementation(localEndPoint, remoteEndPoint);
                try {
                    while (!cancellationTokenSource.IsCancellationRequested) {
                        try {
                            if (remoteEndPoint.Equals(new IPEndPoint(IPAddress.Any, 0))) {
                                dtlsTransport = new DtlsServerProtocol(new SecureRandom()).Accept(new TlsServerImplementation(), datagramTransport);
                            } else {
                                dtlsTransport = new DtlsClientProtocol(new SecureRandom()).Connect(new TlsClientImplementation(), datagramTransport);
                            }

                            break;
                        } catch (Exception e) {
                            // ignore
                            Console.WriteLine(e);
                        }
                    }

                    var buffer = ArrayPool<byte>.Shared.Rent(dtlsTransport.GetReceiveLimit());
                    try {
                        while (!cancellationTokenSource.IsCancellationRequested) {
                            //Console.WriteLine($"fragmentsByNumber.Count = {fragmentsByNumber.Count}, blobsByNumber.Count = {blobsByLeftNumber.Count}");

                            int length = dtlsTransport.Receive(buffer, 0, buffer.Length, 1000);
                            if (length <= 0) {
                                continue;
                            }

                            //Console.WriteLine($"\tReceived {length} bytes from {datagramTransport.RemoteEndPoint} via DTLS: {BitConverter.ToString(buffer, 0, length)}");
                            // always little-endian, whereas BitConverter.ToInt32 depends on the endianness of the machine
                            int header = new BinaryReader(new MemoryStream(buffer, 0, buffer.Length)).ReadInt32(); 
                            bool isLeftEnd = (header & 0x80000000) != 0;
                            bool isRightEnd = (header & 0x40000000) != 0;
                            int size = (header & 0x3ff00000) >> 20;
                            int number = header & 0xfffff;
                            //Console.WriteLine($"Received fragment {number}: isLeftEnd = {isLeftEnd}, isRightEnd = {isRightEnd}");

                            if (anchorNumber.HasValue) {
                                int minimum = (anchorNumber.Value - anchorRange) & 0xfffff;
                                int maximum = (anchorNumber.Value + anchorRange) & 0xfffff;
                                //Console.WriteLine($"anchorNumber.Value = {anchorNumber.Value}, minimum = {minimum}, number = {number}, maximum = {maximum}");

                                if (minimum < maximum ?
                                    number < minimum || maximum < number :
                                    number < minimum && maximum < number
                                ) {
                                    // received number is out of range
                                    continue;
                                }

                                if (anchorNumber.Value < maximum ?
                                    anchorNumber.Value < number && number < maximum :
                                    anchorNumber.Value < number || number < maximum
                                ) {
                                    // received number is within range and greater than the anchor number
                                    anchorNumber = number;
                                    //Console.WriteLine($"Incremented anchor number to {number}; minimum = {minimum}");

                                    // recycle old fragment buffers
                                    int newMinimum = (anchorNumber.Value - anchorRange) & 0xfffff;
                                    for (int i = minimum; i != newMinimum; i = (i + 1) & 0xfffff) {
                                        if (fragmentsByNumber.TryGetValue(i, out (byte[], int) fragment)) {
                                            fragmentsByNumber.Remove(i);
                                            ArrayPool<byte>.Shared.Return(fragment.Item1);
                                            //Console.WriteLine($"Recycled fragment buffer {i}");
                                        }

                                        if (blobsByLeftNumber.TryGetValue(i, out Blob blob)) {
                                            blobsByLeftNumber.Remove(i);
                                            Debug.Assert(blobsByRightNumber.Remove(blob.rightNumber));
                                            //Console.WriteLine($"Recycled blob ({i}, {blob.rightFragmentNumber})");
                                        }
                                    }
                                }
                            } else {
                                // first number received
                                anchorNumber = number;
                            }

                            if (fragmentsByNumber.ContainsKey(number)) {
                                // duplicate fragment
                                continue;
                            }

                            var fragmentBuffer = ArrayPool<byte>.Shared.Rent(size);
                            Array.Copy(buffer, 4, fragmentBuffer, 0, size);
                            fragmentsByNumber.Add(number, (fragmentBuffer, size));
                            //Console.WriteLine($"Added fragment {number}, size = {size}: \"{Encoding.ASCII.GetString(fragmentBuffer, 0, size)}\"");

                            bool mergeLeft;
                            int rightNumberOfLeftNeighbor = number == 0 ? 0xfffff : number - 1;
                            if (blobsByRightNumber.TryGetValue(rightNumberOfLeftNeighbor, out Blob leftNeighbor)) {
                                if (leftNeighbor.hasRightEnd) {
                                    if (isLeftEnd) {
                                        mergeLeft = false;
                                    } else {
                                        throw new InvalidOperationException($"{number} is not a left end but {rightNumberOfLeftNeighbor} has right end");
                                    }
                                } else {
                                    if (isLeftEnd) {
                                        throw new InvalidOperationException($"{number} is a left end but {rightNumberOfLeftNeighbor} has no right end");
                                    } else {
                                        mergeLeft = true;
                                    }
                                }
                            } else {
                                mergeLeft = false;
                            }

                            if (mergeLeft) {
                                blobsByRightNumber.Remove(rightNumberOfLeftNeighbor);
                                blobsByLeftNumber.Remove(leftNeighbor.leftNumber);
                            }

                            bool mergeRight;
                            int leftNumberOfRightNeighbor = (number + 1) & 0xfffff;
                            if (blobsByLeftNumber.TryGetValue(leftNumberOfRightNeighbor, out Blob rightNeighbor)) {
                                if (rightNeighbor.hasLeftEnd) {
                                    if (isRightEnd) {
                                        mergeRight = false;
                                    } else {
                                        throw new InvalidOperationException($"{number} is not a right end but {leftNumberOfRightNeighbor} has left end");
                                    }
                                } else {
                                    if (isRightEnd) {
                                        throw new InvalidOperationException($"{number} is a right end but {leftNumberOfRightNeighbor} has no left end");
                                    } else {
                                        mergeRight = true;
                                    }
                                }
                            } else {
                                mergeRight = false;
                            }

                            if (mergeRight) {
                                blobsByLeftNumber.Remove(leftNumberOfRightNeighbor);
                                blobsByRightNumber.Remove(rightNeighbor.rightNumber);
                            }

                            Blob newBlob;
                            if (mergeLeft && mergeRight) {
                                newBlob = new Blob(
                                    leftNeighbor.hasLeftEnd,
                                    rightNeighbor.hasRightEnd,
                                    leftNeighbor.leftNumber,
                                    rightNeighbor.rightNumber);
                            } else if (mergeLeft) {
                                newBlob = new Blob(
                                    leftNeighbor.hasLeftEnd,
                                    isRightEnd,
                                    leftNeighbor.leftNumber,
                                    number);
                            } else if (mergeRight) {
                                newBlob = new Blob(
                                    isLeftEnd,
                                    rightNeighbor.hasRightEnd,
                                    number,
                                    rightNeighbor.rightNumber);
                            } else {
                                newBlob = new Blob(
                                    isLeftEnd,
                                    isRightEnd,
                                    number,
                                    number);
                            }

                            if (newBlob.Complete) {
                                int messageSize = 0;
                                for (int i = newBlob.leftNumber; i != ((newBlob.rightNumber + 1) & 0xfffff); i = (i + 1) & 0xfffff) {
                                    messageSize += fragmentsByNumber[i].Item2;
                                }

                                var messageBuffer = ArrayPool<byte>.Shared.Rent(messageSize);
                                try {
                                    int messageIndex = 0;
                                    for (int i = newBlob.leftNumber; i != ((newBlob.rightNumber + 1) & 0xfffff); i = (i + 1) & 0xfffff) {
                                        Array.Copy(fragmentsByNumber[i].Item1, 0, messageBuffer, messageIndex, fragmentsByNumber[i].Item2);
                                        messageIndex += fragmentsByNumber[i].Item2;
                                    }

                                    onMessageReceived(messageBuffer, 0, messageSize);
                                } finally {
                                    ArrayPool<byte>.Shared.Return(messageBuffer);
                                }
                            } else {
                                blobsByLeftNumber.Add(newBlob.leftNumber, newBlob);
                                blobsByRightNumber.Add(newBlob.rightNumber, newBlob);
                            }
                        }
                    } finally {
                        dtlsTransport.Close();
                        ArrayPool<byte>.Shared.Return(buffer);
                    }
                } finally {
                    datagramTransport.Close();
                }
            });
            receiveThread.Start();
        }

        public void SendMessage(byte[] buffer, int index, int size) {
            var dtlsBuffer = ArrayPool<byte>.Shared.Rent(dtlsTransport.GetSendLimit());
            try {
                int fragmentSizeLimit = dtlsTransport.GetSendLimit() - 4;
                Debug.Assert(fragmentSizeLimit <= 0x3ff);
                int fragmentCount = size / fragmentSizeLimit + (size % fragmentSizeLimit > 0 ? 1 : 0);
                for (int i = 0; i < fragmentCount; ++i) {
                    bool isLeftEnd = i == 0;
                    uint header = isLeftEnd ? 0x80000000 : 0;

                    bool isRightEnd = i == fragmentCount - 1;
                    header |= isRightEnd ? (uint)0x40000000 : 0;

                    int fragmentSize = isRightEnd ? size % fragmentSizeLimit : fragmentSizeLimit;
                    if (fragmentSize > fragmentSizeLimit) { throw new InvalidOperationException($"Size of fragment ({fragmentSize}) must fit 10 bits (maximum of {fragmentSizeLimit})."); }
                    header |= (uint)(fragmentSize << 20 & 0x3ff00000);

                    uint number = nextNumber;
                    nextNumber = (nextNumber + 1) % 0x000fffff;
                    header |= number;

                    // always little-endian, whereas BitConverter.GetBytes depends on the endianness of the machine
                    new BinaryWriter(new MemoryStream(dtlsBuffer, 0, dtlsBuffer.Length)).Write((int)header);
                    Array.Copy(buffer, index, dtlsBuffer, 4, fragmentSize);
                    dtlsTransport.Send(dtlsBuffer, 0, 4 + fragmentSize);
                    //Console.WriteLine($"\tSent {HEADER_SIZE + fragmentSize} bytes to {datagramTransport.RemoteEndPoint} via DTLS: {BitConverter.ToString(dtlsBuffer, 0, HEADER_SIZE + fragmentSize)}");
                }
            } finally {
                ArrayPool<byte>.Shared.Return(dtlsBuffer);
            }
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    cancellationTokenSource.Cancel();
                    try {
                        receiveThread.Join();
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
