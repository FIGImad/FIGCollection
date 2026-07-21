#include "fig/logger.hpp"
#include <chrono>
#include <format>
#include <iostream>

namespace fig
{
Logger::Logger(std::filesystem::path file)
{
    if (!file.empty())
        file_.open(file, std::ios::app);
}
void Logger::write(LogLevel level, std::string_view message)
{
    static constexpr std::string_view names[]{"DBG", "INF", "WRN", "ERR"};
    const auto now = std::chrono::floor<std::chrono::seconds>(std::chrono::system_clock::now());
    const auto line = std::format("{:%Y-%m-%d %H:%M:%S} [{}] {}", now, names[static_cast<int>(level)], message);
    std::scoped_lock lock(mutex_);
    std::clog << line << '\n';
    if (file_)
    {
        file_ << line << '\n';
        file_.flush();
    }
}
} // namespace fig
