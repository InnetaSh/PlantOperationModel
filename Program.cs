using System.Collections.Concurrent;

class Program
{
    static Random random = new Random();
    static int MaxStorage = 10;


    static Semaphore storageSemaphore = new Semaphore(1, 1);
    static Dictionary<string, BlockingCollection<string>> storage = new Dictionary<string, BlockingCollection<string>>();


    static object monitorLock = new object();
    static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

    static async Task Main(string[] args)
    {

        storage["A"] = new BlockingCollection<string>(MaxStorage);
        storage["B"] = new BlockingCollection<string>(MaxStorage);
        storage["C"] = new BlockingCollection<string>(MaxStorage);

        var productionThreads = new List<Thread>
        {
            new Thread(() => Produce("A")),
            new Thread(() => Produce("B")),
            new Thread(() => Produce("C"))
        };


        var consumerThreads = new Thread[5];
        for (int i = 0; i < 5; i++)
        {
            int consumerId = i+1;
            consumerThreads[i] = new Thread(() => Consume(consumerId));
        }


        foreach (var thread in productionThreads) thread.Start();
        foreach (var thread in consumerThreads) thread.Start();


        cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(30));

        foreach (var thread in productionThreads) thread.Join();
        foreach (var thread in consumerThreads) thread.Join();
    }





    static void Produce(string type)
    {
        while (!cancellationTokenSource.Token.IsCancellationRequested)
        {
            storageSemaphore.WaitOne();
           
            if (storage[type].Count <= MaxStorage)
            {
                storage[type].TryAdd($"Product {type}");
                Console.WriteLine($"Произведено продукт {type}");
            }
            else
            {
                Console.WriteLine($"Склад для продукта {type} полон, остановка производстваn.");
            }
           
            storageSemaphore.Release();
            Thread.Sleep(random.Next(100, 501));
        }
    }

    static void Consume(int consumerId)
    {
        while (!cancellationTokenSource.Token.IsCancellationRequested)
        {
            Thread.Sleep(random.Next(100, 501));
            string productType = random.Next(3) switch
            {
                0 => "A",
                1 => "B",
                _ => "C"
            };

            storageSemaphore.WaitOne(); 
           
            if (storage[productType].Count != 0)
            {
                var product = storage[productType].Take();
                Console.WriteLine($"Потребитель {consumerId} забрал {product}");
                
            }
            else { Console.WriteLine($"Потребитель {consumerId} ожидает продукт {productType}"); }

            storageSemaphore.Release();
           
        }
    }
}