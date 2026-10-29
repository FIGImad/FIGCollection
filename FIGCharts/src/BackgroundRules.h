#pragma once
#include "CsvData.h"
#include <algorithm>
#include <cmath>
#include <iomanip>
#include <map>
#include <stdexcept>
#include <sstream>
namespace figcharts {
// group: -1 comparison, 0 AND, 1 OR, 2 NOT (negates the AND of its children).
struct BackgroundNode { int group=0, op=0; std::string study; double value=0; std::vector<BackgroundNode> children; std::string valueStudy; };
struct BackgroundRule { std::string name; bool enabled=true; int color=0x173b29; BackgroundNode root; };
inline std::vector<BackgroundRule> backgroundRules;
inline std::vector<int> backgroundCache;
inline const wchar_t* backgroundOps[]{L">",L"<",L"=",L">=",L"<=",L"!="};
inline void writeBackgroundNode(std::ostream& out,const BackgroundNode& node) {
    out << (node.group==-1 && !node.valueStudy.empty()?-2:node.group) << ' ' << node.op << ' ' << std::quoted(node.study) << ' ' << std::setprecision(17) << node.value << ' ' << node.children.size() << ' ';
    if(node.group==-1 && !node.valueStudy.empty())out<<std::quoted(node.valueStudy)<<' ';
    for(const auto& child:node.children) writeBackgroundNode(out,child);
}
inline BackgroundNode readBackgroundNode(std::istream& in,int depth=0,int* total=nullptr) {
    int local=0;if(!total)total=&local;if(depth>16 || ++*total>512) throw std::runtime_error("Background condition tree is too large.");
    BackgroundNode node;size_t count=0;
    if(!(in>>node.group>>node.op>>std::quoted(node.study)>>node.value>>count) || node.group< -2 || node.group>2 || node.op<0 || node.op>5 || !std::isfinite(node.value) || count>512 || (node.group<0 && count)) throw std::runtime_error("Invalid background condition.");
    if(node.group==-2){node.group=-1;if(!(in>>std::quoted(node.valueStudy)) || node.valueStudy.empty())throw std::runtime_error("Invalid comparison study.");}
    for(size_t i=0;i<count;++i) node.children.push_back(readBackgroundNode(in,depth+1,total));return node;
}
struct CompiledBackgroundNode { int group=0,op=0,index=-1,valueIndex=-1;double value=0;bool valid=true;std::vector<CompiledBackgroundNode> children; };
inline CompiledBackgroundNode compileBackground(const BackgroundNode& node,const CsvData& data) {
    CompiledBackgroundNode result;result.group=node.group;result.op=node.op;result.value=node.value;
    if(node.group==-1) {for(size_t i=0;i<data.studies.size();++i) if(data.studies[i].name==node.study) result.index=static_cast<int>(i);result.valid=result.index>=0; if(!node.valueStudy.empty()){for(size_t i=0;i<data.studies.size();++i)if(data.studies[i].name==node.valueStudy)result.valueIndex=static_cast<int>(i);result.valid=result.valid && result.valueIndex>=0;}}
    else {result.valid=!node.children.empty();for(const auto& child:node.children) {auto c=compileBackground(child,data);result.valid=result.valid&&c.valid;result.children.push_back(std::move(c));}}
    return result;
}
// -1 means missing/invalid data. It never becomes true through NOT.
inline int evaluateBackground(const CompiledBackgroundNode& node,const CsvData& data,size_t bar) {
    if(!node.valid)return -1;
    if(node.group==-1) {
        const auto& v=data.studies[node.index].values;if(bar>=v.size() || !std::isfinite(v[bar]) || std::abs(v[bar])>=1e300)return -1;
        double a=v[bar],b=node.value;if(node.valueIndex>=0){const auto& rhs=data.studies[node.valueIndex].values;if(bar>=rhs.size() || !std::isfinite(rhs[bar]) || std::abs(rhs[bar])>=1e300)return -1;b=rhs[bar];}bool equal=std::abs(a-b)<=1e-9*std::max({1.0,std::abs(a),std::abs(b)});
        switch(node.op) {case 0:return a>b;case 1:return a<b;case 2:return equal;case 3:return a>b||equal;case 4:return a<b||equal;case 5:return !equal;}return -1;
    }
    bool any=false,all=true;for(const auto& c:node.children) {int result=evaluateBackground(c,data,bar);if(result<0)return -1;any=any||result;all=all&&result;}
    return node.group==0?all:node.group==1?any:!all;
}
inline std::vector<int> calculateBackground(const CsvData& data,const std::vector<BackgroundRule>& rules,std::vector<size_t>* matches=nullptr) {
    std::vector<CompiledBackgroundNode> compiled;for(const auto& rule:rules)compiled.push_back(compileBackground(rule.root,data));
    std::vector<int> colors(data.time.size(),-1);if(matches)matches->assign(rules.size(),0);
    for(size_t bar=0;bar<data.time.size();++bar) for(size_t i=0;i<rules.size();++i) if(rules[i].enabled && evaluateBackground(compiled[i],data,bar)==1) {
        if(matches)++(*matches)[i];if(colors[bar]<0) colors[bar]=rules[i].color;
        if(!matches)break;
    }
    return colors;
}
inline void testBackground() {
    CsvData d;d.time={0,1,2,3};d.studies={{"Trend",{-1,0,1,1.7e308}},{"ADX",{30,20,40,50}}};
    BackgroundNode trend;trend.group=-1;trend.study="Trend";trend.op=0;
    BackgroundNode adx;adx.group=-1;adx.study="ADX";adx.op=0;adx.value=25;
    BackgroundRule r;r.name="Up";r.root.children={trend,adx};r.color=0x123456;
    auto check=[](bool ok){if(!ok)throw std::runtime_error("Background rule regression");};
    auto c=calculateBackground(d,{r});check(c[0]==-1&&c[1]==-1&&c[2]==r.color&&c[3]==-1);
    r.root.group=1;c=calculateBackground(d,{r});check(c[0]==r.color&&c[1]==-1&&c[2]==r.color&&c[3]==-1);
    r.root.group=2;c=calculateBackground(d,{r});check(c[0]==r.color&&c[1]==r.color&&c[2]==-1&&c[3]==-1);
    r.root.children[0].study="Missing";c=calculateBackground(d,{r});check(c[0]==-1&&c[2]==-1);
    r.root.children={trend};r.root.group=0;auto second=r;second.color=0x999999;c=calculateBackground(d,{r,second});check(c[2]==r.color);
    r.enabled=false;c=calculateBackground(d,{r,second});check(c[2]==second.color);
    for(int op=0;op<6;++op) {trend.op=op;auto node=compileBackground(trend,d);int expected[]{0,0,1,1,1,0};check(evaluateBackground(node,d,1)==expected[op]);}
    trend.valueStudy="ADX";trend.op=1;check(evaluateBackground(compileBackground(trend,d),d,0)==1);check(evaluateBackground(compileBackground(trend,d),d,3)==-1);
    std::stringstream comparison;writeBackgroundNode(comparison,trend);check(readBackgroundNode(comparison).valueStudy=="ADX");
    trend.valueStudy="missing";check(evaluateBackground(compileBackground(trend,d),d,0)==-1);
    std::stringstream serialized;writeBackgroundNode(serialized,r.root);auto restored=readBackgroundNode(serialized);check(restored.children[0].study=="Trend");
}
}
