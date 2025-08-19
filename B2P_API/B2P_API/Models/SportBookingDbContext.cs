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

    public virtual DbSet<Blog> Blogs { get; set; }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<BookingDetail> BookingDetails { get; set; }

    public virtual DbSet<Comment> Comments { get; set; }

    public virtual DbSet<CommissionPaymentHistory> CommissionPaymentHistories { get; set; }

    public virtual DbSet<Court> Courts { get; set; }

    public virtual DbSet<CourtCategory> CourtCategories { get; set; }

    public virtual DbSet<Facility> Facilities { get; set; }

    public virtual DbSet<Image> Images { get; set; }

    public virtual DbSet<MerchantPayment> MerchantPayments { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<PaymentMethod> PaymentMethods { get; set; }

    public virtual DbSet<PaymentType> PaymentTypes { get; set; }

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
        modelBuilder.Entity<Blog>(entity =>
        {
            entity.HasKey(e => e.BlogId).HasName("PK__Blog__54379E3020B0B253");

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
            entity.Property(e => e.TransactionCode).HasMaxLength(100);
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");

            entity.HasOne(d => d.PaymentType).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.PaymentTypeId)
                .HasConstraintName("FK_Booking_PaymentType");

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
            entity.HasKey(e => e.CommentId).HasName("PK__Comment__C3B4DFCA43D2077D");

            entity.ToTable("Comment");

            entity.Property(e => e.PostAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Blog).WithMany(p => p.Comments)
                .HasForeignKey(d => d.BlogId)
                .HasConstraintName("FK__Comment__BlogId__534D60F1");

            entity.HasOne(d => d.ParentComment).WithMany(p => p.InverseParentComment)
                .HasForeignKey(d => d.ParentCommentId)
                .HasConstraintName("FK__Comment__ParentC__5629CD9C");

            entity.HasOne(d => d.User).WithMany(p => p.Comments)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Comment__UserId__5441852A");
        });

        modelBuilder.Entity<CommissionPaymentHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Commissi__3214EC07B39154E0");

            entity.ToTable("CommissionPaymentHistory");

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Note).HasMaxLength(255);
            entity.Property(e => e.PaidAt).HasColumnType("datetime");

            entity.HasOne(d => d.Status).WithMany(p => p.CommissionPaymentHistories)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Commission_Status");

            entity.HasOne(d => d.User).WithMany(p => p.CommissionPaymentHistories)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Commission_User");
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
            entity.HasKey(e => e.CategoryId).HasName("PK__CourtCat__19093A0B3C79B75D");

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

        modelBuilder.Entity<MerchantPayment>(entity =>
        {
            entity.HasKey(e => e.MerchantPaymentId).HasName("PK__Merchant__6528F24361E071EB");

            entity.ToTable("MerchantPayment");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PaymentKey).HasMaxLength(255);
            entity.Property(e => e.StatusId).HasDefaultValue(1);

            entity.HasOne(d => d.PaymentMethod).WithMany(p => p.MerchantPayments)
                .HasForeignKey(d => d.PaymentMethodId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MerchantPayment_Method");

            entity.HasOne(d => d.Status).WithMany(p => p.MerchantPayments)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MerchantPayment_Status");

            entity.HasOne(d => d.User).WithMany(p => p.MerchantPayments)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MerchantPayment_User");
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

        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.HasKey(e => e.PaymentMethodId).HasName("PK__PaymentM__DC31C1D3C245C6DB");

            entity.ToTable("PaymentMethod");

            entity.Property(e => e.Description).HasMaxLength(100);
        });

        modelBuilder.Entity<PaymentType>(entity =>
        {
            entity.HasKey(e => e.PaymentTypeId).HasName("PK__PaymentT__BA430B35279D3CDA");

            entity.ToTable("PaymentType");

            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<Rating>(entity =>
        {
            entity.HasKey(e => e.RatingId).HasName("PK__Rating__FCCDF87CDA7A388D");

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
            entity.HasKey(e => e.RoleId).HasName("PK__Role__8AFACE1A48FC5E24");

            entity.ToTable("Role");

            entity.HasIndex(e => e.RoleName, "UQ__Role__8A2B61604983BD21").IsUnique();

            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<Slider>(entity =>
        {
            entity.HasKey(e => e.SlideId);

            entity.ToTable("Slider");

            entity.Property(e => e.SlideDescription).HasMaxLength(500);
            entity.Property(e => e.SlideUrl).HasMaxLength(500);

            entity.HasOne(d => d.Status).WithMany(p => p.Sliders)
                .HasForeignKey(d => d.StatusId)
                .HasConstraintName("FK_Sliders_Status");
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

            entity.HasIndex(e => e.Email, "IX_User_Email_NotNull")
                .IsUnique()
                .HasFilter("([Email] IS NOT NULL)");

            entity.HasIndex(e => e.Phone, "IX_User_Phone_NotNull")
                .IsUnique()
                .HasFilter("([Phone] IS NOT NULL)");

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.CreateAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .HasDefaultValue("");
            entity.Property(e => e.Phone).HasMaxLength(50);
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
            entity.HasKey(e => e.UserTokenId).HasName("PK__UserToke__BD92DEDB9D7FCEFC");

            entity.ToTable("UserToken");

            entity.Property(e => e.AccessToken).HasMaxLength(1000);
            entity.Property(e => e.RefreshToken).HasMaxLength(1000);

            entity.HasOne(d => d.User).WithMany(p => p.UserTokens)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__UserToken__UserI__48CFD27E");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
