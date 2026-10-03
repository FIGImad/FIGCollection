using FIGOptimizationRunner.Configuration;

namespace FIGOptimizationRunner.Search;

// Replace this implementation to add grid, Bayesian, evolutionary or externally proposed candidates.
// All strategies must keep the baseline first and obey cancellation and explicit evaluation budgets.
public interface ICandidateGenerator
{
    string Name { get; }
    IEnumerable<ParameterSet> Generate(SearchConfiguration configuration, ParameterSet baseline, CancellationToken cancellation);
}
