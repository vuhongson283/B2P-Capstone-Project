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

    public virtual DbSet<BookingDetail> BookingDetails { get; set; }

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
            entity.HasKey(e => e.BankAccountId).HasName("PK__BankAcco__4FC8E4A178130BBF");

            entity.ToTable("BankAccount");

            entity.HasIndex(e => e.UserId, "UQ__BankAcco__1788CC4D4366CC7D").IsUnique();

            entity.Property(e => e.AccountHolder).HasMaxLength(100);
            entity.Property(e => e.AccountNumber).HasMaxLength(50);

            entity.HasOne(d => d.BankType).WithMany(p => p.BankAccounts)
                .HasForeignKey(d => d.BankTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__BankAccou__BankT__6754599E");

            entity.HasOne(d => d.User).WithOne(p => p.BankAccount)
                .HasForeignKey<BankAccount>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__BankAccou__UserI__44FF419A");
        });

        modelBuilder.Entity<BankType>(entity =>
        {
            entity.HasKey(e => e.BankTypeId).HasName("PK__BankType__91F2C3798B47CF33");

            entity.ToTable("BankType");

            entity.HasIndex(e => e.BankName, "UQ__BankType__DA9ADFAADD855B4D").IsUnique();

            entity.Property(e => e.BankName).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(255);
        });

        modelBuilder.Entity<Blog>(entity =>
        {
            entity.HasKey(e => e.BlogId).HasName("PK__Blog__54379E307B34C652");

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

            entity.Property(e => e.CreateAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsDayOff).HasDefaultValue(false);
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");

            entity.HasOne(d => d.Status).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Booking_Status");

            entity.HasOne(d => d.User).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Booking__UserId__693CA210");
        });

        modelBuilder.Entity<BookingDetail>(entity =>
        {
            entity.HasKey(e => e.BookingDetailId).HasName("PK__BookingD__8136D45A1FD9E5D5");

            entity.ToTable("BookingDetail");

            entity.Property(e => e.CheckInDate).HasColumnType("datetime");
            entity.Property(e => e.CreateAt).HasColumnType("datetime");
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");

            entity.HasOne(d => d.Booking).WithMany(p => p.BookingDetails)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BookingDetail_Booking");

            entity.HasOne(d => d.Court).WithMany(p => p.BookingDetails)
                .HasForeignKey(d => d.CourtId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BookingDetail_Court");

            entity.HasOne(d => d.Status).WithMany(p => p.BookingDetails)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BookingDetail_Status");

            entity.HasOne(d => d.TimeSlot).WithMany(p => p.BookingDetails)
                .HasForeignKey(d => d.TimeSlotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BookingDetail_TimeSlot");
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.CommentId).HasName("PK__Comment__C3B4DFCAC3B22144");

            entity.ToTable("Comment");

            entity.Property(e => e.PostAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Blog).WithMany(p => p.Comments)
                .HasForeignKey(d => d.BlogId)
                .HasConstraintName("FK__Comment__BlogId__6FE99F9F");

            entity.HasOne(d => d.ParentComment).WithMany(p => p.InverseParentComment)
                .HasForeignKey(d => d.ParentCommentId)
                .HasConstraintName("FK__Comment__ParentC__70DDC3D8");

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
            entity.HasKey(e => e.CategoryId).HasName("PK__CourtCat__19093A0B85B9E6FC");

            entity.Property(e => e.CategoryName).HasMaxLength(100);
        });

        modelBuilder.Entity<Facility>(entity =>
        {
            entity.HasKey(e => e.FacilityId).HasName("PK__Facility__5FB08A74A68A9609");

            entity.ToTable("Facility");

            entity.Property(e => e.Contact).HasMaxLength(100);
            entity.Property(e => e.FacilityName)
                .HasMaxLength(255)
                .HasDefaultValue("Unknown Facility");
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
            entity.HasKey(e => e.RatingId).HasName("PK__Rating__FCCDF87CEDC28A62");

            entity.ToTable("Rating");

            entity.Property(e => e.CreateAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Booking).WithMany(p => p.Ratings)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("FK__Rating__BookingI__71D1E811");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Role__8AFACE1A067D3E78");

            entity.ToTable("Role");

            entity.HasIndex(e => e.RoleName, "UQ__Role__8A2B61604B500CFC").IsUnique();

            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<Slider>(entity =>
        {
            entity.HasKey(e => e.SlideId);

            entity.ToTable("Slider");

            entity.Property(e => e.SlideId).ValueGeneratedOnAdd();
            entity.Property(e => e.SlideDescription).HasMaxLength(500);
            entity.Property(e => e.SlideUrl).HasMaxLength(500);

            entity.HasOne(d => d.Status).WithMany(p => p.Sliders)
                .HasForeignKey(d => d.StatusId)
                .HasConstraintName("FK_Slider_Status");
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

            entity.Property(e => e.Discount).HasColumnType("decimal(5, 2)");
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

            entity.HasIndex(e => e.Email, "UQ__User__A9D10534D6ABFED3").IsUnique();

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.CreateAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .HasDefaultValue("");
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.StatusId).HasDefaultValue(1);

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
            entity.HasKey(e => e.UserTokenId).HasName("PK__UserToke__BD92DEDB5AAE75B4");

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
