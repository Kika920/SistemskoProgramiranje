
using System.Threading;

namespace PrviProjekat
{
    public class Program
    {
        static void Main(string[] args)
        {
            var service = new RijksServis();
            var server = new Server("http://localhost:8080/",service);

            Thread t = new Thread(server.Start);
            t.Start();

            System.Console.ReadLine();

            server.Stop();
            t.Join();
        }
    }
}