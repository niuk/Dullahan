using Dullahan;
using Dullahan.Network;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace TestClient {
    class Program {
        static void Main(string[] args) {
            var client = new Client<int, int, int, int>(0, new PrimitiveDiffer<int>(), new PrimitiveDiffer<int>(), new IPEndPoint(IPAddress.Loopback, 9000));
            for (int i = 0; ; ++i) {
                if (client.Connected) {
                    string s = "But I must explain to you how all this mistaken idea of denouncing of a pleasure and praising pain was born and I will give you a complete account of the system, and expound the actual teachings of the great explorer of the truth, the master-builder of human happiness. No one rejects, dislikes, or avoids pleasure itself, because it is pleasure, but because those who do not know how to pursue pleasure rationally encounter consequences that are extremely painful. Nor again is there anyone who loves or pursues or desires to obtain pain of itself, because it is pain, but occasionally circumstances occur in which toil and pain can procure him some great pleasure. To take a trivial example, which of us ever undertakes laborious physical exercise, except to obtain some advantage from it? But who has any right to find fault with a man who chooses to enjoy a pleasure that has no annoying consequences, or one who avoids a pain that produces no resultant pleasure?";/*
                    using (var stream = WebRequest.Create("http://www.randomtext.me/api/").GetResponse().GetResponseStream())
                    using (var textReader = new StreamReader(stream))
                    using (var reader = new JsonTextReader(textReader)) {
                        s = (string)JToken.ReadFrom(reader)["text_out"];
                    }*/

                    var message = $"{i}: {s}";
                    var buffer = Encoding.ASCII.GetBytes(message);
                    client.Send(buffer, 0, buffer.Length);
                    Console.WriteLine($"Sent message: \"{message}\"");
                }

                Thread.Sleep(1000);
            }
        }
    }
}
