using FIGCommon.Models.FIGProviderAPI;

namespace FIGCommon.Interfaces;

/// <summary>Optional contract discovery capability; implementations return all matching, non-expired futures.</summary>
public interface IContractChainProvider
{
    IReadOnlyList<ContractInfo> ContractChain(InstrumentLookupRequest request,int timeoutInMsec=30000);
}
