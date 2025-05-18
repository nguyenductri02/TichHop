# This script provides examples of Entity Framework Core commands
# that explicitly specify which DbContext to use

# For HumanDbContext (SQL Server)
Write-Host "Commands for HumanDbContext:" -ForegroundColor Green
Write-Host "------------------------"
Write-Host "Add-Migration InitialHuman -Context HumanDbContext -OutputDir Migrations/HumanDb"
Write-Host "Update-Database -Context HumanDbContext"
Write-Host "Get-DbContext -Context HumanDbContext"
Write-Host "Script-Migration -Context HumanDbContext"
Write-Host ""

# For PayrollDbContext (MySQL)
Write-Host "Commands for PayrollDbContext:" -ForegroundColor Green
Write-Host "--------------------------"
Write-Host "Add-Migration InitialPayroll -Context PayrollDbContext -OutputDir Migrations/PayrollDb"
Write-Host "Update-Database -Context PayrollDbContext"
Write-Host "Get-DbContext -Context PayrollDbContext"
Write-Host "Script-Migration -Context PayrollDbContext"
Write-Host ""

# Examples of using these commands with dotnet CLI
Write-Host "Dotnet CLI equivalents:" -ForegroundColor Yellow
Write-Host "----------------------"
Write-Host "dotnet ef migrations add InitialHuman --context HumanDbContext --output-dir Migrations/HumanDb"
Write-Host "dotnet ef database update --context HumanDbContext"
Write-Host "dotnet ef dbcontext info --context HumanDbContext"
Write-Host "dotnet ef migrations script --context HumanDbContext"
Write-Host ""

Write-Host "Remember to run these commands from the project directory that contains your DbContext classes." -ForegroundColor Cyan
