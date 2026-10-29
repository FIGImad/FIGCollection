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
            IsValid = param.IsValid;
        }

        public virtual void OnCheckpointRestored() { }

        // Explicit opt-in only: implementations must copy every state field and
        // must not retain references to other historical parameter objects.
        public virtual bool TryCopyForNextBar(BaseParams previous, PriceDataRS price) => false;

        protected void CopyBaseForNextBar(BaseParams previous, PriceDataRS price)
        {
            IsValid = previous.IsValid;
            Price.Id = price.Id; Price.DataSetId = price.DataSetId;
            Price.RawTime = price.RawTime; Price.PriceDate = price.PriceDate;
            Price.Open = price.Open; Price.High = price.High; Price.Low = price.Low;
            Price.Close = price.Close; Price.Volume = price.Volume;
        }

        public virtual BaseParams Clone()
        {
            throw new NotImplementedException();
        }

    }
}


