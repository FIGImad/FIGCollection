using FIGCommon.Models;

namespace FIG.Studies
{
    public class BaseParams
    {
        public PriceDataRS Price { get; set; } = new PriceDataRS();
        public bool IsValid { get; set; } = true;

        public BaseParams()
        {
            Price = new PriceDataRS();
        }

        public BaseParams(BaseParams param)
        {
            Assign(param);
        }

        public virtual void Assign(BaseParams param)
        {
            Price = new PriceDataRS(param.Price);
        }

        public virtual BaseParams Clone()
        {
            throw new NotImplementedException();
        }

    }
}


