namespace Singleton
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var cache = CacheProcessor.Instance;
            cache.SaveDataCache("1", "1");
            cache.SaveDataCache("2", "2");

            var cache2 = CacheProcessor.Instance;
            Thread.Sleep(15000);
            cache2.TryGetValueFromCache("2", out string? result);
            Console.Write(result);

            cache2.SaveDataCache("3", "3");
            cache.TryGetValueFromCache("3", out result);
            Console.Write(result);
        }
    }
}