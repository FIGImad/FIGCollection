using System.Security.Claims;
using FIGCommon.Models.SignalPublication;

namespace FIGAutoTradeExSvc.SignalIngress;

public sealed class SignalIngressOptions
{
    public bool Enabled { get; set; }
    public string ProducerId { get; set; } = "";
    public string SubjectClaim { get; set; } = ClaimTypes.NameIdentifier;
    public string Subject { get; set; } = "";
    public string[] Strategies { get; set; } = [];
    public void Validate()
    {
        if (Enabled && (string.IsNullOrWhiteSpace(ProducerId) || ProducerId.Length > 50
            || ProducerId != ProducerId.Trim() || string.IsNullOrWhiteSpace(Subject)
            || string.IsNullOrWhiteSpace(SubjectClaim) || Strategies.Length == 0
            || Strategies.Any(s => string.IsNullOrWhiteSpace(s) || s.Length > 100 || s != s.Trim())))
            throw new InvalidOperationException("SignalIngress requires a producer, authenticated subject and explicit strategy allow-list.");
    }
    public bool Allows(ClaimsPrincipal user, SignalPublicationMessage message) => Enabled
        && user.Identity?.IsAuthenticated == true
        && user.FindAll(SubjectClaim).Any(c => c.Value == Subject)
        && message.ProducerId == ProducerId
        && message.Signal != null && Strategies.Contains(message.Signal.Strategy, StringComparer.Ordinal);
}
