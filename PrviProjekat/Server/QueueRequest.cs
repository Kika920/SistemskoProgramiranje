
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace PrviProjekat;

public class QueueRequest
{
    private readonly Queue<HttpListenerContext> red = new();
    private readonly object locker = new();

    private bool stopping = false;

    public void Stop()
    {
        lock (locker)
        {
            stopping = true;
            Monitor.PulseAll(locker); // budi sve koji čekaju
        }
    }

    public void Dodaj(HttpListenerContext ctx)
    {
        lock (locker)
        {
            if (stopping) return;

            red.Enqueue(ctx);
              Log.Info($"Dodaj u red zahteva: {ctx.Request.Url}");
            Monitor.Pulse(locker); 
        }
    }

    public HttpListenerContext? Get()
    {
        lock (locker)
        {
            while (red.Count == 0 && !stopping){ 
                Log.Info("Nema zahteva- CEKANJE");
                Monitor.Wait(locker);}
               

            if (stopping && red.Count == 0)
                return null;

              var ctx = red.Dequeue();

            Log.Info($"Poslat na obradu i uklonjen iz reda: {ctx.Request.Url}");

            return ctx;
        }
    }
}

