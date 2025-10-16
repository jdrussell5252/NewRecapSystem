using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NewRecap.Model;
using NewRecap.MyAppHelper;
using System.Data.OleDb;
using System.Text.RegularExpressions;

namespace NewRecap.Pages.Account
{
    public class RegisterModel : PageModel
    {
        [BindProperty]
        public Registration NewUser { get; set; }

        public List<string> PasswordErrors { get; set; } = new();

        public string connectionString = "Provider = Microsoft.ACE.OLEDB.12.0; Data Source = C:\\Users\\jaker\\OneDrive\\Desktop\\Nacspace\\New Recap\\NewRecapDB\\NewRecapDB.accdb;";

        public IActionResult OnPost()
        {
            PasswordErrors.Clear();
            string password = NewUser.Password;

            if (password == null)
                return Page();
            if (password.Length < 10)
                PasswordErrors.Add("Password must be at least 10 characters long.");
            if (!Regex.IsMatch(password, @"\d"))
                PasswordErrors.Add("Password must contain at least one number.");
            if (!Regex.IsMatch(password, @"[A-Z]"))
                PasswordErrors.Add("Password must contain at least one uppercase letter.");
            if (!Regex.IsMatch(password, @"[a-z]"))
                PasswordErrors.Add("Password must contain at least one lowercase letter.");

            if (PasswordErrors.Count > 0)
            {
                return Page();
            }


            if (ModelState.IsValid)
            {
                using (OleDbConnection conn = new OleDbConnection(this.connectionString))
                {
                    conn.Open();
                    string cmdEmployeeText = "INSERT INTO Employee (EmployeeFName, EmployeeLName) VALUES (?, ?);";
                    OleDbCommand cmdE = new OleDbCommand(cmdEmployeeText, conn);        
                    cmdE.Parameters.AddWithValue("?", NewUser.FirstName);
                    cmdE.Parameters.AddWithValue("?", NewUser.LastName);
                    cmdE.ExecuteNonQuery();

                    // Get the new AutoNumber (must be SAME connection)
                    int employeeId;
                    using (var idCmd = new OleDbCommand("SELECT @@IDENTITY;", conn))
                    {
                        employeeId = Convert.ToInt32(idCmd.ExecuteScalar());
                    }

                    string cmdSystemUserText = "INSERT INTO SystemUser (EmployeeID, SystemUsername, SystemUserPassword, SystemUserRole, SystemUserEmail) VALUES (?, ?, ?, ?, ?);";
                    OleDbCommand cmdS = new OleDbCommand(cmdSystemUserText, conn);
                    cmdS.Parameters.AddWithValue("?", employeeId);
                    cmdS.Parameters.AddWithValue("?", NewUser.UserName);
                    cmdS.Parameters.AddWithValue("?", AppHelper.GeneratePasswordHash(NewUser.Password));
                    cmdS.Parameters.AddWithValue("?", 3);
                    cmdS.Parameters.AddWithValue("?", NewUser.Email);
                    cmdS.ExecuteNonQuery();
                    
                }
                    return RedirectToPage("/Account/Login");
            }

            return Page();
        }//End of 'OnPost'.
    }//End of 'Register'.
}//End of 'namespace'.
