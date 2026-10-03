#pragma once
#include "fig/studies/study_base_adx.hpp"
namespace fig::studies
{
// Experimental classes present in the EFS but not instantiated by its StudyCollection.
class StudyLBaseADXVariant
{
  public:
    [[nodiscard]] StudyBar process(const AdxBaseInput&);

  protected:
    StudyLBaseADXVariant(bool new_variant, std::size_t depth) : new_(new_variant), states_(depth)
    {
    }

  private:
    struct State
    {
        std::int64_t raw_time{};
        int side{}, bars_since_close{};
        Decimal trigger{-0.25L}, max_bias{}, stop_bias{};
        std::optional<AdxBaseInput> last_valid;
    };
    bool new_;
    LiveStateHistory<State> states_;
};
class StudyLBaseADXAttempt final : public StudyLBaseADXVariant
{
  public:
    explicit StudyLBaseADXAttempt(std::size_t depth = default_revision_depth) : StudyLBaseADXVariant(false, depth)
    {
    }
};
class StudyLBaseADXNew final : public StudyLBaseADXVariant
{
  public:
    explicit StudyLBaseADXNew(std::size_t depth = default_revision_depth) : StudyLBaseADXVariant(true, depth)
    {
    }
};
} // namespace fig::studies
