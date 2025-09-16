using System;
using System.Collections.Generic;
using GuessItAPI.Models;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace GuessItAPI.Context;

public partial class GuessItDbContext : DbContext
{
    public GuessItDbContext()
    {
    }

    public GuessItDbContext(DbContextOptions<GuessItDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Card> Cards { get; set; }

    public virtual DbSet<CardsCategory> CardsCategories { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserDatum> UserData { get; set; }

    public virtual DbSet<UserSubscribe> UserSubscribes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseMySql("server=antishelkon.ru;port=3306;user=GuessItAPI;password=adminsky;database=GuessItDb", Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.0.42-mysql"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Card>(entity =>
        {
            entity.HasKey(e => e.CardId).HasName("PRIMARY");

            entity.HasIndex(e => e.CardCategoryId, "CardCategoryId");

            entity.Property(e => e.CardImage).HasColumnType("mediumblob");
            entity.Property(e => e.CardName).HasMaxLength(64);

            entity.HasOne(d => d.CardCategory).WithMany(p => p.Cards)
                .HasForeignKey(d => d.CardCategoryId)
                .HasConstraintName("Cards_ibfk_1");
        });

        modelBuilder.Entity<CardsCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PRIMARY");

            entity.HasIndex(e => e.CategoryOwnerId, "CaregoryOwnerId");

            entity.Property(e => e.CategoryDescription).HasColumnType("text");
            entity.Property(e => e.CategoryImage).HasColumnType("mediumblob");
            entity.Property(e => e.CategoryName).HasMaxLength(64);

            entity.HasOne(d => d.CategoryOwner).WithMany(p => p.CardsCategories)
                .HasForeignKey(d => d.CategoryOwnerId)
                .HasConstraintName("CardsCategories_ibfk_1");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PRIMARY");

            entity.Property(e => e.PasswordHash).HasMaxLength(64);
            entity.Property(e => e.Role)
                .HasMaxLength(8)
                .HasDefaultValueSql("'user'");
            entity.Property(e => e.Username).HasMaxLength(64);
        });

        modelBuilder.Entity<UserDatum>(entity =>
        {
            entity.HasKey(e => e.AvatarId).HasName("PRIMARY");

            entity.HasIndex(e => e.UserId, "UserId");

            entity.Property(e => e.Avatar).HasColumnType("mediumblob");

            entity.HasOne(d => d.User).WithMany(p => p.UserData)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("UserData_ibfk_1");
        });

        modelBuilder.Entity<UserSubscribe>(entity =>
        {
            entity.HasKey(e => e.SubscribeId).HasName("PRIMARY");

            entity.HasIndex(e => e.CategoryId, "CategoryId");

            entity.HasIndex(e => e.UserId, "UserId");

            entity.HasOne(d => d.Category).WithMany(p => p.UserSubscribes)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("UserSubscribes_ibfk_1");

            entity.HasOne(d => d.User).WithMany(p => p.UserSubscribes)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("UserSubscribes_ibfk_2");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
