namespace FIGCommon.Utilities
{
    public class CircularBuffer<T>
    {
        private readonly T?[] _buffer;
        private int _count;

        public CircularBuffer(int capacity)
        {
            _buffer = new T[capacity];
            _count = 0;
        }

        public CircularBuffer(CircularBuffer<T> rec)
        {
            _count = rec._count;
            _buffer = new T[rec._buffer.Length];
            Array.Copy(rec._buffer, 0, _buffer, 0, _buffer.Length);
        }

        public void AddToFront(T? item)
        {
            // Shift right
            Array.Copy(_buffer, 0, _buffer, 1, _buffer.Length - 1);
            _buffer[0] = item;
            _count = Math.Min(_count + 1, _buffer.Length);
        }

        // Remove item from front (shifts all items left)
        public void RemoveFromFront()
        {
            if (_count == 0)
                return;

            // Shift left
            Array.Copy(_buffer, 1, _buffer, 0, _buffer.Length - 1);
            _buffer[_buffer.Length - 1] = default;
            _count--;
        }

        // clear, remove all items (or set values to default if T is a value type)
        public void Clear()
        {
            if (_count == 0)
                return;

            for (int i = 0; i < _buffer.Length; i++)
            {
                _buffer[i] = default;
            }
            _count = 0;
        }

        // Remove item from specific index
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _count)
                throw new ArgumentOutOfRangeException(nameof(index));

            // Shift items after the index left
            Array.Copy(_buffer, index + 1, _buffer, index, _buffer.Length - index - 1);
            _buffer[_buffer.Length - 1] = default;
            _count--;
        }

        public bool TryPeekLast(out T? item)
        {
            if (_count == 0)
            {
                item = default;
                return false;
            }
            item = _buffer[0];
            return true;
        }

        public bool TryPeekFirst(out T? item)
        {
            if (_count == 0)
            {
                item = default;
                return false;
            }
            item = _buffer[_count - 1];
            return true;
        }

        public bool TryPeek(int ndx, out T? item)
        {
            if (_count == 0 || ndx < 0 || ndx >= _count)
            {
                item = default;
                return false;
            }
            item = _buffer[ndx];
            return true;
        }

        public IReadOnlyList<T?> Items => Array.AsReadOnly(_buffer);
        public int Count => _count;
    }
}
