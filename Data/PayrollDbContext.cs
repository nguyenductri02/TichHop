﻿using System;
using System.Collections.Generic;
using CeoMemo.Models.Payroll;
using Microsoft.EntityFrameworkCore;

namespace CeoMemo.Data;

public partial class PayrollDbContext : DbContext
{
    public PayrollDbContext()
    {
    }

    public PayrollDbContext(DbContextOptions<PayrollDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Attendance> Attendances { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<Position> Positions { get; set; }

    public virtual DbSet<Salary> Salaries { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Server=localhost;Database=Payroll;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(e => e.AttendanceId).HasName("PK_Attendance");

            entity.Property(e => e.AbsentDays).HasDefaultValueSql("'0'");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.LeaveDays).HasDefaultValueSql("'0'");
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.DepartmentId).HasName("PK_Payroll_Department");

            entity.Property(e => e.DepartmentId).ValueGeneratedNever();
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeId).HasName("PK_Payroll_Employee");

            entity.Property(e => e.EmployeeId).ValueGeneratedNever();
        });

        modelBuilder.Entity<Position>(entity =>
        {
            entity.HasKey(e => e.PositionId).HasName("PK_Payroll_Position");

            entity.Property(e => e.PositionId).ValueGeneratedNever();
        });

        modelBuilder.Entity<Salary>(entity =>
        {
            entity.HasKey(e => e.SalaryId).HasName("PK_Salary");

            entity.Property(e => e.Bonus).HasDefaultValueSql("'0.00'");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Deductions).HasDefaultValueSql("'0.00'");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
