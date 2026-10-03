#pragma once
#include <filesystem>
#include <fstream>
#include <mutex>
#include <string_view>

namespace fig
{
enum class LogLevel
{
    debug,
    info,
    warning,
    error
};
class Logger final
{
  public:
    explicit Logger(std::filesystem::path file = {});
    void write(LogLevel level, std::string_view message);

  private:
    std::mutex mutex_;
    std::ofstream file_;
};
} // namespace fig
