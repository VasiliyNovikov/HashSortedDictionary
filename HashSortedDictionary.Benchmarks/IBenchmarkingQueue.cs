namespace HashSortedDictionary.Benchmarks
{
    public interface IBenchmarkingQueue<T>
    {
        void Enqueue(T key);
        T Dequeue();
    }
}