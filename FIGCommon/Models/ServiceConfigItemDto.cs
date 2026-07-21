namespace FIGCommon.Models
{
    public class ServiceConfigItemDto
    {
        public string Key { get; set; } = string.Empty;
        public object? Value { get; set; } = null;

        public ServiceConfigItemDto()
        {
            Key = string.Empty;
            Value = null;
        }

        public ServiceConfigItemDto(ServiceConfigItemDto rec)
        {
            Clone(rec);
        }

        public void Clone(ServiceConfigItemDto src)
        {
            var srcT = src.GetType();
            var dstT = this.GetType();
            foreach (var f in srcT.GetFields())
            {
                var dstF = dstT.GetField(f.Name);
                if (dstF == null || dstF.IsLiteral)
                    continue;
                dstF.SetValue(this, f.GetValue(src));
            }

            foreach (var f in srcT.GetProperties())
            {
                var dstF = dstT.GetProperty(f.Name);
                if (dstF == null)
                    continue;

                dstF.SetValue(this, f.GetValue(src, null), null);
            }
        }
    }
}
