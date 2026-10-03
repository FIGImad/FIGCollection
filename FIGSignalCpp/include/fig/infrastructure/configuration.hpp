#pragma once
#include <filesystem>
#include <map>
#include <string>

namespace fig
{
class Configuration final
{
  public:
    static Configuration load(const std::filesystem::path& path);
    [[nodiscard]] std::string get(std::string_view dotted_key, std::string fallback = {}) const;
    [[nodiscard]] int get_int(std::string_view dotted_key, int fallback) const;

  private:
    std::map<std::string, std::string, std::less<>> values_;
};
} // namespace fig
