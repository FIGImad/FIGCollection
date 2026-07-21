#include "fig/configuration.hpp"
#include <charconv>
#include <fstream>
#include <regex>
#include <sstream>
#include <stdexcept>

namespace fig
{
Configuration Configuration::load(const std::filesystem::path& path)
{
    std::ifstream input(path, std::ios::binary);
    if (!input)
        throw std::runtime_error("Cannot open configuration: " + path.string());
    std::ostringstream buffer;
    buffer << input.rdbuf();
    const auto json = buffer.str();
    Configuration result;
    // The production parser is intentionally dependency-free. It indexes leaf JSON keys;
    // duplicate leaf names should be accessed by their unique FIG configuration name.
    const std::regex leaf(R"re("([^"\\]+)"\s*:\s*("((?:\\.|[^"\\])*)"|-?[0-9]+|true|false|null))re", std::regex::icase);
    for (auto it = std::sregex_iterator(json.begin(), json.end(), leaf); it != std::sregex_iterator(); ++it)
    {
        auto value = (*it)[2].str();
        if (value.size() >= 2 && value.front() == '"')
            value = (*it)[3].str();
        result.values_[(*it)[1].str()] = value;
    }
    return result;
}

std::string Configuration::get(std::string_view key, std::string fallback) const
{
    const auto leaf = key.substr(key.find_last_of('.') == std::string_view::npos ? 0 : key.find_last_of('.') + 1);
    const auto found = values_.find(std::string(leaf));
    return found == values_.end() ? std::move(fallback) : found->second;
}

int Configuration::get_int(std::string_view key, int fallback) const
{
    const auto text = get(key);
    int value{};
    const auto [end, error] = std::from_chars(text.data(), text.data() + text.size(), value);
    return error == std::errc{} && end == text.data() + text.size() ? value : fallback;
}
} // namespace fig
