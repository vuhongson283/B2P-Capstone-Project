using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace B2P_API.Models;

public partial class SportBookingDbContext : DbContext
{
    public SportBookingDbContext()
    {
    }

    public SportBookingDbContext(DbContextOptions<SportBookingDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<BankAccount> BankAccounts { get; set; }

    public virtual DbSet<BankType> BankTypes { get; set; }

    public virtual DbSet<Blog> Blogs { get; set; }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<Comment> Comments { get; set; }

    public virtual DbSet<Court> Courts { get; set; }

    public virtual DbSet<CourtCategory> CourtCategories { get; set; }

    public virtual DbSet<Facility> Facilities { get; set; }

    public virtual DbSet<Image> Images { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Rating> Ratings { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Slider> Sliders { get; set; }

    public virtual DbSet<Status> Statuses { get; set; }

    public virtual DbSet<TimeSlot> TimeSlots { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserToken> UserTokens { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("server =(local); database = SportBookingDB;uid=sa;pwd=123;TrustServerCertificate=true");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BankAccount>(entity =>
        {
            entity.HasKey(e => e.BankAccountId).HasName("PK__BankAcco__4FC8E4A1FFF9E256");

            entity.ToTable("BankAccount");

            entity.HasIndex(e => e.UserId, "UQ__BankAcco__1788CC4DC793F361").IsUnique();

            entity.Property(e => e.AccountHolder).HasMaxLength(100);
            entity.Property(e => e.AccountNumber).HasMaxLength(50);

            entity.HasOne(d => d.BankType).WithMany(p => p.BankAccounts)
                .HasForeignKey(d => d.BankTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__BankAccou__BankT__74AE54BC");

            entity.HasOne(d => d.User).WithOne(p => p.BankAccount)
                .HasForeignKey<BankAccount>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__BankAccou__UserI__44FF419A");
        });

        modelBuilder.Entity<BankType>(entity =>
        {
            entity.HasKey(e => e.BankTypeId).HasName("PK__BankType__91F2C37994F360E6");

            entity.ToTable("BankType");

            entity.HasIndex(e => e.BankName, "UQ__BankType__DA9ADFAAAB5BE600").IsUnique();

            entity.Property(e => e.BankName).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(255);
        });

        modelBuilder.Entity<Blog>(entity =>
        {
            entity.HasKey(e => e.BlogId).HasName("PK__Blog__54379E30E57F202F");

            entity.ToTable("Blog");

            entity.Property(e => e.PostAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Title).HasMaxLength(255);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.User).WithMany(p => p.Blogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Blog__UserId__4BAC3F29");
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.BookingId).HasName("PK__Booking__73951AED0DCA61FE");

            entity.ToTable("Booking");

            entity.Property(e => e.TotalPrice).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Court).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.CourtId)
                .HasConstraintName("FK__Booking__CourtId__6A30C649");

            entity.HasOne(d => d.Status).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Booking_Status");

            entity.HasOne(d => d.TimeSlot).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.TimeSlotId)
                .HasConstraintName("FK__Booking__TimeSlo__6B24EA82");

            entity.HasOne(d => d.User).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Booking__UserId__693CA210");
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.CommentId).HasName("PK__Comment__C3B4DFCA973EC6A3");

            entity.ToTable("Comment");

            entity.Property(e => e.PostAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Blog).WithMany(p => p.Comments)
                .HasForeignKey(d => d.BlogId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Comment__BlogId__7B5B524B");

            entity.HasOne(d => d.ParentComment).WithMany(p => p.InverseParentComment)
                .HasForeignKey(d => d.ParentCommentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Comment__ParentC__7C4F7684");

            entity.HasOne(d => d.User).WithMany(p => p.Comments)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Comment__UserId__5441852A");
        });

        modelBuilder.Entity<Court>(entity =>
        {
            entity.HasKey(e => e.CourtId).HasName("PK__Court__C3A67C9A99E5D279");

            entity.ToTable("Court");

            entity.Property(e => e.CourtName).HasMaxLength(100);
            entity.Property(e => e.PricePerHour).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Category).WithMany(p => p.Courts)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK__Court__CategoryI__628FA481");

            entity.HasOne(d => d.Facility).WithMany(p => p.Courts)
                .HasForeignKey(d => d.FacilityId)
                .HasConstraintName("FK__Court__FacilityI__619B8048");

            entity.HasOne(d => d.Status).WithMany(p => p.Courts)
                .HasForeignKey(d => d.StatusId)
                .HasConstraintName("FK_Court_Status1");
        });

        modelBuilder.Entity<CourtCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__CourtCat__19093A0BECCB913C");

            entity.Property(e => e.CategoryName).HasMaxLength(100);
        });

