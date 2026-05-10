namespace PrviProjekat;

public class Cache<T>
{
    private readonly Dictionary<string, CacheEntry<T>> mapa = new();
    //Ovde se čuvaju podaci

    private readonly HashSet<string> uToku = new();
//Ovde upisujemo ključeve koje neka nit upravo sada skida sa interneta, da druge niti ne bi radile isto
    private readonly object locker = new();
    //Osigurava da samo jedna nit može da čita ili piše po mapi u datom trenutku

    private readonly TimeSpan ttl;

    private readonly int maxSize;

    public Cache(TimeSpan ttl, int maxSize)
    {
        this.ttl = ttl;
        this.maxSize = maxSize;
    }

    public T GetOrAdd(string key, Func<T> factory)
    {
        lock (locker)
        {
            //Ako nit vidi da se ovaj podatak već skida (u uToku je), ona ulazi u ovu petlju.
            while (uToku.Contains(key))
            {
                Log.Info($"WAIT CACHE: {key}");

                Monitor.Wait(locker);
                //Nit zaspi. Ona oslobađa lock i čeka da je neko probudi (Pulse).
            }

            if (mapa.TryGetValue(key, out var entry))
            {
                if (DateTime.UtcNow - entry.KreiranU <= ttl)
                { //Proverava da li je podatak previše star (Time To Live).
               

                    Log.Info($"CACHE HIT: {key}");

                    return entry.Podaci!;
                }

                mapa.Remove(key);
            }

            uToku.Add(key);
            //Nit koja je prva stigla "rezerviše" ovaj ključ
        }

        T data;

        try
        {
            Log.Info($"CACHE MISS: {key}");

            data = factory();
            //Izvan lock-a: Ovde se vrši HTTP poziv
            //  Radimo to van lock-a da ne bismo blokirali ceo keš dok čekamo net
        }

        finally
        {
            lock (locker)
            {
                uToku.Remove(key);

                Monitor.PulseAll(locker);
                //Kada jedna nit završi skidanje, budi sve ostale koje su čekale na Monitor.Wait
            }
        }

        lock (locker)
        {
            mapa[key] = new CacheEntry<T>
            {
                Podaci = data,
                KreiranU = DateTime.UtcNow
            };

            Log.Info($"CACHE SET: {key}");

            if (mapa.Count > maxSize)
            {
                IzbaciNajstariji();
            }

            return data;
        }
    }

//Ako je keš pun (maxSize), briše najstariji podatak da oslobodi mesto
    private void IzbaciNajstariji()
    {
        var najstariji = mapa
            .OrderBy(x => x.Value.KreiranU)
            .First();

        mapa.Remove(najstariji.Key);

        Log.Info($"CACHE EVICT: {najstariji.Key}");
    }

    public void Clear()
    {
        lock (locker)
        {
            mapa.Clear();

            Log.Info("CACHE CLEAR");
        }
    }
}