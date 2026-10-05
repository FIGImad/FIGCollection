using FIGCommon.Models;
using FIGCommon.Utilities;


namespace FIG.Studies
{
    public class BaseStudy 
    {
        // Explicit opt-in: an unknown derived study may have additional mutable state.
        protected virtual string? CheckpointConfiguration => null;
        protected virtual System.Text.Json.JsonElement CaptureRuntimeState()
            => System.Text.Json.JsonSerializer.SerializeToElement(new { });
        protected virtual void RestoreRuntimeState(System.Text.Json.JsonElement state) { }
        public bool SupportsCheckpoint => CheckpointConfiguration != null;
        private string CheckpointIdentity => $"{GetType().FullName}|{GetType().Module.ModuleVersionId}|{CheckpointConfiguration}";

        public StudyCheckpoint CaptureCheckpoint()
            => CaptureCheckpoint(false);

        internal StudyCheckpoint CaptureCheckpoint(bool referencePrices)
        {
            if (!SupportsCheckpoint) throw new NotSupportedException($"{GetType().Name} does not support checkpoints.");
            return new StudyCheckpoint(CheckpointIdentity,
                paramHist.Items.Take(paramHist.Count).Select(p =>
                    System.Text.Json.JsonSerializer.SerializeToElement(p, p!.GetType(),
                        referencePrices ? StudyStateJson.PriceReferences : StudyStateJson.Options)).ToArray(),
                CaptureRuntimeState());
        }

        public void RestoreCheckpoint(StudyCheckpoint checkpoint)
        {
            if (!SupportsCheckpoint || checkpoint.Identity != CheckpointIdentity || checkpoint.Parameters.Length > MAXBUFFER)
                throw new InvalidDataException("Study checkpoint type, configuration or version mismatch.");
            var parameterType = GetParams()?.GetType() ?? throw new InvalidDataException("Missing study parameter type.");
            var requiredNames = System.Text.Json.JsonSerializer.SerializeToElement(GetParams(), parameterType, StudyStateJson.Options)
                .EnumerateObject().Select(p => p.Name).ToArray();
            foreach (var parameter in checkpoint.Parameters)
                if (parameter.ValueKind != System.Text.Json.JsonValueKind.Object
                    || requiredNames.Any(name => !parameter.TryGetProperty(name, out _)))
                    throw new InvalidDataException("Study checkpoint is missing parameter fields.");
            var restored = checkpoint.Parameters.Select(p =>
                (BaseParams?)System.Text.Json.JsonSerializer.Deserialize(p, parameterType, StudyStateJson.Options)
                ?? throw new InvalidDataException("Invalid study parameters.")).ToArray();
            foreach (var parameter in restored) parameter.OnCheckpointRestored();
            for (int i = 1; i < restored.Length; i++)
                if (restored[i].Price.RawTime >= restored[i - 1].Price.RawTime)
                    throw new InvalidDataException("Unordered study parameter history.");
            RestoreRuntimeState(checkpoint.Runtime);
            paramHist.Clear();
            foreach (var parameter in restored.Reverse()) paramHist.AddToFront(parameter);
        }

        private readonly int MAXBUFFER = 10;
        protected CircularBuffer<BaseParams> paramHist;


        public BaseStudy()
        {
             paramHist = new CircularBuffer<BaseParams>(MAXBUFFER);
        }

        public BaseStudy(BaseStudy study)
        {
            paramHist = new CircularBuffer<BaseParams>(MAXBUFFER);
            Assign(study);
        }


        public virtual void Assign(BaseStudy study)
        {
            paramHist.Clear();
            foreach (var item in study.paramHist.Items)
            {
                paramHist.AddToFront(item?.Clone());
            }
        }

        public void Process(PriceDataRS price, CircularBuffer<BarStudy> studies, CPriceList priceList)
        {
            // get the param from previous price
            while (paramHist.Items[0] != null && paramHist.Items[0]?.Price.RawTime >= price.RawTime)
            {
                paramHist.RemoveAt(0);
            }
            var cParam = paramHist.Items[0]?.Clone()?? GetParams();
            paramHist.AddToFront(cParam);
            if (cParam != null)
            {
                cParam.Price = new PriceDataRS(price);
            }
            Calc(studies, priceList);
        }

        public virtual void Calc(CircularBuffer<BarStudy> studies, CPriceList priceList)
        {
            throw new NotImplementedException();
        }

        public virtual BaseParams? GetParams()
        {
            throw new NotImplementedException();
        }

        public virtual string Tag(string? varName = null)
        {
            throw new NotImplementedException();
        }

    }
}

