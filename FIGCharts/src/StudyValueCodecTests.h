#pragma once
namespace figcharts {
inline void testStudyCodec(){
 if(databaseHistoryQuery(10000).find(L"TOP (10000)")==std::wstring::npos||databaseHistoryQuery(10000).find(L"ORDER BY RawTime DESC")==std::wstring::npos||databaseHistoryQuery(0).find(L"TOP")!=std::wstring::npos)throw std::runtime_error("Database history query regression.");
 if(databaseHistoryQuery(0).find(L"Volume,Studies,StudiesOverride FROM")==std::wstring::npos)throw std::runtime_error("Database query must fetch base studies and overrides.");
 using Bytes=std::vector<unsigned char>;auto require=[](bool ok){if(!ok)throw std::runtime_error("FIGS regression failed.");};
 Bytes empty{'F','I','G','S',1,0,0,0,0,0};require(decodeFIGS(empty).empty());
 auto word=[](Bytes& b,uint64_t v,int n){for(int i=0;i<n;++i)b.push_back(static_cast<unsigned char>(v>>(8*i)));};
 auto entry=[&](Bytes& b,const std::string& name,int tag){word(b,name.size(),4);b.insert(b.end(),name.begin(),name.end());b.push_back(static_cast<unsigned char>(tag));};
 auto bad=[&](const Bytes& b){bool rejected=false;try{decodeFIGS(b);}catch(const std::exception&){rejected=true;}require(rejected);rejected=false;try{NumericStudyDecoder decoder;decoder.decode(b);}catch(const std::exception&){rejected=true;}require(rejected);};
 Bytes all=empty;all[6]=8;
 entry(all,"null",0);entry(all,"double",1);word(all,std::bit_cast<uint64_t>(1.25),8);
 entry(all,"decimal",2);word(all,125,4);word(all,0,4);word(all,0,4);word(all,0x80020000,4);
 entry(all,"int64",3);word(all,uint64_t(-42LL),8);entry(all,"bool",4);word(all,1,1);
 entry(all,"string",5);word(all,3,4);all.insert(all.end(),{'a',0,'b'});
 entry(all,"int32",6);word(all,uint32_t(-7),4);entry(all,"single",7);word(all,std::bit_cast<uint32_t>(2.5f),4);
 auto values=decodeFIGS(all);require(values.size()==8&&studyNumber(values.at("decimal"))==-1.25&&std::get<std::string>(values.at("string")).size()==3&&std::get<int64_t>(values.at("int64"))==-42);
 NumericStudyDecoder fast;CsvData numeric;std::unordered_map<std::string,size_t> columns;
 fast.decode(all);fast.appendTo(numeric,columns,0);
 for(auto& series:numeric.studies){double expected=studyNumber(values.at(series.name));require(series.values[0]==expected||(std::isnan(series.values[0])&&std::isnan(expected)));}
 // Same schema, then a reordered/changed schema, with missing values and null overrides.
 for(auto& series:numeric.studies)series.values.push_back(NAN);fast.decode(all);fast.appendTo(numeric,columns,1);
 Bytes changed=empty;changed[6]=2;entry(changed,"single",0);entry(changed,"new",6);word(changed,77,4);
 for(auto& series:numeric.studies)series.values.push_back(NAN);fast.decode(changed);fast.appendTo(numeric,columns,2);
 require(std::isnan(numeric.studies[columns.at("single")].values[2])&&numeric.studies[columns.at("new")].values[2]==77&&std::isnan(numeric.studies[columns.at("new")].values[0]));
 NumericStudyDecoder overrideDecoder;overrideDecoder.decode(changed);overrideDecoder.appendTo(numeric,columns,1);require(std::isnan(numeric.studies[columns.at("single")].values[1]));
 auto overrides=empty;overrides[6]=1;entry(overrides,"double",0);for(auto& [name,v]:decodeFIGS(overrides))values.insert_or_assign(name,v);require(std::holds_alternative<std::monostate>(values.at("double")));
 for(size_t i=0;i<all.size();++i)bad(Bytes(all.begin(),all.begin()+i));
 auto b=empty;b[0]=0;bad(b);b=empty;b[4]=2;bad(b);b=empty;b.push_back(0);bad(b);b=empty;b[6]=0xff;b[7]=0xff;b[8]=0xff;b[9]=0xff;bad(b);
 b=empty;b[6]=2;entry(b,"x",0);entry(b,"x",0);bad(b);
 b=empty;b[6]=1;entry(b,std::string("\xc0\xaf",2),0);bad(b);
 b=empty;b[6]=1;entry(b,"x",99);bad(b);b=empty;b[6]=1;entry(b,"x",4);b.push_back(2);bad(b);
 b=empty;b[6]=1;entry(b,"x",2);word(b,0,8);word(b,0,4);word(b,1,4);bad(b);
 b=empty;b[6]=1;entry(b,"x",2);word(b,0,8);word(b,0,4);word(b,29<<16,4);bad(b);
 bad(Bytes(16*1024*1024+1,0));
}
inline void benchmarkStudyDecode(const std::filesystem::path& output){
 std::vector<unsigned char> payload{'F','I','G','S',1,0,64,0,0,0};
 auto word=[&](uint64_t v,int n){for(int i=0;i<n;++i)payload.push_back(static_cast<unsigned char>(v>>(8*i)));};
 for(int i=0;i<64;++i){auto name="Study_Collection_Output_"+std::to_string(i);word(name.size(),4);payload.insert(payload.end(),name.begin(),name.end());payload.push_back(1);word(std::bit_cast<uint64_t>(i+0.25),8);}
 auto run=[&](bool fast){
  CsvData data;std::unordered_map<std::string,size_t> columns;NumericStudyDecoder decoder;
  auto start=std::chrono::steady_clock::now();
  for(size_t row=0;row<50000;++row){for(auto& s:data.studies)s.values.push_back(NAN);
   if(fast){decoder.decode(payload);decoder.appendTo(data,columns,row);}
   else {for(auto& [name,value]:decodeFIGS(payload)){auto found=columns.find(name);if(found==columns.end()){auto index=data.studies.size();columns.emplace(name,index);data.studies.push_back({name,std::vector<double>(row+1,NAN)});found=columns.find(name);}data.studies[found->second].values[row]=studyNumber(value);}}
  }
  auto ms=std::chrono::duration<double,std::milli>(std::chrono::steady_clock::now()-start).count();double checksum=0;for(auto& s:data.studies)for(auto v:s.values)checksum+=v;return std::pair{ms,checksum};
 };
 auto old=run(false),fast=run(true);if(old.second!=fast.second)throw std::runtime_error("Decoder benchmark values differ.");
 std::ofstream report(output);report<<"Synthetic benchmark: 50,000 bars x 64 numeric studies (no SQL/network)\nDictionary path ms: "<<old.first<<"\nReusable path ms: "<<fast.first<<"\nSpeedup: "<<old.first/fast.first<<"x\nMatching checksum: "<<fast.second<<'\n';
}

}
