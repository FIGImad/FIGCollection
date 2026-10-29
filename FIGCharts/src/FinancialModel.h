#pragma once
#include "CsvData.h"
#include <algorithm>
#include <cmath>
#include <map>
#include <limits>
#include <cctype>
#include <stdexcept>
#include <sstream>
#include <iomanip>
namespace figcharts {
struct SideAppearance { int color = 0x37c978; bool show = true; };
struct FinancialConfig {
    std::string study;
    double aum = 1200000, multiplier = 20, commission = 1.6, quantity = 0, allocation = 1;
    bool reinvest = false, showWinLoss = true;
    std::map<double, SideAppearance> sides;
};
inline FinancialConfig financialConfig;
inline double sideKey(double v) { return std::round(v * 100000000.0) / 100000000.0; }
inline bool validSide(double v) { return std::isfinite(v) && std::abs(v) < 1e8; }
inline std::string sideName(double v) { std::ostringstream s; s << std::setprecision(10) << v; return s.str(); }
inline bool isSideStudy(std::string name) {
    std::transform(name.begin(), name.end(), name.begin(), [](unsigned char c) { return static_cast<char>(std::toupper(c)); });
    return name.ends_with("_SIDE");
}
inline void validateFinancial(const FinancialConfig& c) {
    if (!std::isfinite(c.aum) || c.aum <= 0 || !std::isfinite(c.multiplier) || c.multiplier <= 0 ||
        !std::isfinite(c.commission) || c.commission < 0 || !std::isfinite(c.quantity) || c.quantity < 0 || c.quantity != std::floor(c.quantity) ||
        !std::isfinite(c.allocation) || c.allocation <= 0 || c.allocation > 1 || c.quantity > 1e9)
        throw std::runtime_error("Use positive AUM/multiplier, nonnegative commission, whole quantity (0 = automatic), and allocation above 0 through 100%.");
}
struct FinancialBar {
    double side = 0, position = 0, closed = 0, open = 0, commission = 0, net = 0;
    double nav = 100, peak = 100, drawdown = 0, maxDrawdown = 0;
    double tradeNav = 100, tradePeak = 100, tradeDrawdown = 0, tradeMaxDrawdown = 0;
    double tradePrice = 0;
    size_t signalBars = 0, signalFrequency = 0;
};
struct SidePoint { size_t bar = 0, bars = 0, frequency = 0; double closed = 0, winningPL = 0, losingPL = 0; size_t wins = 0, losses = 0; };
struct ClosedSignal { size_t bar; double side, pnl; };
struct FinancialResult {
    int studyIndex = -1;
    size_t missing = 0;
    std::vector<FinancialBar> bars;
    std::vector<ClosedSignal> closedSignals;
    std::vector<std::pair<size_t,double>> signals;
    size_t signalFrom(size_t anchor, bool forward, const std::map<double,SideAppearance>& styles, const double* filter=nullptr, const std::string& prefix={}) const {
        auto enabled=[&](double side) {auto it=styles.find(side);return it!=styles.end() && it->second.show && (!filter || side==sideKey(*filter)) && (prefix.empty() || sideName(side).starts_with(prefix));};
        auto it=std::lower_bound(signals.begin(),signals.end(),anchor,[](const auto& p,size_t bar){return p.first<bar;});
        if(forward) {for(;it!=signals.end();++it) if(it->first>anchor && enabled(it->second)) return it->first;}
        else {while(it!=signals.begin()) {--it;if(enabled(it->second)) return it->first;}}
        return bars.size();
    }
    std::map<double, std::vector<SidePoint>> sides;
    SidePoint at(double side, size_t bar) const {
        auto found = sides.find(side); if (found == sides.end()) return {};
        const auto& points = found->second;
        auto it = std::upper_bound(points.begin(), points.end(), bar, [](size_t b, const SidePoint& p) { return b < p.bar; });
        return it == points.begin() ? SidePoint{} : *std::prev(it);
    }
};
inline FinancialResult calculateFinancial(const CsvData& data, FinancialConfig& config) {
    validateFinancial(config); FinancialResult result;
    for (size_t i=0; i<data.studies.size(); ++i) if (data.studies[i].name == config.study) result.studyIndex = static_cast<int>(i);
    if (result.studyIndex < 0) return result;
    const auto& values = data.studies[result.studyIndex].values;
    if (values.size() != data.time.size()) throw std::runtime_error("SIDE data length does not match prices.");
    const int palette[]{0x32cd64,0xffc400,0xff6980,0x54bdff,0xcd82ed,0xff963f,0x50dccc,0xe59bc7};
    for (double raw : values) if (validSide(raw) && sideKey(raw) != 0) {
        double side = sideKey(raw); result.sides.try_emplace(side);
        if (!config.sides.contains(side)) {
            // Stable across file order and changes in the set of available sides.
            auto key=static_cast<unsigned long long>(static_cast<long long>(std::llround(side*100000000.0)));
            key^=key>>33;key*=0xff51afd7ed558ccdULL;key^=key>>33;
            config.sides[side] = {palette[key%8],true};
        }

    }
    result.bars.reserve(values.size());
    FinancialBar state; double entry = 0, currentSide = 0;
    std::map<double, SidePoint> totals;
    auto dd = [&](double nav, double peak) { return config.reinvest ? (nav/peak-1)*100 : nav-peak; };
    for (size_t i=0; i<values.size(); ++i) {
        double side = currentSide; if (validSide(values[i])) side = sideKey(values[i]); else ++result.missing;
        double price = data.close[i]; bool closedTrade = false; state.tradePrice = 0;
        if (side != currentSide) {
            if (currentSide != 0 && state.position != 0) {
                double pnl = (price-entry)*state.position*config.multiplier;
                result.closedSignals.push_back({i,currentSide,pnl});
                state.closed += pnl; state.commission -= std::abs(state.position)*config.commission;
                auto& t = totals[currentSide]; t.bar=i; t.closed += pnl;
                if(pnl >= 0) {t.winningPL += pnl; ++t.wins;} else {t.losingPL += pnl; ++t.losses;}
                result.sides[currentSide].push_back(t);
                state.position=0; state.open=0; state.tradePrice=price; closedTrade=true;
            }
            if (side != 0) {
                result.signals.emplace_back(i,side);
                double capital = config.aum + (config.reinvest ? state.closed+state.commission : 0);
                double typical = (data.high[i]+data.low[i]+price)/3;
                double qty = config.quantity > 0 ? config.quantity : (typical > 0 && capital > 0 ? std::ceil(capital*config.allocation/config.multiplier/typical) : 0);
                if (!std::isfinite(qty) || qty > 1e9) throw std::runtime_error("Position size exceeds supported limits. Check the multiplier and AUM.");
                state.position = side > 0 ? qty : -qty; entry=price;
                state.commission -= qty*config.commission; if (qty > 0) state.tradePrice=price;
                ++totals[side].frequency; ++state.signalFrequency;
            }
            currentSide=side;
        }
        if (side != 0) { ++state.signalBars; auto& t=totals[side]; t.bar=i; ++t.bars; result.sides[side].push_back(t); }
        state.side=side; state.open=state.position*(price-entry)*config.multiplier;
        state.net=state.closed+state.open+state.commission; state.nav=100*(config.aum+state.net)/config.aum;
        state.peak=std::max(state.peak,state.nav); state.drawdown=dd(state.nav,state.peak); state.maxDrawdown=std::min(state.maxDrawdown,state.drawdown);
        if (closedTrade) { state.tradeNav=state.nav; state.tradePeak=std::max(state.tradePeak,state.tradeNav); state.tradeDrawdown=dd(state.tradeNav,state.tradePeak); state.tradeMaxDrawdown=std::min(state.tradeMaxDrawdown,state.tradeDrawdown); }
        result.bars.push_back(state);
    }
    return result;
}
inline void testFinancial() {
    CsvData d; d.time={0,1,2,3,4,5,6}; d.close={100,100,110,120,115,105,100}; d.open=d.high=d.low=d.close; d.volume.assign(7,1);
    d.studies.push_back({"Test_SIDE",{0,1.1,1.1,1.2,-1.1,0,1.1}});
    FinancialConfig c; c.study="Test_SIDE"; c.aum=1000;c.multiplier=2;c.commission=1;c.quantity=3;
    auto r=calculateFinancial(d,c);
    auto check=[](bool b) { if(!b) throw std::runtime_error("Financial calculation regression"); };
    {auto reordered=d;std::reverse(reordered.studies[0].values.begin(),reordered.studies[0].values.end());auto fresh=c;fresh.sides.clear();calculateFinancial(reordered,fresh);
        for(const auto& [side,style]:c.sides)check(fresh.sides.at(side).color==style.color);
        fresh.sides[-4.9982]={0x123456,true};calculateFinancial(d,fresh);check(fresh.sides.at(-4.9982).color==0x123456);}
    d.studies[0].name="MarketTrend";c.study="MarketTrend";auto arbitrary=calculateFinancial(d,c);check(arbitrary.studyIndex==0 && arbitrary.bars.back().net==r.bars.back().net);
    check(r.closedSignals.size()==3 && r.closedSignals[0].bar==3 && r.closedSignals[0].side==1.1 && r.closedSignals[0].pnl==120 && r.closedSignals[1].pnl==-30 && r.closedSignals[2].side==-1.1);
    for(size_t bar=0;bar<r.bars.size();++bar){size_t bars=0,frequency=0;double closed=0;for(const auto& [side,points]:r.sides){auto t=r.at(side,bar);bars+=t.bars;frequency+=t.frequency;closed+=t.closed;}check(r.bars[bar].signalBars==bars && r.bars[bar].signalFrequency==frequency && r.bars[bar].closed==closed);}
    check(r.bars[1].position==3 && r.bars[1].open==0 && r.bars[1].net==-3);
    check(r.bars[2].open==60 && r.bars[2].closed==0);
    check(r.bars[3].closed==120 && r.bars[3].position==3 && r.bars[3].commission==-9);
    check(r.bars[4].closed==90 && r.bars[4].position==-3);
    check(r.bars[5].closed==150 && r.bars[5].open==0 && r.bars[5].net==132 && r.bars[5].commission==-18);
    check(r.at(1.1,2).bars==2 && r.at(1.1,2).frequency==1 && r.at(1.1,2).closed==0);
    check(r.at(1.1,5).closed==120 && r.at(1.2,5).closed==-30 && r.at(-1.1,5).closed==60);
    check(r.bars.back().position==3 && r.bars.back().closed==150); // Do not force-close final signal.
    check(std::abs(r.bars[5].nav-113.2)<1e-9 && r.bars[4].drawdown<0);
    check(r.signalFrom(1,true,c.sides)==3 && r.signalFrom(4,false,c.sides)==3);
    check(r.signalFrom(0,true,c.sides,nullptr,"1")==1 && r.signalFrom(6,false,c.sides,nullptr,"1")==3);
    check(sideName(4.9612).starts_with("4.96") && sideName(41.2).starts_with("4") && !sideName(-4.96).starts_with("4"));
    double filter=1.1;check(r.signalFrom(1,true,c.sides,&filter)==6 && r.signalFrom(6,false,c.sides,&filter)==1);
    filter=-1.1;check(r.signalFrom(0,true,c.sides,&filter)==4 && r.signalFrom(6,false,c.sides,&filter)==4);
    filter=-7.7649;check(r.signalFrom(0,true,c.sides,&filter)==r.bars.size());
    c.sides[1.2].show=false;check(r.signalFrom(1,true,c.sides)==4 && r.signalFrom(4,false,c.sides)==1);
    check(r.signalFrom(0,false,c.sides)==r.bars.size() && r.signalFrom(6,true,c.sides)==r.bars.size());
    c.sides[1.1].show=false; auto hidden=calculateFinancial(d,c); check(hidden.bars[5].net==r.bars[5].net);
    check(r.at(1.1,2).wins==0 && r.at(1.1,5).wins==1 && r.at(1.1,5).winningPL==120);
    check(r.at(1.2,5).losses==1 && r.at(1.2,5).losingPL==-30 && r.at(-1.1,5).wins==1);
    CsvData even=d;even.close[2]=even.close[1];even.studies[0].values={0,1.1,0,0,0,0,0};
    auto breakeven=calculateFinancial(even,c);check(breakeven.closedSignals.size()==1 && breakeven.closedSignals[0].pnl==0);check(breakeven.at(1.1,2).wins==1 && breakeven.at(1.1,2).winningPL==0 && breakeven.at(1.1,2).losses==0);
    c.quantity=0;c.commission=0;c.multiplier=1;auto automatic=calculateFinancial(d,c);check(automatic.bars[1].position==10);
    d.studies[0].values[2]=std::numeric_limits<double>::quiet_NaN();auto missing=calculateFinancial(d,c);check(missing.missing==1 && missing.bars[2].position==10);
}
}
