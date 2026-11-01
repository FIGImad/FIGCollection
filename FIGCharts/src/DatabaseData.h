#pragma once
#include <sql.h>
#include <sqlext.h>
#include <chrono>
#include <sstream>
#include <iomanip>
#include "StudyValueCodec.h"
#pragma comment(lib,"odbc32.lib")
namespace figcharts {
inline void sqlCheck(SQLRETURN rc,SQLSMALLINT type,SQLHANDLE handle){if(SQL_SUCCEEDED(rc))return;SQLWCHAR state[6]{},message[1024]{};SQLINTEGER native=0;SQLSMALLINT length=0;SQLGetDiagRecW(type,handle,1,state,&native,message,1024,&length);throw std::runtime_error("Database operation failed (SQLSTATE "+utf8(reinterpret_cast<wchar_t*>(state))+"). Check the connection and database schema.");}
struct Database {
 SQLHENV env=SQL_NULL_HENV;SQLHDBC connection=SQL_NULL_HDBC;SQLHSTMT statement=SQL_NULL_HSTMT;
 Database(const std::wstring& text){try{sqlCheck(SQLAllocHandle(SQL_HANDLE_ENV,SQL_NULL_HANDLE,&env),SQL_HANDLE_ENV,env);sqlCheck(SQLSetEnvAttr(env,SQL_ATTR_ODBC_VERSION,reinterpret_cast<SQLPOINTER>(SQL_OV_ODBC3),0),SQL_HANDLE_ENV,env);sqlCheck(SQLAllocHandle(SQL_HANDLE_DBC,env,&connection),SQL_HANDLE_ENV,env);SQLSetConnectAttr(connection,SQL_LOGIN_TIMEOUT,reinterpret_cast<SQLPOINTER>(10),0);sqlCheck(SQLDriverConnectW(connection,nullptr,reinterpret_cast<SQLWCHAR*>(const_cast<wchar_t*>(text.c_str())),SQL_NTS,nullptr,0,nullptr,SQL_DRIVER_NOPROMPT),SQL_HANDLE_DBC,connection);sqlCheck(SQLSetConnectAttr(connection,SQL_ATTR_TXN_ISOLATION,reinterpret_cast<SQLPOINTER>(SQL_TXN_READ_COMMITTED),0),SQL_HANDLE_DBC,connection);sqlCheck(SQLAllocHandle(SQL_HANDLE_STMT,connection,&statement),SQL_HANDLE_DBC,connection);SQLSetStmtAttr(statement,SQL_ATTR_QUERY_TIMEOUT,reinterpret_cast<SQLPOINTER>(60),0);}catch(...){close();throw;}}
 void close(){if(statement)SQLFreeHandle(SQL_HANDLE_STMT,statement);if(connection){SQLDisconnect(connection);SQLFreeHandle(SQL_HANDLE_DBC,connection);}if(env)SQLFreeHandle(SQL_HANDLE_ENV,env);}
 ~Database(){close();}Database(const Database&)=delete;
 void query(const wchar_t* text){sqlCheck(SQLExecDirectW(statement,reinterpret_cast<SQLWCHAR*>(const_cast<wchar_t*>(text)),SQL_NTS),SQL_HANDLE_STMT,statement);}
 bool next(){auto rc=SQLFetch(statement);if(rc==SQL_NO_DATA)return false;sqlCheck(rc,SQL_HANDLE_STMT,statement);return true;}
 template<class T>T value(int column,SQLSMALLINT type){T v{};SQLLEN n=0;sqlCheck(SQLGetData(statement,SQLUSMALLINT(column),type,&v,sizeof(v),&n),SQL_HANDLE_STMT,statement);if(n==SQL_NULL_DATA)throw std::runtime_error("Unexpected SQL NULL in price data.");return v;}
 std::wstring text(int column){wchar_t buffer[1024]{};SQLLEN n;auto rc=SQLGetData(statement,SQLUSMALLINT(column),SQL_C_WCHAR,buffer,sizeof(buffer),&n);sqlCheck(rc,SQL_HANDLE_STMT,statement);if(rc==SQL_SUCCESS_WITH_INFO||n==SQL_NULL_DATA)throw std::runtime_error("Invalid collection label.");return buffer;}
 // Keep capacity across rows; large payloads must still be read to completion.
 bool blob(int column,std::vector<unsigned char>& bytes){
  bytes.clear();for(;;){unsigned char buffer[65536];SQLLEN n=0;auto rc=SQLGetData(statement,SQLUSMALLINT(column),SQL_C_BINARY,buffer,sizeof(buffer),&n);
   if(n==SQL_NULL_DATA)return true;if(rc==SQL_NO_DATA)break;sqlCheck(rc,SQL_HANDLE_STMT,statement);
   size_t count=rc==SQL_SUCCESS_WITH_INFO?sizeof(buffer):n>=0?size_t(n):0;
   if(count>sizeof(buffer)||bytes.size()+count>16*1024*1024)throw std::runtime_error("Invalid database FIGS payload size.");
   if(bytes.empty()&&n>0)bytes.reserve(std::min(size_t(n),size_t(16*1024*1024)));bytes.insert(bytes.end(),buffer,buffer+count);if(rc==SQL_SUCCESS)break;
  }return false;
 }

};
struct DatabaseCollection{int32_t id;std::wstring label;};
inline std::vector<DatabaseCollection> databaseCollections(const std::wstring& connection){Database db(connection);db.query(L"SELECT s.Id, CONCAT(s.Id, ' | ', t.LocalSymbol, ' | ', s.IntervalId, ' | ', s.ColType) FROM dbo.StudyCol s JOIN dbo.Ticker t ON t.Id=s.TickerId WHERE s.Enabled=1 ORDER BY s.OrderSeq,s.Id");std::vector<DatabaseCollection> list;while(db.next()){auto id=db.value<int32_t>(1,SQL_C_SLONG);list.push_back({id,db.text(2)});}return list;}
using DatabaseClock=std::chrono::steady_clock;
inline double databaseSeconds(DatabaseClock::time_point start){return std::chrono::duration<double>(DatabaseClock::now()-start).count();}
struct DatabaseLoadPerformance {
 size_t bars=0,studies=0;uint64_t payloadBytes=0;
 double connect=0,execute=0,fetch=0,decode=0,build=0,reader=0,chart=0,total=0;
 bool available=false,chartMeasured=false;
 std::wstring report()const{
  if(!available)return L"Load a database collection first.";
  std::wostringstream out;out<<std::fixed<<std::setprecision(3)
   <<L"All history | Studies and StudiesOverride\n"
   <<bars<<L" bars | "<<studies<<L" studies | "<<double(payloadBytes)/(1024*1024)<<L" MiB of study payloads (base + overrides)\n\n"
   <<L"Connect: "<<connect<<L" s\nPrepare / execute query: "<<execute<<L" s\n"
   <<L"Fetch rows / read binary data: "<<fetch<<L" s\nDecode studies / overrides: "<<decode<<L" s\nBuild / validate chart series: "<<build<<L" s\n"
   <<L"Database reader total: "<<reader<<L" s\n";
  if(chartMeasured)out<<L"Chart setup / calculations / rendering: "<<chart<<L" s\nTotal load: "<<total<<L" s\n";
  if(reader>0)out<<L"Reader throughput: "<<double(bars)/reader<<L" bars/s\n";
  out<<L"\nClient timings include driver, network and SQL waits; these are not server-only execution times.\nOverride values take precedence, including null values.\n\nPress Ctrl+C to copy this report.";return out.str();
 }
};
inline DatabaseLoadPerformance lastDatabasePerformance;
inline std::wstring databaseHistoryQuery(int32_t maxBars){
 if(maxBars<0)throw std::runtime_error("Invalid database history limit.");
 return std::wstring(L"SELECT * FROM (SELECT ")+(maxBars?L"TOP ("+std::to_wstring(maxBars)+L") ":L"")+L"StudyColId,RawTime,[Open],High,Low,[Close],Volume,Studies,StudiesOverride FROM dbo.StudyHistory WHERE StudyColId=? AND RawTime>=? AND RawTime<?"+(maxBars?L" ORDER BY RawTime DESC":L"")+L") recent ORDER BY RawTime";
}
inline CsvData databasePrices(const std::wstring& connection,int32_t id,int64_t from=INT64_MIN,int64_t to=INT64_MAX,int32_t maxBars=0){
 auto readerStart=DatabaseClock::now();DatabaseLoadPerformance perf;
 Database db(connection);perf.connect=databaseSeconds(readerStart);auto executeStart=DatabaseClock::now();
 // Limit on the server before transferring and decoding the large study payloads.
 // TOP is a validated integer; selection/range values remain bound parameters.
 if(maxBars<0)throw std::runtime_error("Invalid database history limit.");
 auto query=databaseHistoryQuery(maxBars);
 sqlCheck(SQLPrepareW(db.statement,reinterpret_cast<SQLWCHAR*>(query.data()),SQL_NTS),SQL_HANDLE_STMT,db.statement);
 SQLLEN n=0;sqlCheck(SQLBindParameter(db.statement,1,SQL_PARAM_INPUT,SQL_C_SLONG,SQL_INTEGER,0,0,&id,0,&n),SQL_HANDLE_STMT,db.statement);sqlCheck(SQLBindParameter(db.statement,2,SQL_PARAM_INPUT,SQL_C_SBIGINT,SQL_BIGINT,0,0,&from,0,&n),SQL_HANDLE_STMT,db.statement);sqlCheck(SQLBindParameter(db.statement,3,SQL_PARAM_INPUT,SQL_C_SBIGINT,SQL_BIGINT,0,0,&to,0,&n),SQL_HANDLE_STMT,db.statement);sqlCheck(SQLExecute(db.statement),SQL_HANDLE_STMT,db.statement);
 perf.execute=databaseSeconds(executeStart);
 int32_t rowId=0,volume=0;int64_t time=0;double o=0,h=0,l=0,c=0;SQLLEN lengths[7]{};
 auto bind=[&](int column,SQLSMALLINT type,auto& value){sqlCheck(SQLBindCol(db.statement,SQLUSMALLINT(column),type,&value,sizeof(value),&lengths[column-1]),SQL_HANDLE_STMT,db.statement);};
 bind(1,SQL_C_SLONG,rowId);bind(2,SQL_C_SBIGINT,time);bind(3,SQL_C_DOUBLE,o);bind(4,SQL_C_DOUBLE,h);bind(5,SQL_C_DOUBLE,l);bind(6,SQL_C_DOUBLE,c);bind(7,SQL_C_SLONG,volume);
 CsvData out;std::unordered_map<std::string,size_t> columns;NumericStudyDecoder baseDecoder,overrideDecoder;std::vector<unsigned char> baseBytes,overrideBytes;int64_t previous=INT64_MIN;for(;;){
 auto fetchStart=DatabaseClock::now();bool hasRow=db.next();perf.fetch+=databaseSeconds(fetchStart);if(!hasRow)break;auto buildStart=DatabaseClock::now();
 for(auto length:lengths)if(length==SQL_NULL_DATA)throw std::runtime_error("Unexpected SQL NULL in price data.");
 if(rowId!=id)throw std::runtime_error("Unexpected StudyColId.");if((!out.time.empty()&&time<=previous)||std::abs(double(time))>9007199254740991.0)throw std::runtime_error("Invalid database timestamp.");previous=time;
 if(!std::isfinite(o)||!std::isfinite(h)||!std::isfinite(l)||!std::isfinite(c)||h<l||h<std::max(o,c)||l>std::min(o,c))throw std::runtime_error("Invalid database OHLC.");perf.build+=databaseSeconds(buildStart);fetchStart=DatabaseClock::now();bool baseNull=db.blob(8,baseBytes);bool overrideNull=db.blob(9,overrideBytes);perf.fetch+=databaseSeconds(fetchStart);perf.payloadBytes+=baseBytes.size()+overrideBytes.size();if(baseNull)throw std::runtime_error("Studies cannot be SQL NULL.");
 auto decodeStart=DatabaseClock::now();baseDecoder.decode(baseBytes);if(!overrideNull)overrideDecoder.decode(overrideBytes);perf.decode+=databaseSeconds(decodeStart);buildStart=DatabaseClock::now();
 size_t row=out.time.size();out.time.push_back(double(time));out.open.push_back(o);out.high.push_back(h);out.low.push_back(l);out.close.push_back(c);out.volume.push_back(volume);
 for(auto& study:out.studies)study.values.push_back(NAN);
 baseDecoder.appendTo(out,columns,row);
 // Apply every override after base values, including null/string gaps.
 if(!overrideNull)overrideDecoder.appendTo(out,columns,row);
 perf.build+=databaseSeconds(buildStart);
 }

 auto sortStart=DatabaseClock::now();
 std::sort(out.studies.begin(),out.studies.end(),[](const Series& a,const Series& b){return a.name<b.name;});
 if(out.time.empty())throw std::runtime_error("No history is available for this study collection.");
 perf.build+=databaseSeconds(sortStart);perf.bars=out.time.size();perf.studies=out.studies.size();perf.reader=databaseSeconds(readerStart);perf.available=true;lastDatabasePerformance=perf;return out;
}
}
