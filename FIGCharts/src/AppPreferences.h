#pragma once
#include <windows.h>
#include <shlobj.h>
#include <filesystem>
#include <fstream>
#include <iomanip>
#include <string>
#include <vector>
#include <stdexcept>

namespace figcharts {
struct Preferences {
    int chartType = 0;
    bool dark = false, legend = true, financialLegend = false;
    int timeMode = 0; // UTC, computer local, named Windows time zone
    std::wstring zoneKey;
    std::wstring databaseConnection;
    bool showDatabasePerformance=false;
    std::string title;
};
inline Preferences preferences;
inline std::vector<DYNAMIC_TIME_ZONE_INFORMATION> timeZones;
inline std::filesystem::path preferenceFolder() {
    wchar_t folder[MAX_PATH];
    if (FAILED(SHGetFolderPathW(nullptr, CSIDL_LOCAL_APPDATA, nullptr, 0, folder))) throw std::runtime_error("Could not locate settings folder.");
    return std::filesystem::path(folder) / L"FIGCharts";
}
inline std::string utf8(const std::wstring& text) {
    int count = WideCharToMultiByte(CP_UTF8, 0, text.data(), static_cast<int>(text.size()), nullptr, 0, nullptr, nullptr);
    std::string result(count, '\0');
    WideCharToMultiByte(CP_UTF8, 0, text.data(), static_cast<int>(text.size()), result.data(), count, nullptr, nullptr); return result;
}
inline std::wstring fromUtf8(const std::string& text) {
    int count = MultiByteToWideChar(CP_UTF8, 0, text.data(), static_cast<int>(text.size()), nullptr, 0);
    std::wstring result(count, L'\0'); MultiByteToWideChar(CP_UTF8, 0, text.data(), static_cast<int>(text.size()), result.data(), count); return result;
}
inline void enumerateZones() {
    if (!timeZones.empty()) return;
    DYNAMIC_TIME_ZONE_INFORMATION zone{};
    for (DWORD i = 0; EnumDynamicTimeZoneInformation(i, &zone) == ERROR_SUCCESS; ++i) timeZones.push_back(zone);
}
inline DYNAMIC_TIME_ZONE_INFORMATION displayZone() {
    DYNAMIC_TIME_ZONE_INFORMATION zone{};
    if (preferences.timeMode == 1) { GetDynamicTimeZoneInformation(&zone); return zone; }
    for (auto& z : timeZones) if (preferences.zoneKey == z.TimeZoneKeyName) return z;
    return zone;
}
inline SYSTEMTIME utcSystemTime(double seconds) {
    ULARGE_INTEGER ticks; ticks.QuadPart = static_cast<ULONGLONG>((seconds + 11644473600.0) * 10000000.0);
    FILETIME ft{ticks.LowPart, ticks.HighPart}; SYSTEMTIME st{};
    if (!FileTimeToSystemTime(&ft, &st)) throw std::runtime_error("Date is outside the supported range."); return st;
}
inline double systemSeconds(const SYSTEMTIME& st) {
    FILETIME ft{}; if (!SystemTimeToFileTime(&st, &ft)) throw std::runtime_error("Invalid date/time.");
    ULARGE_INTEGER ticks; ticks.LowPart = ft.dwLowDateTime; ticks.HighPart = ft.dwHighDateTime;
    return static_cast<double>(ticks.QuadPart / 10000000ULL) - 11644473600.0;
}
inline SYSTEMTIME displaySystemTime(double seconds) {
    auto st = utcSystemTime(seconds);
    if (preferences.timeMode) {
        auto zone = displayZone(); SYSTEMTIME converted{};
        if (!SystemTimeToTzSpecificLocalTimeEx(&zone, &st, &converted)) throw std::runtime_error("Time zone conversion failed.");
        return converted;
    }
    return st;
}
inline double displayToUtc(const SYSTEMTIME& input) {
    if (!preferences.timeMode) return systemSeconds(input);
    auto zone = displayZone(); SYSTEMTIME utc{};
    if (!TzSpecificLocalTimeToSystemTimeEx(&zone, &input, &utc)) throw std::runtime_error("Invalid date/time in the selected time zone.");
    auto seconds = systemSeconds(utc); auto roundTrip = displaySystemTime(seconds);
    if (roundTrip.wYear != input.wYear || roundTrip.wMonth != input.wMonth || roundTrip.wDay != input.wDay || roundTrip.wHour != input.wHour || roundTrip.wMinute != input.wMinute || roundTrip.wSecond != input.wSecond)
        throw std::runtime_error("This local time does not exist because of the daylight-saving clock change.");
    return seconds;
}
inline std::wstring zoneLabel() {
    if (!preferences.timeMode) return L"UTC";
    auto zone = displayZone(); return preferences.timeMode == 1 ? L"Local (" + std::wstring(zone.TimeZoneKeyName) + L")" : std::wstring(zone.TimeZoneKeyName);
}
inline std::wstring formatTimestamp(double seconds) {
    auto st = displaySystemTime(seconds); wchar_t value[64];
    swprintf_s(value, L"%04u-%02u-%02u %02u:%02u:%02u", st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond);
    int offset = static_cast<int>(systemSeconds(st) - seconds);
    wchar_t suffix[32]; swprintf_s(suffix, L" UTC%c%02d:%02d", offset < 0 ? L'-' : L'+', abs(offset) / 3600, abs(offset) % 3600 / 60);
    return std::wstring(value) + (preferences.timeMode ? suffix : L" UTC");
}
inline void writePreferences(const std::filesystem::path& path) {
    std::filesystem::create_directories(path.parent_path()); auto temp = path; temp += L".tmp";
    { std::ofstream out(temp); out << "FIGChartsPreferences 1\n" << preferences.chartType << ' ' << preferences.dark << ' ' << preferences.legend << ' ' << preferences.timeMode << '\n'
        << std::quoted(utf8(preferences.zoneKey)) << '\n' << std::quoted(preferences.title) << '\n' << preferences.financialLegend << '\n' << std::quoted(utf8(preferences.databaseConnection)) << '\n' << preferences.showDatabasePerformance << '\n';
      out.flush(); if (!out) throw std::runtime_error("Could not save preferences."); }
    if (!MoveFileExW(temp.c_str(), path.c_str(), MOVEFILE_REPLACE_EXISTING | MOVEFILE_WRITE_THROUGH)) throw std::runtime_error("Could not replace preferences.");
}
inline void readPreferences(const std::filesystem::path& path) {
    std::ifstream in(path); std::string magic, key; int version; Preferences p;
    if (!(in >> magic >> version) || magic != "FIGChartsPreferences" || version != 1) return;
    if (!(in >> p.chartType >> p.dark >> p.legend >> p.timeMode >> std::quoted(key) >> std::quoted(p.title))) return;
    if (p.chartType < 0 || p.chartType > 2 || p.timeMode < 0 || p.timeMode > 2) return;
    in >> std::ws; if(!in.eof() && !(in >> p.financialLegend)) return;
    std::string connection;if(in >> std::quoted(connection))p.databaseConnection=fromUtf8(connection);
    in >> p.showDatabasePerformance; // Optional field: older files keep the default off.
    p.zoneKey = fromUtf8(key); preferences = p;
    if (p.timeMode == 2) {
        bool found = false; for (auto& z : timeZones) if (p.zoneKey == z.TimeZoneKeyName) found = true;
        if (!found) preferences.timeMode = 0;
    }
}
}
