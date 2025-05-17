using System;
using System.Collections.Generic;
using CeoMemo.Models.Human;
using Microsoft.EntityFrameworkCore;

namespace CeoMemo.Data;

public partial class HumanDbContext : DbContext
{
    public HumanDbContext()
    {
    }

    public HumanDbContext(DbContextOptions<HumanDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<Dividend> Dividends { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<Position> Positions { get; set; }

    public DbSet<User> Users { get; set; }
    public DbSet<Token> Tokens { get; set; }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Server=localhost;Database=Human;User Id=sa;Password=123456;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.DepartmentId).HasName("PK__Departme__B2079BCD7562F320");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<Dividend>(entity =>
        {
            entity.HasKey(e => e.DividendId).HasName("PK__Dividend__E519C7784290CF00");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Employee).WithMany(p => p.Dividends).HasConstraintName("FK_Dividends_Employees");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeId).HasName("PK__Employee__7AD04FF1C13154D6");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Department).WithMany(p => p.Employees).HasConstraintName("FK_Employees_Departments");

            entity.HasOne(d => d.Position).WithMany(p => p.Employees).HasConstraintName("FK_Employees_Positions");
        });

        modelBuilder.Entity<Position>(entity =>
        {
            entity.HasKey(e => e.PositionId).HasName("PK__Position__60BB9A59CA411623");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");
        });
        modelBuilder.Entity<User>()
               .Property(u => u.UpdatedAt)
               .HasDefaultValueSql("GETDATE()")
               .ValueGeneratedOnUpdate();

        // Configure Token entity
        modelBuilder.Entity<Token>()
            .Property(t => t.CreatedAt)
            .HasDefaultValueSql("GETDATE()");

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
