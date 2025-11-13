namespace TEngine
{
    public interface IBinarySerializer
    {
        public byte[] Serialize<T>(T obj) where T : class;
        public T Deserialize<T>(byte[] bytes) where T : class;
    }
}