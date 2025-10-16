using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using NewRecap.Model;
using System.Data.OleDb;
using System.Security.Claims;

namespace NewRecap.Pages.RecapAdder
{
    public class AddHardwareRecapModel : PageModel
    {
        public Recap NewRecap { get; set; } = new Recap();
        public List<EmployeeInfo> Employees { get; set; } = new List<EmployeeInfo>();
        public List<SelectListItem> Locations { get; set; } = new List<SelectListItem>();
        public bool IsAdmin { get; set; }
        public string connectionString = "Provider = Microsoft.ACE.OLEDB.12.0; Data Source = C:\\Users\\jaker\\OneDrive\\Desktop\\Nacspace\\New Recap\\NewRecapDB\\NewRecapDB.accdb;";


        public void OnGet()
        {
            PopulateEmployeeList();
            PopulateLocationList();
            /*--------------------ADMIN PRIV----------------------*/
            // Safely access the NameIdentifier claim
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null)
            {
                int userId = int.Parse(userIdClaim.Value); // Use the claim value only if it exists
                //CheckIfUserIsAdmin(userId);
            }

            /*--------------------ADMIN PRIV----------------------*/
        }// End of 'OnGet'.

        public IActionResult OnPost()
        {
            if (ModelState.IsValid)
            {
                return RedirectToAction("/Index");
            }
            else
            {
                PopulateEmployeeList();
                PopulateLocationList();
                return Page();
            }
        }// End of 'OnPost'.

        private void PopulateLocationList()
        {
            using (OleDbConnection conn = new OleDbConnection(this.connectionString))
            {
                string query = "SELECT StoreLocationID, StoreState, StoreCity FROM StoreLocations";
                OleDbCommand cmd = new OleDbCommand(query, conn);
                conn.Open();
                OleDbDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        var location = new SelectListItem
                        {
                            Value = reader["StoreLocationID"].ToString(),
                            Text = $"{reader["StoreLocationID"]}, {reader["StoreState"]}, {reader["StoreCity"]}"
                        };
                        Locations.Add(location);

                    }
                }
            }
        }//End of 'PopulateLocationList'.

        private void PopulateEmployeeList()
        {
            using (OleDbConnection conn = new OleDbConnection(this.connectionString))
            {
                string query = "SELECT EmployeeID, EmployeeFName, EmployeeLName FROM Employee;";
                OleDbCommand cmd = new OleDbCommand(query, conn);
                conn.Open();
                OleDbDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        var employees = new EmployeeInfo();
                        employees.EmployeeID = int.Parse(reader["EmployeeID"].ToString());
                        employees.EmployeeFName = reader["EmployeeFName"].ToString();
                        employees.EmployeeLName = reader["EmployeeLName"].ToString();
                        employees.IsSelected = false;
                        Employees.Add(employees);
                    }
                }
            }
        }//End of 'PopulateEmployeeList'.

        /*--------------------ADMIN PRIV----------------------*/
        /*
        private void CheckIfUserIsAdmin(int userId)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string cmdText = "SELECT AccountTypeID FROM SystemUser WHERE SystemUserID = @SystemUserID";
                SqlCommand cmd = new SqlCommand(cmdText, conn);
                cmd.Parameters.AddWithValue("@SystemUserID", userId);
                conn.Open();
                var result = cmd.ExecuteScalar();

                // If AccountTypeID is 2, set IsUserAdmin to true
                if (result != null && result.ToString() == "2")
                {
                    this.IsAdmin = true;
                    ViewData["IsAdmin"] = true;
                }
                else
                {
                    this.IsAdmin = false;
                }
            }
        }//End of 'CheckIfUserIsAdmin'.
        /*--------------------ADMIN PRIV----------------------*/
    }// End of 'AddHardwareRecap' Class.
}// End of 'namespace'.
