using System.ComponentModel.DataAnnotations;

namespace NewRecap.Model.Employees
{
    public class EmployeeInfo
    {
        public int EmployeeID { get; set; }
        public string EmployeeFName { get; set; }
        public string EmployeeLName { get; set; }
        public bool IsSelected;
    }// End of 'EmployeeInfo' Class.
}// End of 'namespace'.
