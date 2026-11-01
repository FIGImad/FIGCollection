#pragma once
#include <variant>
#include <array>
#include <unordered_map>
#include <bit>
#include <cstdint>
#include <limits>
#include <string_view>
#include <unordered_set>
namespace figcharts {
struct StudyDecimal { uint32_t low,middle,high,flags; };
using StudyValue=std::variant<std::monostate,double,StudyDecimal,int64_t,bool,std::string,int32_t,float>;
using StudyValues=std::unordered_map<std::string,StudyValue>;
struct FigsReader {
 const std::vector<unsigned char>& bytes; size_t pos=0;
 uint64_t number(size_t n){if(n>bytes.size()-pos)throw std::runtime_error("Truncated FIGS payload.");uint64_t v=0;for(size_t i=0;i<n;++i)v|=uint64_t(bytes[pos++])<<(8*i);return v;}
 std::string_view textView(){auto n=static_cast<int32_t>(number(4));if(n<0||size_t(n)>bytes.size()-pos)throw std::runtime_error("Invalid FIGS string length.");std::string_view s(reinterpret_cast<const char*>(bytes.data()+pos),n);pos+=n;return s;}
 static void validateText(std::string_view s){
  // Most study names are ASCII. Avoid a Windows conversion call for each name.
  bool ascii=true;for(unsigned char c:s)if(c>=128){ascii=false;break;}
  if(!ascii&&!MultiByteToWideChar(CP_UTF8,MB_ERR_INVALID_CHARS,s.data(),static_cast<int>(s.size()),nullptr,0))throw std::runtime_error("Invalid FIGS UTF-8.");
 }
 std::string text(){auto s=textView();validateText(s);return std::string(s);}

};
inline StudyValue readStudyValue(FigsReader& r){
 StudyValue value;switch(r.number(1)){
 case 0:break;case 1:value=std::bit_cast<double>(r.number(8));break;
 case 2:{StudyDecimal d{uint32_t(r.number(4)),uint32_t(r.number(4)),uint32_t(r.number(4)),uint32_t(r.number(4))};if((d.flags&0x7f00ffff)||((d.flags>>16)&255)>28)throw std::runtime_error("Invalid FIGS decimal.");value=d;break;}
 case 3:value=std::bit_cast<int64_t>(r.number(8));break;case 4:{auto b=r.number(1);if(b>1)throw std::runtime_error("Invalid FIGS Boolean.");value=bool(b);break;}
 case 5:value=r.text();break;case 6:value=std::bit_cast<int32_t>(uint32_t(r.number(4)));break;case 7:value=std::bit_cast<float>(uint32_t(r.number(4)));break;default:throw std::runtime_error("Unknown FIGS tag.");}
 return value;
}
inline StudyValues decodeFIGS(const std::vector<unsigned char>& bytes){
 if(bytes.size()<10||bytes.size()>16*1024*1024)throw std::runtime_error("Invalid FIGS payload size.");
 FigsReader r{bytes};if(r.number(4)!=0x53474946||r.number(2)!=1)throw std::runtime_error("Invalid FIGS magic/version.");auto count=r.number(4);if(count>100000)throw std::runtime_error("Invalid FIGS entry count.");StudyValues values;values.reserve(static_cast<size_t>(std::min<uint64_t>(count,(bytes.size()-10)/5)));
 for(uint64_t i=0;i<count;++i){auto name=r.text();StudyValue v;v=readStudyValue(r);
 if(!values.emplace(std::move(name),std::move(v)).second)throw std::runtime_error("Duplicate FIGS name.");}
 if(r.pos!=bytes.size())throw std::runtime_error("Trailing FIGS bytes.");return values;
}
inline double studyNumber(const StudyValue& v){return std::visit([](const auto& x)->double{using T=std::decay_t<decltype(x)>;if constexpr(std::is_same_v<T,StudyDecimal>){double n=double(x.low)+double(x.middle)*4294967296.0+double(x.high)*18446744073709551616.0;return (x.flags&0x80000000?-n:n)/[] (uint32_t scale){static const auto powers=[] {std::array<double,29> p{};p[0]=1;for(size_t i=1;i<p.size();++i)p[i]=p[i-1]*10;return p;}();return powers[scale];}((x.flags>>16)&255);}else if constexpr(std::is_arithmetic_v<T>)return double(x);else return std::numeric_limits<double>::quiet_NaN();},v);}
// Reuses the ordered study schema and storage across bars. The general decoder
// above remains available for callers that need exact typed dictionaries.
struct NumericStudyEntry {
 std::string name;
 double value=std::numeric_limits<double>::quiet_NaN();
 size_t column=SIZE_MAX;
};
class NumericStudyDecoder {
 std::vector<NumericStudyEntry> entries;
public:
 const std::vector<NumericStudyEntry>& decode(const std::vector<unsigned char>& bytes){
  if(bytes.size()<10||bytes.size()>16*1024*1024)throw std::runtime_error("Invalid FIGS payload size.");
  FigsReader r{bytes};if(r.number(4)!=0x53474946||r.number(2)!=1)throw std::runtime_error("Invalid FIGS magic/version.");
  auto count=r.number(4);if(count>100000||count>(bytes.size()-10)/5)throw std::runtime_error("Invalid FIGS entry count.");
  bool changed=entries.size()!=count;entries.resize(static_cast<size_t>(count));
  for(auto& entry:entries){auto name=r.textView();if(entry.column==SIZE_MAX||name!=entry.name){FigsReader::validateText(name);entry.name.assign(name);entry.column=SIZE_MAX;changed=true;}entry.value=studyNumber(readStudyValue(r));}
  if(r.pos!=bytes.size())throw std::runtime_error("Trailing FIGS bytes.");
  if(changed){std::unordered_set<std::string_view> names;names.reserve(entries.size());for(auto& entry:entries)if(!names.emplace(entry.name).second)throw std::runtime_error("Duplicate FIGS name.");}
  return entries;
 }
 void appendTo(CsvData& out,std::unordered_map<std::string,size_t>& columns,size_t row){
  for(auto& entry:entries){if(entry.column==SIZE_MAX){auto found=columns.find(entry.name);if(found==columns.end()){entry.column=out.studies.size();columns.emplace(entry.name,entry.column);out.studies.push_back({entry.name,std::vector<double>(row+1,NAN)});}else entry.column=found->second;}out.studies[entry.column].values[row]=entry.value;}
 }
};

}
