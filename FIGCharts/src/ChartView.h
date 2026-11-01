#pragma once
#include "CsvData.h"
#include "FinancialModel.h"
#include "BackgroundRules.h"
#include "AppPreferences.h"
#include <FinanceChart.h>
#include <memory>
namespace figcharts {
enum class ChartType { OHLC, Candle, Line };
struct StudyEntry {
    int index = -1;
    bool overlay = false;
    int color = 0x2768d7, pattern = 0, width = 1, type = 0;
    bool enabled = true;
    int decimals = 2;
    int order = 2147483647;
};
struct ChartOptions {
    size_t first = 0, count = 200;
    std::vector<StudyEntry> studies;
    int activeStudy = -1;
    ChartType type = ChartType::OHLC;
    bool volume = false;
    bool autoScale=true, priceOnly=true;
    double scaleMin=0, scaleMax=1;
    double panExtra=0;
    int signalStudy = -1;
    std::vector<ClosedSignal> signalLabels;
    std::vector<int> backgroundColors;
    std::map<double, SideAppearance> signalStyles;
};
inline size_t displayStride(size_t count, int width) {
    size_t limit = static_cast<size_t>(std::max(300, width * 2));
    return std::max(size_t(1), (count + limit - 1) / limit);
}
inline std::unique_ptr<FinanceChart> render(const CsvData& data, const ChartOptions& options, int width, int height, std::vector<XYChart*>* panelsOut = nullptr) {
    const size_t first = options.first, count = std::min(options.count, data.time.size() - first);
    size_t stride = displayStride(count, width);
    if (stride > 1) {
        CsvData compact; ChartOptions view = options; view.first = 0; view.backgroundColors.clear();
        // Keep OHLC extrema and summed volume; studies use the last source value in each bucket.
        for (auto& entry : view.studies) {
            int original = entry.index; entry.index = static_cast<int>(compact.studies.size());
            compact.studies.push_back({data.studies[original].name, {}});
        }
        if(options.signalStudy >= 0) { view.signalStudy=static_cast<int>(compact.studies.size());compact.studies.push_back({data.studies[options.signalStudy].name,{}}); }
        for (size_t start = first; start < first + count; start += stride) {
            size_t end = std::min(start + stride, first + count), last = end - 1;
            if(last-first<options.backgroundColors.size()) view.backgroundColors.push_back(options.backgroundColors[last-first]);
            double hi = data.high[start], lo = data.low[start], vol = 0;
            for (size_t j = start; j < end; ++j) { hi = std::max(hi, data.high[j]); lo = std::min(lo, data.low[j]); vol += data.volume[j]; }
            if(options.signalStudy >= 0) compact.studies[view.signalStudy].values.push_back(data.studies[options.signalStudy].values[last]);
            compact.time.push_back(data.time[last]); compact.open.push_back(data.open[start]); compact.high.push_back(hi);
            compact.low.push_back(lo); compact.close.push_back(data.close[last]); compact.volume.push_back(vol);
            for (size_t j = 0; j < options.studies.size(); ++j) compact.studies[j].values.push_back(data.studies[options.studies[j].index].values[last]);
        }
        for(auto& mark:view.signalLabels) mark.bar=(mark.bar-first)/stride;
        view.panExtra/=stride;view.count = compact.time.size(); return render(compact, view, width, height, panelsOut);
    }
    std::vector<double> times(data.time.begin() + first, data.time.begin() + first + count);
    for (auto& t : times) t = systemSeconds(displaySystemTime(t)) + Chart::chartTime(1970, 1, 1);
    auto array = [&](const std::vector<double>& v) { return DoubleArray(v.data() + first, static_cast<int>(count)); };
    auto chart = std::make_unique<FinanceChart>(width);
    if (panelsOut) panelsOut->clear();
        int background = preferences.dark ? 0x18202b : 0xffffff;
    int foreground = preferences.dark ? 0xdce4ef : 0x263244;
    std::vector<XYChart*> renderedPanels;
    auto panel = [&](XYChart* p) {
        p->setClipping();renderedPanels.push_back(p);
        p->getPlotArea()->setGridWidth(1, 1);
        p->setBackground(Chart::Transparent); p->yAxis()->setLabelFormat("{value|2}");
        p->yAxis2()->setLabelFormat("{value|2}");
        if (panelsOut) panelsOut->push_back(p); return p;
    };
    chart->setBackground(background);
    chart->setXAxisStyle("Arial", 8, foreground, 0);
    chart->setYAxisStyle("Arial", 8, foreground, 13);
    chart->setNumberLabelFormat("{value|2}");
    if (!preferences.title.empty()) chart->addTitle(preferences.title.c_str(), "Arial Bold", 12, foreground);
    chart->setLegendStyle("Arial", 8, Chart::Transparent, Chart::Transparent);
    chart->setMargins(65, 32, 70, 35);
    // Keep plotting surfaces dark in both themes; surrounding labels follow the theme.
    int plotBackground = 0x18202b;
    int grid = preferences.dark ? 0x34383e : 0x191b1f;
    chart->setPlotAreaStyle(plotBackground, grid, grid, plotBackground, plotBackground);
    // During overscroll we own the time-axis labels. Initialize without timestamps
    // so FinanceChart does not install automatic date labels on the first panel.
    chart->setData(options.panExtra!=0?DoubleArray():DoubleArray(times.data(), static_cast<int>(count)), array(data.high), array(data.low), array(data.open), array(data.close), array(data.volume), 0);
    bool hasPanel = options.activeStudy >= 0 && options.activeStudy < static_cast<int>(options.studies.size()) && options.studies[options.activeStudy].enabled && !options.studies[options.activeStudy].overlay;
    int panelHeight = std::max(100, std::min(180, height / 4));
    auto main = panel(chart->addMainChart(std::max(140, height - 100 - (hasPanel ? panelHeight + 10 : 0))));
    for(size_t j=0;j<std::min(count,options.backgroundColors.size());) {
        size_t end=j+1;int color=options.backgroundColors[j];while(end<count && end<options.backgroundColors.size() && options.backgroundColors[end]==color) ++end;
        if(color>=0) main->xAxis()->addZone(static_cast<double>(j)-0.5,static_cast<double>(end)-0.5,color);j=end;
    }
    if (options.signalStudy >= 0 && options.signalStudy < static_cast<int>(data.studies.size())) {
        std::map<int,std::vector<size_t>> groups;
        const auto& sides=data.studies[options.signalStudy].values;
        for(size_t j=0;j<count;++j) { int color=0xffffff;double side=validSide(sides[first+j])?sideKey(sides[first+j]):0;
            auto found=options.signalStyles.find(side);if(side!=0 && found!=options.signalStyles.end() && found->second.show) color=found->second.color;
            groups[color].push_back(j);
        }
        for(const auto& [color,indices]:groups) {
            std::vector<double> hi(count,Chart::NoValue),lo=hi,op=hi,cl=hi;
            for(size_t j:indices) {hi[j]=data.high[first+j];lo[j]=data.low[first+j];op[j]=data.open[first+j];cl[j]=data.close[first+j];}
            auto arr=[](const auto& v) {return DoubleArray(v.data(),static_cast<int>(v.size()));};
            if(options.type==ChartType::Candle) {auto layer=main->addCandleStickLayer(arr(hi),arr(lo),arr(op),arr(cl),color,color,color);layer->setDataGap(0);}
            else if(options.type==ChartType::OHLC) main->addHLOCLayer(arr(hi),arr(lo),arr(op),arr(cl),color)->setDataGap(0);
            else {
                // Color each close-to-close segment by its ending bar's signal.
                std::vector<double> xs,ys;
                for(size_t j:indices) if(j>0) { xs.insert(xs.end(),{static_cast<double>(j-1),static_cast<double>(j),static_cast<double>(j)});ys.insert(ys.end(),{data.close[first+j-1],data.close[first+j],Chart::NoValue}); }
                auto layer=main->addLineLayer(arr(ys),color);layer->setXData(arr(xs));layer->setLineWidth(1);
            }
        }
    }
    else if (options.type == ChartType::Candle) chart->addCandleStick(0x199477, 0xd65360);
    else if (options.type == ChartType::Line) chart->addCloseLine(0x2768d7);
    else chart->addHLOC(0x199477, 0xd65360);
    if (options.volume) chart->addVolBars(55, 0x99d6c5, 0xefb4ba, 0xb8c2cf);
    for (size_t i = 0; i < options.studies.size(); ++i) {
        const auto& entry = options.studies[i];
        if(!entry.enabled)continue;
        if (!entry.overlay && static_cast<int>(i) != options.activeStudy) continue;
        const auto& study = data.studies.at(entry.index);
        auto target = entry.overlay ? main : panel(chart->addIndicator(panelHeight));
        int patterns[] = {0, Chart::DashLine, Chart::DotLine, Chart::DotDashLine};
        int color = entry.pattern ? target->dashLineColor(entry.color, patterns[entry.pattern]) : entry.color;
        if(entry.type==0) chart->addLineIndicator2(target, array(study.values), color, study.name.c_str())->setLineWidth((entry.width * 4 + 1) / 3);
        else {
            std::vector<double> xs,ys;
            for(size_t j=0;j<count;++j){double v=study.values[first+j];if(!std::isfinite(v)||std::abs(v)>=1e300)continue;
                if(entry.type==1){xs.insert(xs.end(),{j-0.4,j+0.4,j+0.4});ys.insert(ys.end(),{v,v,Chart::NoValue});}
                else {xs.push_back(static_cast<double>(j));ys.push_back(v);}}
            auto x=DoubleArray(xs.data(),static_cast<int>(xs.size())),y=DoubleArray(ys.data(),static_cast<int>(ys.size()));
            if(entry.type==1){auto layer=target->addLineLayer(y,color);layer->setXData(x);layer->setLineWidth((entry.width*4+1)/3);}
            else target->addScatterLayer(x,y,"",Chart::CircleSymbol,std::max(3,entry.width*2+1),entry.color,entry.color);
        }
        if(entry.overlay && static_cast<int>(i)==options.activeStudy) {
            std::vector<double> xs,ys;
            size_t step=std::max(size_t(1),static_cast<size_t>(std::ceil(count*48.0/std::max(1,width-135))));
            for(size_t j=0;j<count;j+=step){double value=study.values[first+j];if(std::isfinite(value) && std::abs(value)<1e300){xs.push_back(static_cast<double>(j));ys.push_back(value);}}
            target->addScatterLayer(DoubleArray(xs.data(),static_cast<int>(xs.size())),DoubleArray(ys.data(),static_cast<int>(ys.size())),"",Chart::SquareSymbol,6,entry.color,0xffffff);
        }
        if (!entry.overlay) target->addText(70, 34, study.name.c_str(), "Arial", 9, entry.color);
    }
    if(!options.autoScale) {main->yAxis()->setLinearScale(options.scaleMin,options.scaleMax);main->yAxis()->setRounding(false,false);}
    else if(options.priceOnly) {
        double low=std::numeric_limits<double>::infinity(),high=-low;
        for(size_t j=first;j<first+count;++j){if(std::isfinite(data.low[j])&&std::abs(data.low[j])<1e300)low=std::min(low,data.low[j]);if(std::isfinite(data.high[j])&&std::abs(data.high[j])<1e300)high=std::max(high,data.high[j]);}
        if(std::isfinite(low)&&std::isfinite(high)){double pad=std::max((high-low)*0.05,std::max(0.01,std::abs(high)*1e-6));main->yAxis()->setLinearScale(low-pad,high+pad);main->yAxis()->setRounding(false,false);}
    }
    // Set every panel's range before any layout (signal-label positioning lays out main).
    if(options.panExtra!=0){
        int plotWidth=std::max(1,width-135);
        size_t step=std::max(size_t(1),static_cast<size_t>(std::ceil(count*110.0/plotWidth)));
        double edgeMargin=count*55.0/plotWidth;
        size_t begin=static_cast<size_t>(std::max(0.0,std::ceil(options.panExtra-0.5+edgeMargin)));
        double end=std::min(double(count-1),count-0.5+options.panExtra-edgeMargin);
        for(auto p:renderedPanels){
            auto axis=p->xAxis();axis->copyAxis(nullptr);
            axis->setLinearScale(options.panExtra-0.5,count-0.5+options.panExtra,Chart::NoValue);
            axis->setRounding(false,false);
            for(size_t i=begin;i<count&&double(i)<=end;i+=step){auto t=displaySystemTime(data.time[first+i]);char label[48];sprintf_s(label,"%04u-%02u-%02u\n%02u:%02u",t.wYear,t.wMonth,t.wDay,t.wHour,t.wMinute);axis->addLabel(static_cast<double>(i),label);}
        }
        // Retain real timestamps for FinanceChart metadata after all axes exist.
        chart->setData(DoubleArray(times.data(),static_cast<int>(count)),array(data.high),array(data.low),array(data.open),array(data.close),array(data.volume),0);
    }
    if(options.signalStudy>=0 && !options.signalLabels.empty()) {
        main->layout();auto area=main->getPlotArea();int lastX=-1000,lane=0;
        for(const auto& mark:options.signalLabels) {
            if(mark.bar<first || mark.bar>=first+count) continue;
            auto style=options.signalStyles.find(mark.side);if(style==options.signalStyles.end() || !style->second.show) continue;
            int x=main->getXCoor(static_cast<double>(mark.bar-first));
            if(x<area->getLeftX() || x>area->getLeftX()+area->getWidth())continue;
            lane=x-lastX<55?(lane+1)%3:0;lastX=x;
            std::string label=(mark.pnl>=0?"W ":"L ")+sideName(mark.side);
            int labelX=std::clamp(x,area->getLeftX()+24,area->getLeftX()+area->getWidth()-24);
            main->addText(labelX,area->getTopY()+5+lane*15,label.c_str(),"Arial Bold",9,mark.pnl>=0?0x90ee90:0xff4545,Chart::TopCenter);
        }
    }
    return chart;
}
}
