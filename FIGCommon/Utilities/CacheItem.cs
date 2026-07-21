namespace FIGCommon.Utilities
{
    public class CacheItem<T>
    {
        T item;
        DateTime expiryTime;
        int timeout = int.MaxValue; 

        public CacheItem(T item, int timeout)
        {
            this.item = item;
            this.timeout = timeout;
            this.expiryTime = DateTime.Now.AddMilliseconds(timeout);
        }

        public T GetItem()
        {
            return item;
        }
        public bool IsExpired()
        {
            return DateTime.Now > this.expiryTime;
        }

        public void Touch()
        {
            DateTime.Now.AddMilliseconds(this.timeout);
        }

        public void Touch(T item)
        {
            this.item = item;
            this.expiryTime = DateTime.Now.AddMilliseconds(this.timeout);
        }
    }
}
