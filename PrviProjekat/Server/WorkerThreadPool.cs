using System.Threading;

namespace PrviProjekat;

public class WorkerThreadPool
{
    private readonly QueueRequest red;
    private readonly Server server;
    private readonly int workers;

    public WorkerThreadPool(QueueRequest red, Server server, int workers)
    {
        this.red = red;
        this.server = server;
        this.workers = workers;
    }

    public void Go()
    {
        for (int i = 0; i < workers; i++)
        {
            new Thread(Work)
            {
                IsBackground = true
            }.Start();
        }
    }

    private void Work()
    {
        while (true)
        {
            var ctx = red.Get();

            if (ctx == null)
                break;

            server.Obradi(ctx);
        }
    }
}