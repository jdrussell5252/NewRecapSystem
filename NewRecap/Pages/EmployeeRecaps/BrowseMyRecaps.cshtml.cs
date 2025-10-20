using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NewRecap.Model;
using System.Data.OleDb;
using System.Security.Claims;

namespace NewRecap.Pages.EmployeeRecaps
{
    [Authorize]
    public class BrowseMyRecapsModel : PageModel
    {
        public List<RecapView> Recaps { get; set; } = new List<RecapView>();
        public string connectionString = "Provider = Microsoft.ACE.OLEDB.12.0; Data Source = C:\\Users\\jaker\\OneDrive\\Desktop\\Nacspace\\New Recap\\NewRecapDB\\NewRecapDB.accdb;";
        public void OnGet()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null)
            {
                int userId = int.Parse(userIdClaim.Value); // Use the claim value only if it exists
                PopulateRecapList(userId);
            }
        }

        private void PopulateRecapList(int id)
        {
            using (OleDbConnection conn = new OleDbConnection(this.connectionString))
            {
                //string query = "SELECT r.RecapID, r.RecapWorkorderNumber, r.RecapDate, r.RecapDescription, r.RecapState, r.RecapCity, r.RecapAssetNumber, r.RecapSerialNumber, v.VehicleID, v.VehicleName, v.VehicleNumber, v.VehicleVin, se.TotalWorkTime, se.TotalLunchTime, se.TotalDriveTime, sl.StoreLocationID, sl.StoreNumber, sl.StoreState, sl.StoreCity FROM ((Recap AS r LEFT JOIN Vehicle AS v ON v.VehicleID = r.VehicleID) LEFT JOIN StoreLocations AS sl ON sl.StoreLocationID = r.StoreLocationID) LEFT JOIN StartEnd AS se ON se.RecapID = r.RecapID ORDER BY r.RecapDate DESC, r.RecapID DESC;";
                const string query = @"
                SELECT
                r.RecapID,
  r.RecapWorkorderNumber,
  r.RecapDate,
  r.AddedBy,
  r.RecapDescription,
  r.RecapState,
  r.RecapCity,
  r.RecapAssetNumber,
  r.RecapSerialNumber,

  v.VehicleID,
  v.VehicleName,
  v.VehicleNumber,
  v.VehicleVin,

  ROUND(SUM(IIf(IsNull(se.TotalWorkTime), 0, se.TotalWorkTime)), 2)  AS WorkHours,
  ROUND(SUM(IIf(IsNull(se.TotalLunchTime), 0, se.TotalLunchTime)), 2) AS LunchHours,
  ROUND(SUM(IIf(IsNull(se.TotalDriveTime), 0, se.TotalDriveTime)), 2) AS DriveHours,
  ROUND(SUM(IIf(IsNull(se.TotalTime), 0, se.TotalTime)), 2) AS TotalHours,

  sl.StoreLocationID,
  sl.StoreNumber,
  sl.StoreState,
  sl.StoreCity
FROM ((((Recap AS r
LEFT JOIN Vehicle        AS v  ON v.VehicleID = r.VehicleID)
LEFT JOIN StoreLocations AS sl ON sl.StoreLocationID = r.StoreLocationID)
LEFT JOIN StartEnd       AS se ON se.RecapID = r.RecapID)
LEFT JOIN EmployeeRecaps AS er ON er.RecapID = r.RecapID)
WHERE r.AddedBy = @AddedBy
GROUP BY
  r.RecapID, r.RecapWorkorderNumber, r.RecapDate, r.AddedBy, r.RecapDescription,
  r.RecapState, r.RecapCity, r.RecapAssetNumber, r.RecapSerialNumber,
  v.VehicleID, v.VehicleName, v.VehicleNumber, v.VehicleVin,
  sl.StoreLocationID, sl.StoreNumber, sl.StoreState, sl.StoreCity
ORDER BY r.RecapDate DESC, r.RecapID DESC;";

                OleDbCommand cmd = new OleDbCommand(query, conn);
                cmd.Parameters.AddWithValue("@AddedBy", id);
                conn.Open();
                OleDbDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        RecapView ARecap = new RecapView
                        {
                            RecapID = reader.GetInt32(0),
                            RecapWorkorderNumber = reader.GetInt32(1),
                            RecapDate = reader.GetDateTime(2),
                            AddedBy = id,
                            RecapDescription = reader.GetString(4),
                            RecapState = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                            RecapCity = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),

                            RecapAssetNumber = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                            RecapSerialNumber = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),

                            VehicleID = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                            VehicleName = reader.IsDBNull(10) ? null : reader.GetString(10),
                            VehicleNumber = reader.IsDBNull(11) ? null : reader.GetInt32(11),
                            VehicleVin = reader.IsDBNull(12) ? null : reader.GetString(12),

                            TotalWorkTime = reader.IsDBNull(13) ? 0.0 : Math.Round(reader.GetDouble(13), 2),
                            TotalLunchTime = reader.IsDBNull(14) ? 0.0 : Math.Round(reader.GetDouble(14), 2),
                            TotalDriveTime = reader.IsDBNull(15) ? 0.0 : Math.Round(reader.GetDouble(15), 2),
                            TotalTime = reader.IsDBNull(16) ? 0.0 : Math.Round(reader.GetDouble(16), 2),

                            StoreLocationID = reader.IsDBNull(17) ? null : reader.GetInt32(17),
                            StoreNumber = reader.IsDBNull(18) ? null : reader.GetInt32(18),
                            StoreState = reader.IsDBNull(19) ? null : reader.GetString(19),
                            StoreCity = reader.IsDBNull(20) ? null : reader.GetString(20),



                            RecapEmployees = PopulateRecapEmployees(reader.GetInt32(0)),
                        };
                        Recaps.Add(ARecap);

                    }
                }
            }
        }//End of 'PopulateRecapList'.

        private List<string> PopulateRecapEmployees(int recapID)
        {
            List<string> Employees = new List<string>();
            using (OleDbConnection conn = new OleDbConnection(this.connectionString))
            {
                string query = "SELECT e.EmployeeFName, e.EmployeeLName " +
                               "FROM Employee AS e " +
                               "INNER JOIN EmployeeRecaps AS er ON e.EmployeeID = er.EmployeeID " +
                               "WHERE er.RecapID = @RecapID";
                OleDbCommand cmd = new OleDbCommand(query, conn);
                cmd.Parameters.AddWithValue("@RecapID", recapID);
                conn.Open();
                OleDbDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        string fName = reader.IsDBNull(0) ? "" : reader.GetString(0);
                        string lName = reader.IsDBNull(1) ? "" : reader.GetString(1);
                        Employees.Add($"{fName} {lName}");
                    }
                }
            }
            return Employees;
        }
    }
}
