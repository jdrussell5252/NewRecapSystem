using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using NewRecap.Model;
using NewRecap.MyAppHelper;
using NewRecap.Services;
using System.Security.Claims;

namespace NewRecap.Pages.AdminPages
{
    public class RecapWeeklyLogModel : PageModel
    {

        private readonly WeeklyReportService _weeklyReportService;

        public List<WeeklyReport> WeeklyReports { get; set; } = new();
        public DateTime WeekStart { get; set; }

        public List<WeeklyReport> DailyReports { get; set; } = new();
        public DateTime DayDate { get; set; }

        public bool IsAdmin { get; set; }


        public RecapWeeklyLogModel(WeeklyReportService weeklyReportService)
        {
            _weeklyReportService = weeklyReportService;
        }// End of 'RecapWeeklyLogModel'.

        public IActionResult OnGet(DateTime? weekStart, DateTime? dayDate)
        {
            // Safely access the NameIdentifier claim
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            /*--------------------ADMIN PRIV----------------------*/
            if (userIdClaim != null)
            {
                int userId = int.Parse(userIdClaim.Value); // Use the claim value only if it exists
                CheckIfUserIsAdmin(userId);

            }

            if (!IsAdmin)
                return Forbid();

            // Determine the start of the week
            WeekStart = weekStart?.Date ?? GetWeekStart(DateTime.Today);

            // Load the weekly report
            WeeklyReports = GetWeeklyReport(WeekStart);

            // Daily report
            DayDate = dayDate?.Date ?? DateTime.Today;
            DailyReports = GetDailyReport(DayDate);


            return Page();
        }// End of 'OnGet'.

        public List<WeeklyReport> GetDailyReport(DateTime day)
        {
            var reports = GetWeeklyEmployees(day); // reuse, still works for single day

            var workTimes = GetWeeklyWorkTime(day, 1);    // pass optional spanDays
            var travelTimes = GetWeeklyTravelTime(day, 1);
            var recapTimes = GetWeeklyRecapTime(day, 1);
            var recapCounts = GetWeeklyRecapCount(day, 1);

            foreach (var report in reports)
            {
                report.WeeklyWorkHours =
                    workTimes.TryGetValue(report.EmployeeID, out var w) ? w : 0;

                report.WeeklyTravelHours =
                    travelTimes.TryGetValue(report.EmployeeID, out var t) ? t : 0;

                report.WeeklyRecapHours =
                    recapTimes.TryGetValue(report.EmployeeID, out var r) ? r : 0;

                report.TotalHours =
                    report.WeeklyWorkHours + report.WeeklyTravelHours + report.WeeklyRecapHours;

                report.WeeklyRecapCount =
                    recapCounts.TryGetValue(report.EmployeeID, out var c) ? c : 0;
            }

            return reports;
        }// End of 'GetDailyReport'.


        public List<WeeklyReport> GetWeeklyReport(DateTime weekStart)
        {
            var reports = GetWeeklyEmployees(weekStart);

            var workTimes = GetWeeklyWorkTime(weekStart);
            var travelTimes = GetWeeklyTravelTime(weekStart);
            var recapTimes = GetWeeklyRecapTime(weekStart);
            var supportTimes = GetWeeklySupportTime(weekStart);
            var recapCounts = GetWeeklyRecapCount(weekStart);

            foreach (var report in reports)
            {
                report.WeeklyWorkHours =
                    workTimes.TryGetValue(report.EmployeeID, out var w) ? w : 0;

                report.WeeklyTravelHours =
                    travelTimes.TryGetValue(report.EmployeeID, out var t) ? t : 0;

                report.WeeklySupportHours =
                    recapTimes.TryGetValue(report.EmployeeID, out var s) ? s : 0;

                report.WeeklyRecapHours =
                    recapTimes.TryGetValue(report.EmployeeID, out var r) ? r : 0;

                report.TotalHours =
                    report.WeeklyWorkHours + report.WeeklyTravelHours + report.WeeklySupportHours + report.WeeklyRecapHours;

                report.WeeklyRecapCount =
                    recapCounts.TryGetValue(report.EmployeeID, out var c) ? c : 0;
            }

            return reports;
        }// End of 'GetWeeklyReport'.

        private List<WeeklyReport> GetWeeklyEmployees(DateTime weekStart, int spanDays = 7)
        {
            var results = new List<WeeklyReport>();

            using SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString());

