using System.Text.Json;
using FIGCommon.Models;
using Microsoft.Extensions.Logging;

namespace FIG.Studies;

public sealed class StudyCollectionContext
{
    private static readonly JsonSerializerOptions ParameterOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public StudyCollectionContext(
        StudyColRS configuration,
        TickerRS ticker,
        IntervalRS interval,
        ILoggerFactory loggerFactory)
    {
        Configuration = new StudyColRS(configuration);
        Ticker = new TickerRS(ticker);
        Interval = new IntervalRS(interval);
        LoggerFactory = loggerFactory;

        try
        {
            using var document = JsonDocument.Parse(configuration.ColParams);
            Parameters = document.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException(
                $"StudyCol {configuration.Id} contains invalid ColParams JSON.", ex);
        }

        if (Parameters.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException(
                $"StudyCol {configuration.Id} ColParams must be a JSON object.");
        }
    }

    public StudyColRS Configuration { get; }
    public TickerRS Ticker { get; }
    public IntervalRS Interval { get; }
    public JsonElement Parameters { get; }
    public ILoggerFactory LoggerFactory { get; }

    public T DeserializeParameters<T>() where T : class
    {
        return Parameters.Deserialize<T>(ParameterOptions)
            ?? throw new InvalidDataException(
                $"StudyCol {Configuration.Id} parameters could not be read as {typeof(T).Name}.");
    }
}
