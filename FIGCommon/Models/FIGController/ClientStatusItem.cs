namespace FIGCommon.Models.FIGController
{
    public class ClientStatusItem : ICloneable
    {
        public string Name { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public object? Value { get; set; } = null;
        public long UpdateTime { get; set; } = 0L;

        public ClientStatusItem()
        {
            Name = string.Empty;
            Source = string.Empty;
            Value = null;
            UpdateTime = 0L;
        }

        public ClientStatusItem(ClientStatusItem rec)
        {
            Name = rec.Name;
            Source = rec.Source;
            Value = rec.Value;
            UpdateTime = rec.UpdateTime;
        }

        public object Clone()
        {
            return new ClientStatusItem(this);
        }
    }
}