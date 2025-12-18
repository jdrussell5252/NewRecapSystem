using Microsoft.Data.SqlClient;
using NewRecap.Model;
using NewRecap.MyAppHelper;

namespace NewRecap.Services
{
    public class WeeklyReportService
    {
        public void GenerateWeeklyReport(DateTime weekStart)
        {
            using SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString());

            string sql = @"
                INSERT INTO WeeklyReportLog (WeekStart, GeneratedOn)
                VALUES (@WeekStart, GETDATE());
            ";

            using SqlCommand cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@WeekStart", System.Data.SqlDbType.Date).Value = weekStart;

            conn.Open();
            cmd.ExecuteNonQuery();
        }// End of 'GenerateWeeklyReport'.

    }// End of 'WeeklyReportService' Class.
}// End of 'namespace'.
