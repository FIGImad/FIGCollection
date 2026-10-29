#pragma once
// Included inside the application's namespace; controls share the chart state.
enum { LegendStudies = 300, LegendAdd, CellEditor, PickerList, PickerOverlay, PickerText, PickerClose, LegendAdvanced, PickerSearch, PickerClear, PickerSearchFrame, LegendMoveUp, LegendMoveDown };
HWND legendStudies = nullptr;
bool syncingLegend = false;
std::vector<std::wstring> legendValues;
std::map<std::string, StudyEntry> savedStudies;
LRESULT CALLBACK auxiliaryProc(HWND, UINT, WPARAM, LPARAM);

std::wstring wide(const std::string& text) {
    int count = MultiByteToWideChar(CP_UTF8, 0, text.data(), static_cast<int>(text.size()), nullptr, 0);
    std::wstring result(count, L'\0');
    MultiByteToWideChar(CP_UTF8, 0, text.data(), static_cast<int>(text.size()), result.data(), count);
    return result;
}
std::filesystem::path settingsPath() {
    wchar_t folder[MAX_PATH];
    if (FAILED(SHGetFolderPathW(nullptr, CSIDL_LOCAL_APPDATA, nullptr, 0, folder))) throw std::runtime_error("Could not locate local settings folder");
    return std::filesystem::path(folder) / L"FIGCharts" / L"studies.txt";
}
void writeSettingsStream(std::ostream& out) { out << "FIGChartsStudies 2\n";
      for (const auto& [name, e] : savedStudies) out << "STUDY " << std::quoted(name) << ' ' << e.overlay << ' ' << e.color << ' ' << e.pattern << ' ' << e.width << ' ' << e.type << ' ' << e.enabled << ' ' << e.decimals << ' ' << e.order << '\n';
      out << std::setprecision(17) << "FINANCIAL " << std::quoted(financialConfig.study) << ' ' << financialConfig.aum << ' ' << financialConfig.multiplier << ' ' << financialConfig.commission << ' ' << financialConfig.quantity << ' ' << financialConfig.allocation << ' ' << financialConfig.reinvest << ' ' << financialConfig.showWinLoss << '\n';
      for(const auto& [side,style]:financialConfig.sides) out << "SIDE " << side << ' ' << style.color << ' ' << style.show << '\n';
      for(const auto& rule:backgroundRules) {out << "BACKGROUND " << std::quoted(rule.name) << ' ' << rule.enabled << ' ' << rule.color << ' ';writeBackgroundNode(out,rule.root);out << '\n';}
      out.flush(); if (!out) throw std::runtime_error("Could not save study settings");
}
void writeSettings(const std::filesystem::path& path) {
    std::filesystem::create_directories(path.parent_path());auto temporary=path;temporary+=L".tmp";
    {std::ofstream out(temporary);writeSettingsStream(out);}
    if (!MoveFileExW(temporary.c_str(), path.c_str(), MOVEFILE_REPLACE_EXISTING | MOVEFILE_WRITE_THROUGH)) throw std::runtime_error("Could not replace study settings");
}
void readSettingsStream(std::istream& in) { std::string signature; int version;
    if (!(in >> signature >> version) || signature != "FIGChartsStudies" || (version != 1 && version != 2)) return;
    std::map<std::string,StudyEntry> parsed; FinancialConfig config; std::vector<BackgroundRule> backgrounds; std::string line;std::getline(in,line);
    while(std::getline(in,line)) {
        if(line.empty()) continue;std::istringstream row(line);std::string tag,name;StudyEntry e;
        if(version==1) tag="STUDY"; else row>>tag;
        if(tag=="STUDY") {
            if(!(row>>std::quoted(name)>>e.overlay>>e.color>>e.pattern>>e.width) || e.color<0 || e.color>0xffffff || e.pattern<0 || e.pattern>3 || e.width<1 || e.width>6) throw std::runtime_error("Invalid study settings entry.");
            row>>std::ws;if(!row.eof() && (!(row>>e.type)||e.type<0||e.type>2))throw std::runtime_error("Invalid study type.");
            if(!row.eof()){row>>std::ws;if(!row.eof() && !(row>>e.enabled))throw std::runtime_error("Invalid study enabled setting.");}
            if(!row.eof()){row>>std::ws;if(!row.eof() && (!(row>>e.decimals)||e.decimals<0||e.decimals>10))throw std::runtime_error("Decimal places must be between 0 and 10.");}
            row>>std::ws;if(!row.eof() && (!(row>>e.order)||e.order<0))throw std::runtime_error("Invalid study order.");
            parsed[name]=e;
        } else if(tag=="FINANCIAL") {
            if(!(row>>std::quoted(config.study)>>config.aum>>config.multiplier>>config.commission>>config.quantity>>config.allocation>>config.reinvest)) throw std::runtime_error("Invalid financial settings settings.");
            row >> std::ws; if(!row.eof() && !(row >> config.showWinLoss)) throw std::runtime_error("Invalid W/L visibility setting.");
            validateFinancial(config);
        } else if(tag=="SIDE") {
            double side;SideAppearance style;
            if(!(row>>side>>style.color>>style.show) || !validSide(side) || side==0 || style.color<0 || style.color>0xffffff) throw std::runtime_error("Invalid SIDE settings entry.");
            config.sides[sideKey(side)]=style;
        } else if(tag=="BACKGROUND") {
            BackgroundRule rule;if(!(row>>std::quoted(rule.name)>>rule.enabled>>rule.color) || rule.color<0 || rule.color>0xffffff || backgrounds.size()>=128) throw std::runtime_error("Invalid background rule.");
            int nodes=0;rule.root=readBackgroundNode(row,0,&nodes);backgrounds.push_back(std::move(rule));
        } else throw std::runtime_error("Unknown study settings entry.");
    }
    for(const auto& [name,e]:parsed) savedStudies[name]=e;
    financialConfig=std::move(config);backgroundRules=std::move(backgrounds);
}
void readSettings(const std::filesystem::path& path) {std::ifstream in(path);readSettingsStream(in);}
void saveStudies() {
    for (size_t i=0;i<options.studies.size();++i) {auto& e=options.studies[i];e.order=static_cast<int>(i);savedStudies[data.studies[e.index].name] = e;}
    if (!testing && !restoringWorkspace) try { writeSettings(settingsPath()); }
    catch (const std::exception&) { SetWindowTextW(statusLabel, L"Study changes applied; settings could not be saved."); }
}

#include "LegendGrid.h"

