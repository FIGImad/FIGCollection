#include "fig/database.hpp"
#include <array>
#include <cstdlib>
#include <format>
#include <stdexcept>

namespace fig
{
namespace
{
void check(SQLRETURN result, SQLSMALLINT type, SQLHANDLE handle, std::string_view operation)
{
    if (result == SQL_SUCCESS || result == SQL_SUCCESS_WITH_INFO || result == SQL_NO_DATA)
        return;
    std::array<SQLCHAR, 6> state{};
    std::array<SQLCHAR, 1024> message{};
    SQLINTEGER native{};
    SQLSMALLINT size{};
    SQLGetDiagRecA(
        type, handle, 1, state.data(), &native, message.data(), static_cast<SQLSMALLINT>(message.size()), &size);
    throw std::runtime_error(std::format("{} failed [{}]: {}",
                                         operation,
                                         reinterpret_cast<const char*>(state.data()),
                                         reinterpret_cast<const char*>(message.data())));
}

std::vector<PriceBar> read_prices(OdbcStatement& statement)
{
    std::vector<PriceBar> result;
    while (statement.fetch())
        result.push_back({statement.integer(1),
                          statement.integer(2),
                          statement.integer64(3),
                          statement.decimal(5),
                          statement.decimal(6),
                          statement.decimal(7),
                          statement.decimal(8),
                          statement.integer64(9)});
    return result;
}
} // namespace

OdbcConnection::OdbcConnection(std::string connection_string)
{
    check(SQLAllocHandle(SQL_HANDLE_ENV, SQL_NULL_HANDLE, &environment_),
          SQL_HANDLE_ENV,
          environment_,
          "Allocate ODBC environment");
    check(SQLSetEnvAttr(environment_, SQL_ATTR_ODBC_VERSION, reinterpret_cast<SQLPOINTER>(SQL_OV_ODBC3), 0),
          SQL_HANDLE_ENV,
          environment_,
          "Select ODBC version");
    check(SQLAllocHandle(SQL_HANDLE_DBC, environment_, &connection_),
          SQL_HANDLE_DBC,
          connection_,
          "Allocate ODBC connection");
    std::array<SQLCHAR, 2048> output{};
    SQLSMALLINT output_size{};
    const auto result = SQLDriverConnectA(connection_,
                                          nullptr,
                                          reinterpret_cast<SQLCHAR*>(connection_string.data()),
                                          SQL_NTS,
                                          output.data(),
                                          static_cast<SQLSMALLINT>(output.size()),
                                          &output_size,
                                          SQL_DRIVER_NOPROMPT);
    check(result, SQL_HANDLE_DBC, connection_, "Connect to SQL Server");
}
OdbcConnection::~OdbcConnection()
{
    if (transaction_)
        SQLEndTran(SQL_HANDLE_DBC, connection_, SQL_ROLLBACK);
    if (connection_ != SQL_NULL_HDBC)
    {
        SQLDisconnect(connection_);
        SQLFreeHandle(SQL_HANDLE_DBC, connection_);
    }
    if (environment_ != SQL_NULL_HENV)
        SQLFreeHandle(SQL_HANDLE_ENV, environment_);
}
void OdbcConnection::begin()
{
    check(SQLSetConnectAttr(connection_, SQL_ATTR_AUTOCOMMIT, reinterpret_cast<SQLPOINTER>(SQL_AUTOCOMMIT_OFF), 0),
          SQL_HANDLE_DBC,
          connection_,
          "Begin transaction");
    transaction_ = true;
}
void OdbcConnection::commit()
{
    check(SQLEndTran(SQL_HANDLE_DBC, connection_, SQL_COMMIT), SQL_HANDLE_DBC, connection_, "Commit transaction");
    SQLSetConnectAttr(connection_, SQL_ATTR_AUTOCOMMIT, reinterpret_cast<SQLPOINTER>(SQL_AUTOCOMMIT_ON), 0);
    transaction_ = false;
}
void OdbcConnection::rollback()
{
    check(SQLEndTran(SQL_HANDLE_DBC, connection_, SQL_ROLLBACK), SQL_HANDLE_DBC, connection_, "Rollback transaction");
    SQLSetConnectAttr(connection_, SQL_ATTR_AUTOCOMMIT, reinterpret_cast<SQLPOINTER>(SQL_AUTOCOMMIT_ON), 0);
    transaction_ = false;
}

OdbcStatement::OdbcStatement(SQLHDBC connection, std::string_view sql)
{
    check(SQLAllocHandle(SQL_HANDLE_STMT, connection, &statement_),
          SQL_HANDLE_DBC,
          connection,
          "Allocate ODBC statement");
    check(SQLPrepareA(statement_,
                      reinterpret_cast<SQLCHAR*>(const_cast<char*>(sql.data())),
                      static_cast<SQLINTEGER>(sql.size())),
          SQL_HANDLE_STMT,
          statement_,
          "Prepare SQL statement");
}
OdbcStatement::~OdbcStatement()
{
    if (statement_ != SQL_NULL_HSTMT)
        SQLFreeHandle(SQL_HANDLE_STMT, statement_);
}
void OdbcStatement::bind(int index, int value)
{
    auto parameter = std::make_unique<Parameter>();
    parameter->int_value = value;
    parameter->indicator = 0;
    check(SQLBindParameter(statement_,
                           static_cast<SQLUSMALLINT>(index),
                           SQL_PARAM_INPUT,
                           SQL_C_SLONG,
                           SQL_INTEGER,
                           0,
                           0,
                           &parameter->int_value,
                           0,
                           &parameter->indicator),
          SQL_HANDLE_STMT,
          statement_,
          "Bind integer parameter");
    parameters_.push_back(std::move(parameter));
}
void OdbcStatement::bind(int index, std::int64_t value)
{
    auto parameter = std::make_unique<Parameter>();
    parameter->int64_value = value;
    parameter->indicator = 0;
    check(SQLBindParameter(statement_,
                           static_cast<SQLUSMALLINT>(index),
                           SQL_PARAM_INPUT,
                           SQL_C_SBIGINT,
                           SQL_BIGINT,
                           0,
                           0,
                           &parameter->int64_value,
                           0,
                           &parameter->indicator),
          SQL_HANDLE_STMT,
          statement_,
          "Bind bigint parameter");
    parameters_.push_back(std::move(parameter));
}
void OdbcStatement::bind(int index, std::string value)
{
    auto parameter = std::make_unique<Parameter>();
    parameter->string_value = std::move(value);
    parameter->indicator = SQL_NTS;
    check(SQLBindParameter(statement_,
                           static_cast<SQLUSMALLINT>(index),
                           SQL_PARAM_INPUT,
                           SQL_C_CHAR,
                           SQL_VARCHAR,
                           parameter->string_value.size(),
                           0,
                           parameter->string_value.data(),
                           static_cast<SQLLEN>(parameter->string_value.size() + 1),
                           &parameter->indicator),
          SQL_HANDLE_STMT,
          statement_,
          "Bind text parameter");
    parameters_.push_back(std::move(parameter));
}
void OdbcStatement::execute()
{
    check(SQLExecute(statement_), SQL_HANDLE_STMT, statement_, "Execute SQL statement");
}
bool OdbcStatement::fetch()
{
    const auto result = SQLFetch(statement_);
    if (result == SQL_NO_DATA)
        return false;
    check(result, SQL_HANDLE_STMT, statement_, "Fetch SQL row");
    return true;
}
int OdbcStatement::integer(int column) const
{
    SQLINTEGER value{};
    SQLLEN indicator{};
    check(SQLGetData(statement_, static_cast<SQLUSMALLINT>(column), SQL_C_SLONG, &value, sizeof value, &indicator),
          SQL_HANDLE_STMT,
          statement_,
          "Read integer column");
    return indicator == SQL_NULL_DATA ? 0 : value;
}
std::int64_t OdbcStatement::integer64(int column) const
{
    SQLBIGINT value{};
    SQLLEN indicator{};
    check(SQLGetData(statement_, static_cast<SQLUSMALLINT>(column), SQL_C_SBIGINT, &value, sizeof value, &indicator),
          SQL_HANDLE_STMT,
          statement_,
          "Read bigint column");
    return indicator == SQL_NULL_DATA ? 0 : value;
}
bool OdbcStatement::boolean(int column) const
{
    return integer(column) != 0;
}
std::string OdbcStatement::text(int column) const
{
    std::string result;
    std::array<char, 2048> part{};
    SQLLEN indicator{};
    for (;;)
    {
        const auto status =
            SQLGetData(statement_, static_cast<SQLUSMALLINT>(column), SQL_C_CHAR, part.data(), part.size(), &indicator);
        if (indicator == SQL_NULL_DATA)
            return {};
        check(status, SQL_HANDLE_STMT, statement_, "Read text column");
        result += part.data();
        if (status == SQL_SUCCESS)
            break;
    }
    return result;
}
Decimal OdbcStatement::decimal(int column) const
{
    const auto value = text(column);
    return value.empty() ? 0 : std::strtold(value.c_str(), nullptr);
}

std::vector<StudyCollectionConfig> MainRepository::study_collections() const
{
    OdbcConnection connection(connection_string_);
    OdbcStatement query(connection.handle(), "{CALL dbo.usp_study_col_select(?)}");
    query.bind(1, -1);
    query.execute();
    std::vector<StudyCollectionConfig> result;
    while (query.fetch())
        result.push_back({query.integer(1),
                          query.integer(2),
                          query.text(3),
                          query.text(4),
                          query.integer(5),
                          query.text(6),
                          query.boolean(7)});
    return result;
}
std::optional<Ticker> MainRepository::ticker(int id) const
{
    OdbcConnection connection(connection_string_);
    OdbcStatement query(connection.handle(), "{CALL dbo.usp_ticker_select(?)}");
    query.bind(1, id);
    query.execute();
    if (!query.fetch())
        return std::nullopt;
    Ticker result;
    result.id = query.integer(1);
    result.symbol = query.text(2);
    result.local_symbol = query.text(3);
    result.min_tick = query.decimal(8);
    result.period_multiplier = query.integer(12);
    return result;
}
std::optional<Interval> MainRepository::interval(std::string_view id) const
{
    OdbcConnection connection(connection_string_);
    OdbcStatement query(connection.handle(), "{CALL dbo.usp_interval_select(?)}");
    query.bind(1, std::string(id));
    query.execute();
    if (!query.fetch())
        return std::nullopt;
    return Interval{query.text(1), query.text(2), query.integer(3), query.integer(4)};
}
std::optional<DataSet> MainRepository::data_set(int id) const
{
    OdbcConnection connection(connection_string_);
    OdbcStatement query(connection.handle(), "{CALL dbo.usp_dataset_select(?)}");
    query.bind(1, id);
    query.execute();
    if (!query.fetch())
        return std::nullopt;
    return DataSet{query.integer(1), query.integer(2), query.text(3)};
}
std::optional<DataSet> MainRepository::data_set(int ticker_id, std::string_view interval_id) const
{
    OdbcConnection connection(connection_string_);
    OdbcStatement query(connection.handle(), "{CALL dbo.usp_dataset_query(?,?)}");
    query.bind(1, ticker_id);
    query.bind(2, std::string(interval_id));
    query.execute();
    if (!query.fetch())
        return std::nullopt;
    return DataSet{query.integer(1), query.integer(2), query.text(3)};
}
std::vector<PriceBar> MainRepository::prices(int data_set_id, std::int64_t start_time, int maximum) const
{
    OdbcConnection connection(connection_string_);
    OdbcStatement query(connection.handle(), "{CALL dbo.usp_price_data_select(?,?,?)}");
    query.bind(1, data_set_id);
    query.bind(2, start_time);
    query.bind(3, maximum);
    query.execute();
    return read_prices(query);
}
std::vector<PriceBar> MainRepository::prices_until(int data_set_id, std::int64_t until_time, int maximum) const
{
    OdbcConnection connection(connection_string_);
    OdbcStatement query(connection.handle(), "{CALL dbo.usp_price_data_select_until(?,?,?)}");
    query.bind(1, data_set_id);
    query.bind(2, until_time);
    query.bind(3, maximum);
    query.execute();
    return read_prices(query);
}
std::optional<PriceBar> MainRepository::last_price(int data_set_id) const
{
    OdbcConnection connection(connection_string_);
    OdbcStatement query(connection.handle(), "{CALL dbo.usp_price_data_select_last(?)}");
    query.bind(1, data_set_id);
    query.execute();
    if (!query.fetch())
        return std::nullopt;
    return PriceBar{query.integer(1),
                    query.integer(2),
                    query.integer64(3),
                    query.decimal(5),
                    query.decimal(6),
                    query.decimal(7),
                    query.decimal(8),
                    query.integer64(9)};
}
std::vector<StudyHistory> MainRepository::top_study_history(int id, int maximum) const
{
    OdbcConnection connection(connection_string_);
    OdbcStatement query(connection.handle(), "{CALL dbo.usp_study_history_select_top(?,?)}");
    query.bind(1, id);
    query.bind(2, maximum);
    query.execute();
    std::vector<StudyHistory> result;
    while (query.fetch())
    {
        StudyHistory row;
        row.id = query.integer(1);
        row.study_collection_id = query.integer(2);
        row.price.raw_time = query.integer64(3);
        row.price.open = query.decimal(4);
        row.price.high = query.decimal(5);
        row.price.low = query.decimal(6);
        row.price.close = query.decimal(7);
        row.price.volume = query.integer64(8);
        row.studies_json = query.text(9);
        result.push_back(std::move(row));
    }
    return result;
}
} // namespace fig
