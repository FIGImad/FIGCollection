#pragma once
#include "fig/models.hpp"
#include <Windows.h>
#include <memory>
#include <optional>
#include <span>
#include <sql.h>
#include <sqlext.h>
#include <string>
#include <vector>

namespace fig
{
class OdbcConnection final
{
  public:
    explicit OdbcConnection(std::string connection_string);
    ~OdbcConnection();
    OdbcConnection(const OdbcConnection&) = delete;
    OdbcConnection& operator=(const OdbcConnection&) = delete;
    [[nodiscard]] SQLHDBC handle() const noexcept
    {
        return connection_;
    }
    void begin();
    void commit();
    void rollback();

  private:
    SQLHENV environment_{SQL_NULL_HENV};
    SQLHDBC connection_{SQL_NULL_HDBC};
    bool transaction_{};
};

class OdbcStatement final
{
  public:
    OdbcStatement(SQLHDBC connection, std::string_view sql);
    ~OdbcStatement();
    OdbcStatement(const OdbcStatement&) = delete;
    OdbcStatement& operator=(const OdbcStatement&) = delete;
    void bind(int index, int value);
    void bind(int index, std::int64_t value);
    void bind(int index, std::string value);
    void execute();
    [[nodiscard]] bool fetch();
    [[nodiscard]] int integer(int column) const;
    [[nodiscard]] std::int64_t integer64(int column) const;
    [[nodiscard]] bool boolean(int column) const;
    [[nodiscard]] Decimal decimal(int column) const;
    [[nodiscard]] std::string text(int column) const;

  private:
    struct Parameter
    {
        SQLLEN indicator{};
        int int_value{};
        std::int64_t int64_value{};
        std::string string_value;
    };
    SQLHSTMT statement_{SQL_NULL_HSTMT};
    std::vector<std::unique_ptr<Parameter>> parameters_;
};

class MainRepository final
{
  public:
    explicit MainRepository(std::string connection_string) : connection_string_(std::move(connection_string))
    {
    }
    [[nodiscard]] std::vector<StudyCollectionConfig> study_collections() const;
    [[nodiscard]] std::optional<Ticker> ticker(int id) const;
    [[nodiscard]] std::optional<Interval> interval(std::string_view id) const;
    [[nodiscard]] std::optional<DataSet> data_set(int id) const;
    [[nodiscard]] std::optional<DataSet> data_set(int ticker_id, std::string_view interval_id) const;
    [[nodiscard]] std::vector<PriceBar> prices(int data_set_id, std::int64_t start_time, int maximum) const;
    [[nodiscard]] std::vector<PriceBar> prices_until(int data_set_id, std::int64_t until_time, int maximum) const;
    [[nodiscard]] std::optional<PriceBar> last_price(int data_set_id) const;
    [[nodiscard]] std::vector<StudyHistory> top_study_history(int study_collection_id, int maximum) const;

  private:
    std::string connection_string_;
};
} // namespace fig
