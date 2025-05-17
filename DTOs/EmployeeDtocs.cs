namespace CeoMemo.DTOs
{
    public class EmployeeDto
    {
        public required string EmployeeID { get; set; }
        public required string FullName { get; set; }
        public int DepartmentID { get; set; }
        public int PositionID { get; set; }
        public decimal BaseSalary { get; set; }
    }
}