        modelBuilder.Entity<Facility>(entity =>
        {
            entity.HasKey(e => e.FacilityId).HasName("PK__Facility__5FB08A74A68A9609");

            entity.ToTable("Facility");

            entity.Property(e => e.Contact).HasMaxLength(100);
            entity.Property(e => e.Location).HasMaxLength(255);

            entity.HasOne(d => d.Status).WithMany(p => p.Facilities)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Facility_Status");

            entity.HasOne(d => d.User).WithMany(p => p.Facilities)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Facility__UserId__59063A47");
        });

        modelBuilder.Entity<Image>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("PK__Facility__60B51202994B66DB");

            entity.ToTable("Image");

            entity.Property(e => e.Caption).HasMaxLength(255);
            entity.Property(e => e.ImageUrl).HasMaxLength(255);
            entity.Property(e => e.Order).HasDefaultValue(1);

            entity.HasOne(d => d.Blog).WithMany(p => p.Images)
                .HasForeignKey(d => d.BlogId)
                .HasConstraintName("FK_Image_Blog");

            entity.HasOne(d => d.Facility).WithMany(p => p.Images)
                .HasForeignKey(d => d.FacilityId)
                .HasConstraintName("FK__FacilityI__Facil__5BE2A6F2");

            entity.HasOne(d => d.Slide).WithMany(p => p.Images)
                .HasForeignKey(d => d.SlideId)
                .HasConstraintName("FK_Image_Slider");

            entity.HasOne(d => d.User).WithMany(p => p.Images)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Image_User");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payment__9B556A380DC9AEB0");

            entity.ToTable("Payment");

            entity.Property(e => e.Amount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.TimeStamp)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Booking).WithMany(p => p.Payments)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("FK__Payment__Booking__6E01572D");

            entity.HasOne(d => d.Status).WithMany(p => p.Payments)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payment_Status");
        });

        modelBuilder.Entity<Rating>(entity =>
        {
            entity.HasKey(e => e.RatingId).HasName("PK__Rating__FCCDF87CD9400539");

            entity.ToTable("Rating");

            entity.Property(e => e.CreateAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Rating1).HasColumnName("Rating");

            entity.HasOne(d => d.Booking).WithMany(p => p.Ratings)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("FK__Rating__BookingI__71D1E811");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Role__8AFACE1AC1FC512F");

            entity.ToTable("Role");

            entity.HasIndex(e => e.RoleName, "UQ__Role__8A2B61609F005CBB").IsUnique();

            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<Slider>(entity =>
        {
            entity.HasKey(e => e.SlideId);

            entity.ToTable("Slider");

            entity.Property(e => e.SlideId).ValueGeneratedNever();
            entity.Property(e => e.SlideDescription).HasMaxLength(500);
            entity.Property(e => e.SlideUrl).HasMaxLength(500);
        });

        modelBuilder.Entity<Status>(entity =>
        {
            entity.ToTable("Status");

            entity.Property(e => e.StatusDescription).HasMaxLength(500);
            entity.Property(e => e.StatusName).HasMaxLength(50);
        });

        modelBuilder.Entity<TimeSlot>(entity =>
        {
            entity.HasKey(e => e.TimeSlotId).HasName("PK__TimeSlot__41CC1F32B26722EE");

            entity.ToTable("TimeSlot");

            entity.Property(e => e.StatusId).HasDefaultValue(1);

            entity.HasOne(d => d.Facility).WithMany(p => p.TimeSlots)
                .HasForeignKey(d => d.FacilityId)
                .HasConstraintName("FK__TimeSlot__Facili__656C112C");

            entity.HasOne(d => d.Status).WithMany(p => p.TimeSlots)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TimeSlot_Status");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__User__1788CC4CFCBB3028");

            entity.ToTable("User");

            entity.HasIndex(e => e.Username, "UQ__User__536C85E4EC4F884F").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__User__A9D10534D6ABFED3").IsUnique();

            entity.Property(e => e.CreateAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.StatusId).HasDefaultValue(1);
            entity.Property(e => e.Username).HasMaxLength(50);

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__User__RoleId__3D5E1FD2");

            entity.HasOne(d => d.Status).WithMany(p => p.Users)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_User_Status");
        });

        modelBuilder.Entity<UserToken>(entity =>
        {
            entity.HasKey(e => e.UserTokenId).HasName("PK__UserToke__BD92DEDB268CDE65");

            entity.ToTable("UserToken");

            entity.Property(e => e.AccessToken).HasMaxLength(255);
            entity.Property(e => e.RefreshToken).HasMaxLength(255);

            entity.HasOne(d => d.User).WithMany(p => p.UserTokens)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__UserToken__UserI__48CFD27E");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
