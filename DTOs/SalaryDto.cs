namespace CeoMemo.DTOs
{
    public class SalaryDto
    {
        public int SalaryId { get; set; }
        public DateOnly SalaryMonth { get; set; }
        public decimal BaseSalary { get; set; }
        public decimal? Bonus { get; set; }
        public decimal? Deductions { get; set; }
        public decimal NetSalary { get; set; }
        public int EmployeeID { get; set; }
        public required string FullName { get; set; }
        public int? DepartmentID { get; set; }
        public int? PositionID { get; set; }
    }
}