            string sql = @"
                SELECT DISTINCT
                    e.EmployeeID,
                    e.EmployeeFName,
                    e.EmployeeLName
                FROM Recap r
                JOIN EmployeeRecaps er ON r.RecapID = er.RecapID
                JOIN Employee e ON er.EmployeeID = e.EmployeeID
                WHERE r.RecapDate >= @WeekStart
                  AND r.RecapDate < DATEADD(DAY, 7, @WeekStart)
                ORDER BY e.EmployeeLName, e.EmployeeFName;
            ";

            using SqlCommand cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@WeekStart", System.Data.SqlDbType.Date).Value = weekStart.Date;

            conn.Open();
            using SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                results.Add(new WeeklyReport
                {
                    EmployeeID = reader.GetInt32(0),
                    EmployeeFName = reader.GetString(1),
                    EmployeeLName = reader.GetString(2)
                });
            }

            return results;
        }// End of 'GetWeeklyEmployees'.

        private Dictionary<int, decimal> GetWeeklyWorkTime(DateTime weekStart, int spanDays = 7)
        {
            var totals = new Dictionary<int, decimal>();

            using SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString());

            /*string sql = @"
                SELECT
                    e.EmployeeID,
                    SUM(sew.TotalWorkTime) AS WeeklyWorkHours
                FROM Recap r
                JOIN EmployeeRecaps er ON r.RecapID = er.RecapID
                JOIN Employee e ON er.EmployeeID = e.EmployeeID
                JOIN StartEndWork sew ON r.RecapID = sew.RecapID
                WHERE r.RecapDate >= @WeekStart
                  AND r.RecapDate < DATEADD(DAY, @SpanDays, @WeekStart)
                GROUP BY e.EmployeeID;
            ";*/
            string sql = @"SELECT
                EmployeeID,
                SUM(TotalWorkTime) AS WeeklyWorkHours
            FROM
            (
                SELECT
                    er.EmployeeID,
                    sew.TotalWorkTime,
                    r.RecapDate as ActivityDate
                FROM Recap r
                JOIN EmployeeRecaps er ON r.RecapID = er.RecapID
                JOIN StartEndWork sew ON r.RecapID = sew.RecapID

                UNION ALL

                SELECT
                    ehr.EmployeeID,
                    sewh.TotalWorkTime,
                    hr.HardwareRecapDate as ActivityDate
                FROM HardwareRecap hr
                JOIN EmployeeHardwareRecaps ehr ON hr.HardwareRecapID = ehr.HardwareRecapID
                JOIN StartEndWorkHardware sewh ON hr.HardwareRecapID = sewh.HardwareRecapID
            ) AS Combined
            WHERE ActivityDate >= @WeekStart
                AND ActivityDate < DATEADD(DAY, 7, @WeekStart)
            GROUP BY EmployeeID;";

            using SqlCommand cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@WeekStart", System.Data.SqlDbType.Date).Value = weekStart.Date;
            conn.Open();
            using SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                totals[reader.GetInt32(0)] =
                    reader.IsDBNull(1) ? 0 : reader.GetDecimal(1);
            }

            return totals;
        }// End of 'GetWeeklyWorkTime'.

        private Dictionary<int, decimal> GetWeeklyTravelTime(DateTime weekStart, int spanDays = 7)
        {
            var totals = new Dictionary<int, decimal>();

            using SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString());

            string sql = @"
                SELECT
                    e.EmployeeID,
                    SUM(sew.TotalTravelTime) AS WeeklyTravelHours
                FROM Recap r
                JOIN EmployeeRecaps er ON r.RecapID = er.RecapID
                JOIN Employee e ON er.EmployeeID = e.EmployeeID
                JOIN StartEndTravel sew ON r.RecapID = sew.RecapID
                WHERE r.RecapDate >= @WeekStart
                  AND r.RecapDate < DATEADD(DAY, 7, @WeekStart)
                GROUP BY e.EmployeeID;
            ";

            using SqlCommand cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@WeekStart", System.Data.SqlDbType.Date).Value = weekStart.Date;

            conn.Open();
            using SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                totals[reader.GetInt32(0)] =
                    reader.IsDBNull(1) ? 0 : reader.GetDecimal(1);
            }

            return totals;
        }// End of 'GetWeeklyTravelTime'.

        private Dictionary<int, decimal> GetWeeklySupportTime(DateTime weekStart, int spanDays = 7)
        {
            var totals = new Dictionary<int, decimal>();

            using SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString());

            string sql = @"
                SELECT
                    e.EmployeeID,
                    SUM(sew.TotalSupportTime) AS WeeklySupportHours
                FROM Recap r
                JOIN EmployeeRecaps er ON r.RecapID = er.RecapID
                JOIN Employee e ON er.EmployeeID = e.EmployeeID
                JOIN StartEndSupport sew ON r.RecapID = sew.RecapID
                WHERE r.RecapDate >= @WeekStart
                  AND r.RecapDate < DATEADD(DAY, 7, @WeekStart)
                GROUP BY e.EmployeeID;
            ";

            using SqlCommand cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@WeekStart", System.Data.SqlDbType.Date).Value = weekStart.Date;

            conn.Open();
            using SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                totals[reader.GetInt32(0)] =
                    reader.IsDBNull(1) ? 0 : reader.GetDecimal(1);
            }

            return totals;
        }// End of 'GetWeeklySupportTime'.

        private Dictionary<int, decimal> GetWeeklyRecapTime(DateTime weekStart, int spanDays = 7)
        {
            var totals = new Dictionary<int, decimal>();

            using SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString());

            string sql = @"SELECT
                EmployeeID,
                SUM(TotalRecapTime) AS WeeklyRecapHours
            FROM
            (
                SELECT
                    er.EmployeeID,
                    sew.TotalRecapTime,
                    r.RecapDate as ActivityDate
                FROM Recap r
                JOIN EmployeeRecaps er ON r.RecapID = er.RecapID
                JOIN StartEndRecap sew ON r.RecapID = sew.RecapID

                UNION ALL

                SELECT
                    ehr.EmployeeID,
                    sewh.TotalRecapTime,
                    hr.HardwareRecapDate as ActivityDate
                FROM HardwareRecap hr
                JOIN EmployeeHardwareRecaps ehr ON hr.HardwareRecapID = ehr.HardwareRecapID
                JOIN StartEndRecapHardware sewh ON hr.HardwareRecapID = sewh.HardwareRecapID
            ) AS Combined
            WHERE ActivityDate >= @WeekStart
                AND ActivityDate < DATEADD(DAY, 7, @WeekStart)
            GROUP BY EmployeeID;";

            using SqlCommand cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@WeekStart", System.Data.SqlDbType.Date).Value = weekStart.Date;

            conn.Open();
            using SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                totals[reader.GetInt32(0)] =
                    reader.IsDBNull(1) ? 0 : reader.GetDecimal(1);
            }

            return totals;
        }// End of 'GetWeeklyRecapTime'.

        private Dictionary<int, int> GetWeeklyRecapCount(DateTime weekStart, int spanDays = 7)
        {
            var totals = new Dictionary<int, int>();

            using SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString());

            string sql = @"
                SELECT
                    EmployeeID,
                    COUNT(*) AS RecapCount
                FROM
                (
                    SELECT
                        er.EmployeeID,
                        r.RecapDate AS ActivityDate
                    FROM Recap r
                    JOIN EmployeeRecaps er 
                        ON r.RecapID = er.RecapID

                    UNION ALL

                    SELECT
                        ehr.EmployeeID,
                        hr.HardwareRecapDate AS ActivityDate
                    FROM HardwareRecap hr
                    JOIN EmployeeHardwareRecaps ehr 
                        ON hr.HardwareRecapID = ehr.HardwareRecapID
                ) AS Combined
                WHERE ActivityDate >= @WeekStart
                  AND ActivityDate < DATEADD(DAY, 7, @WeekStart)
                GROUP BY EmployeeID;
            ";

            using SqlCommand cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@WeekStart", System.Data.SqlDbType.Date).Value = weekStart.Date;

            conn.Open();
            using SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                totals[reader.GetInt32(0)] = reader.GetInt32(1);
            }

            return totals;
        }// End of 'GetWeeklyRecapCount'.

        private static DateTime GetWeekStart(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            DateTime thisWeekMonday = date.AddDays(-diff);

            // Subtract 7 days to get previous week's Monday
            return thisWeekMonday.AddDays(-7);
        }// End of 'GetWeekStart'.

        /*--------------------ADMIN PRIV----------------------*/
        private void CheckIfUserIsAdmin(int userId)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string cmdText = "SELECT SystemUserRole FROM SystemUser WHERE SystemUserID = @SystemUserID";
                SqlCommand cmd = new SqlCommand(cmdText, conn);
                cmd.Parameters.Add("@SystemUserID", System.Data.SqlDbType.Int).Value = userId;
                conn.Open();
                var result = cmd.ExecuteScalar();

                bool isAdmin = result != null && Convert.ToBoolean(result);

                IsAdmin = isAdmin;
                ViewData["IsAdmin"] = isAdmin;
            }
        }//End of 'CheckIfUserIsAdmin'.
        /*--------------------ADMIN PRIV----------------------*/
    }// End of 'RecapWeeklyLog' Class.
}// End of 'namespace'.
