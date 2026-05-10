using System.Net;
using System.Text.Json;

namespace PrviProjekat;

public class RijksServis
{
    private readonly string baseUrl =
        "https://data.rijksmuseum.nl/search/collection";

    private readonly HttpClient client; //Objekat zadužen za slanje HTTP zahteva ka internetu.

    private readonly Cache<List<RijksItem>> cache = new Cache<List<RijksItem>>(TimeSpan.FromSeconds(30), 100 );
//Inicijalizacija keša koji čuva liste umetničkih dela (RijksItem).TTL (Time To Live) je postavljen na 30 sekundi.Maksimalna veličina je 100 unosa.
    public RijksServis()
    {
        client = new HttpClient();
    }

    public List<RijksItem> Pretrazi(RijksSearchRequest request)
    {
        string query = NapraviQuery(request);
    //Poziva pomoćnu metodu da pretvori objekat zahteva u URL string (npr. title=nightwatch&artist=rembrandt)

        string key = Helper.Normalizuj(query);
    //Pravi jedinstveni ključ za keš od upita tako da je moguce naci objekte u kesu iako su parametri prosledjeni u izmesanom redosledu
      
        Log.Info($"PRETRAGA: {query}");

        return cache.GetOrAdd(key, () =>
        {
            //Proverava da li rezultat za ovaj ključ već postoji u kešu. 
            // Ako postoji, odmah vraća listu bez trošenja vremena na API poziv.
            // Ako ne postoji, izvršava se blok koda unutar zagrada (factory).
            
            Log.Info($"API POZIV: {key}");

            string url = $"{baseUrl}?{query}"; //Sklapa pun URL za poziv

            var response = PozoviApi(url); //Vrši stvarni mrežni poziv.

            return response?.orderedItems ?? new List<RijksItem>(); //Vraća listu rezultata iz API-ja, ili praznu listu ako je odgovor bio prazan (null).
        });
    }

    private string NapraviQuery(RijksSearchRequest request)
    {
        var properties = typeof(RijksSearchRequest).GetProperties();

        List<string> parametri = new();

        foreach (var p in properties)
        {
            var value = p.GetValue(request);

            if (value == null)
                continue;

            string name =
                Char.ToLowerInvariant(p.Name[0]) +
                p.Name.Substring(1);

            string val = value is bool b ? b.ToString().ToLower() : value.ToString()!;

            parametri.Add( $"{name}={WebUtility.UrlEncode(val)}"
            );
        }

        return string.Join("&", parametri);
    }

    private RijksSearchResponse? PozoviApi(string url)
    {
        try
        {
            Log.Info($"SALJEM HTTP: {url}");

            string json = client.GetStringAsync(url).Result; //Šalje GET zahtev i čeka JSON tekst od servera.
//Result -Blokira trenutnu nit dok odgovor ne stigne (ovo radi nit iz ThreadPool-a koju je poslao WorkerThreadPool).

            Log.Info("API ODGOVOR USPESAN");

            return JsonSerializer.Deserialize<RijksSearchResponse>( //Pretvara primljeni JSON tekst nazad u C# objekte.
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true //Kaže JSON parseru da ignoriše razliku između malih i velikih slova u nazivima polja.
                });
        }
        //client.GetStringAsync(url).Result može da traje i po nekoliko sekundi. 
        // Da nema WorkerThreadPool koji koristi niti iz bazena, glavna nit bi bila blokirana i server ne bi mogao da primi nove zahteve dok ovaj API poziv ne završi.
        //  Ovako, samo jedna nit iz pool-a "stoji i čeka", dok ostale mogu slobodno da rade.
        catch (Exception ex)
        {
            Log.Error($"API GRESKA: {ex.Message}");

            return null;
        }
    }
}