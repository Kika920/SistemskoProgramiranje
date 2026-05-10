using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace PrviProjekat;

public class Server
{
    private readonly HttpListener listener;
    private readonly RijksServis servis;
    private readonly QueueRequest red;
    private readonly WorkerThreadPool pool;

    private bool running;

    public Server(string url, RijksServis servis)
    {
        listener = new HttpListener();
        listener.Prefixes.Add(url);

        this.servis = servis;

        red = new QueueRequest();
        pool = new WorkerThreadPool(red, this, 10);
    }

    public void Start()
    {
        running = true;
        listener.Start();

        Log.Info("SERVER START");

        pool.Go();

        new Thread(() =>
        {
            Console.ReadLine();
            Stop();
        })
        { IsBackground = true }.Start();

        while (running)
        {
            try
            {
                var ctx = listener.GetContext();
                if (ctx.Request.Url.AbsolutePath == "/favicon.ico")
{
    ctx.Response.StatusCode = 204;
    ctx.Response.Close();
    continue;
}

                if (!running)
                    break;

                red.Dodaj(ctx);
            }
            catch
            {
                break;
            }
        }
    }

    public void Stop()
    {
        running = false;

        try { listener.Stop(); } catch { }
        try { listener.Close(); } catch { }

        red.Stop(); 

        Log.Info("SERVER STOP");
    }

    public void Obradi(HttpListenerContext ctx)
    {

if (string.IsNullOrWhiteSpace(ctx.Request.Url.Query))
{
    Vrati(ctx, 400, "{\"error\":\"Prazan upit\"}");
    return;
}
        try
        {
            Log.Info($"REQUEST: {ctx.Request.Url}");

            var qs = ctx.Request.QueryString;

            var request = new RijksSearchRequest
            {
                Type = qs["type"],
                AboutActor = qs["aboutActor"],
                Creator = qs["creator"],
                CreationDate = qs["creationDate"],
                Description = qs["description"],
                ImageAvailable = ParseBool(qs["imageAvailable"]),
                Material = qs["material"],
                MemberOfSetId = qs["memberOfSetId"],
                ObjectNumber = qs["objectNumber"],
                PageToken = qs["pageToken"],
                Technique = qs["technique"],
                Title = qs["title"]
            };

            var result = servis.Pretrazi(request);

            if (result == null || result.Count == 0)
            {
                Vrati(ctx, 404, "{\"error\":\"Nema rezultata\"}");
                return;
            }

            Vrati(ctx, 200, JsonSerializer.Serialize(result));
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            Vrati(ctx, 500, "{\"error\":\"Greska\"}");
        }
    }

    private bool? ParseBool(string? value)
    {
        if (bool.TryParse(value, out var b))
            return b;

        return null;
    }

    private void Vrati(HttpListenerContext ctx, int code, string json)
    {
        var buf = Encoding.UTF8.GetBytes(json);

        ctx.Response.StatusCode = code;
        ctx.Response.ContentType = "application/json";
        ctx.Response.ContentEncoding = Encoding.UTF8;

        ctx.Response.OutputStream.Write(buf, 0, buf.Length);
        ctx.Response.Close();
    }
}