using System.ComponentModel.DataAnnotations;

namespace NewRecap.Model
{
    public class EmployeeInfo
    {
        public int EmployeeID { get; set; }
        public string EmployeeFName { get; set; }
        public string EmployeeLName { get; set; }
        public bool IsSelected;
    }
}
