using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace DeliveryApi.Models;

public partial class DeliveryDbContext : DbContext
{
    public DeliveryDbContext()
    {
    }

    public DeliveryDbContext(DbContextOptions<DeliveryDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Admin> Admins { get; set; }
    public virtual DbSet<Category> Categories { get; set; }
    public virtual DbSet<Client> Clients { get; set; }
    public virtual DbSet<DeliveryMan> DeliveryMen { get; set; }
    public virtual DbSet<ExtraIngredient> ExtraIngredients { get; set; }
    public virtual DbSet<ItemExtra> ItemExtras { get; set; }
    public virtual DbSet<Order> Orders { get; set; }
    public virtual DbSet<OrderItem> OrderItems { get; set; }
    public virtual DbSet<PayMethod> PayMethods { get; set; }
    public virtual DbSet<Product> Products { get; set; }
    public virtual DbSet<Restaurant> Restaurants { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql("Name=ConnectionStrings:DefaultConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasKey(e => e.Dni).HasName("admins_pkey");
            entity.ToTable("admins");
            entity.HasIndex(e => e.Mail, "admins_mail_key").IsUnique();

            entity.Property(e => e.Dni).HasMaxLength(20).HasColumnName("dni");
            entity.Property(e => e.Mail).HasMaxLength(100).HasColumnName("mail");
            entity.Property(e => e.Name).HasMaxLength(100).HasColumnName("name");
            entity.Property(e => e.Password).HasMaxLength(255).HasColumnName("password");
            entity.Property(e => e.Rol).HasMaxLength(50).HasColumnName("rol");
            entity.Property(e => e.Salary).HasPrecision(10, 2).HasColumnName("salary");
            entity.Property(e => e.Status).HasDefaultValue(true).HasColumnName("status");
            entity.Property(e => e.Sucursal).HasMaxLength(100).HasColumnName("sucursal");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.IdCategory).HasName("categories_pkey");
            entity.ToTable("categories");

            entity.Property(e => e.IdCategory).HasMaxLength(50).HasColumnName("id_category");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name).HasMaxLength(100).HasColumnName("name");
        });

        // 🚨 BLOQUE DE CLIENT UNIFICADO Y CORREGIDO
        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(e => e.Dni).HasName("clients_pkey");
            entity.ToTable("clients");
            entity.HasIndex(e => e.Mail, "clients_mail_key").IsUnique();

            // Propiedades heredadas de Person
            entity.Property(e => e.Dni).HasMaxLength(20).HasColumnName("dni");
            entity.Property(e => e.Name).HasMaxLength(100).HasColumnName("name");
            entity.Property(e => e.Mail).HasMaxLength(100).HasColumnName("mail");
            entity.Property(e => e.Password).HasMaxLength(255).HasColumnName("password");
            entity.Property(e => e.Phone).HasMaxLength(20).HasColumnName("phone");
            entity.Property(e => e.DateBirth).HasColumnName("date_birth");

            // Propiedades propias de Client
            entity.Property(e => e.Genere).HasColumnName("genere");
            entity.Property(e => e.DateDelivery).HasColumnName("date_delivery");
            entity.Property(e => e.RegisterData).HasDefaultValueSql("CURRENT_DATE").HasColumnName("register_data");

            // Mapeo del Objeto de Valor (AddressData)
            entity.OwnsOne(e => e.AddressData, a =>
            {
                a.Property(p => p.City).HasMaxLength(50).HasColumnName("city");
                a.Property(p => p.Street1).HasMaxLength(100).HasColumnName("street_1");
                a.Property(p => p.Street2).HasMaxLength(100).HasColumnName("street_2");
                a.Property(p => p.PostalCode).HasMaxLength(20).HasColumnName("postal_code");
                a.Property(p => p.NumberHome).HasMaxLength(20).HasColumnName("number_home");
                a.Property(p => p.Reference).HasColumnName("reference");
            });
        });

        modelBuilder.Entity<DeliveryMan>(entity =>
        {
            entity.HasKey(e => e.Dni).HasName("delivery_men_pkey");
            entity.ToTable("delivery_men");
            entity.HasIndex(e => e.Mail, "delivery_men_mail_key").IsUnique();

            entity.Property(e => e.Dni).HasMaxLength(20).HasColumnName("dni");
            entity.Property(e => e.Name).HasMaxLength(100).HasColumnName("name");
            entity.Property(e => e.Mail).HasMaxLength(100).HasColumnName("mail");
            entity.Property(e => e.Password).HasMaxLength(255).HasColumnName("password");
            entity.Property(e => e.Phone).HasMaxLength(20).HasColumnName("phone");
            entity.Property(e => e.DateBirth).HasColumnName("date_birth");

            entity.Property(e => e.Comission).HasPrecision(10, 2).HasDefaultValueSql("0.0").HasColumnName("comission");
            entity.Property(e => e.LicencePlate).HasMaxLength(20).HasColumnName("licence_plate");
            entity.Property(e => e.Rating).HasPrecision(3, 2).HasDefaultValueSql("5.0").HasColumnName("rating");
            entity.Property(e => e.Status).HasDefaultValue(true).HasColumnName("status");
            entity.Property(e => e.TypeVehicle).HasMaxLength(50).HasColumnName("type_vehicle");
        });

        modelBuilder.Entity<ExtraIngredient>(entity =>
        {
            entity.HasKey(e => e.IdIngredient).HasName("extra_ingredients_pkey");
            entity.ToTable("extra_ingredients");

            entity.Property(e => e.IdIngredient).HasColumnName("id_ingredient");
            entity.Property(e => e.Name).HasMaxLength(100).HasColumnName("name");
            entity.Property(e => e.Price).HasPrecision(10, 2).HasColumnName("price");
        });

        modelBuilder.Entity<ItemExtra>(entity =>
        {
            entity.HasKey(e => new { e.IdItem, e.IdIngredient }).HasName("item_extras_pkey");
            entity.ToTable("item_extras");

            entity.Property(e => e.IdItem).HasColumnName("id_item");
            entity.Property(e => e.IdIngredient).HasColumnName("id_ingredient");
            entity.Property(e => e.ExtraPrice).HasPrecision(10, 2).HasColumnName("extra_price");

            entity.HasOne(d => d.IdIngredientNavigation).WithMany(p => p.ItemExtras)
                .HasForeignKey(d => d.IdIngredient)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("item_extras_id_ingredient_fkey");

            entity.HasOne(d => d.IdItemNavigation).WithMany(p => p.ItemExtras)
                .HasForeignKey(d => d.IdItem)
                .HasConstraintName("item_extras_id_item_fkey");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.IdOrder).HasName("orders_pkey");
            entity.ToTable("orders");

            entity.Property(e => e.IdOrder).HasMaxLength(50).HasColumnName("id_order");
            entity.Property(e => e.ClientDni).HasMaxLength(20).HasColumnName("client_dni");
            entity.Property(e => e.DateOrder).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnType("timestamp without time zone").HasColumnName("date_order");
            entity.Property(e => e.DeliveryDni).HasMaxLength(20).HasColumnName("delivery_dni");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            entity.Property(e => e.EstimatedTime).HasColumnName("estimated_time");
            entity.Property(e => e.IdMethod).HasMaxLength(50).HasColumnName("id_method");
            entity.Property(e => e.RealTime).HasColumnName("real_time");
            entity.Property(e => e.StartTime).HasColumnName("start_time");
            entity.Property(e => e.Total).HasPrecision(10, 2).HasColumnName("total");

            entity.HasOne(d => d.ClientDniNavigation).WithMany(p => p.Orders)
                .HasForeignKey(d => d.ClientDni)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("orders_client_dni_fkey");

            entity.HasOne(d => d.DeliveryDniNavigation).WithMany(p => p.Orders)
                .HasForeignKey(d => d.DeliveryDni)
                .HasConstraintName("orders_delivery_dni_fkey");

            entity.HasOne(d => d.IdMethodNavigation).WithMany(p => p.Orders)
                .HasForeignKey(d => d.IdMethod)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("orders_id_method_fkey");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.IdItem).HasName("order_items_pkey");
            entity.ToTable("order_items");

            entity.Property(e => e.IdItem).HasColumnName("id_item");
            entity.Property(e => e.IdOrder).HasMaxLength(50).HasColumnName("id_order");
            entity.Property(e => e.IdProduct).HasMaxLength(50).HasColumnName("id_product");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.Subtotal).HasPrecision(10, 2).HasColumnName("subtotal");

            entity.HasOne(d => d.IdOrderNavigation).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.IdOrder)
                .HasConstraintName("order_items_id_order_fkey");

            entity.HasOne(d => d.IdProductNavigation).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.IdProduct)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("order_items_id_product_fkey");
        });

        modelBuilder.Entity<PayMethod>(entity =>
        {
            entity.HasKey(e => e.IdMethod).HasName("pay_methods_pkey");
            entity.ToTable("pay_methods");

            entity.Property(e => e.IdMethod).HasMaxLength(50).HasColumnName("id_method");
            entity.Property(e => e.Details).HasColumnName("details");
            entity.Property(e => e.Provider).HasMaxLength(50).HasColumnName("provider");
            entity.Property(e => e.Type).HasMaxLength(50).HasColumnName("type");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.IdProduct).HasName("products_pkey");
            entity.ToTable("products");

            entity.Property(e => e.IdProduct).HasMaxLength(50).HasColumnName("id_product");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IdCategory).HasMaxLength(50).HasColumnName("id_category");
            entity.Property(e => e.IdRestaurant).HasColumnName("id_restaurant");
            entity.Property(e => e.Name).HasMaxLength(100).HasColumnName("name");
            entity.Property(e => e.Price).HasPrecision(10, 2).HasColumnName("price");
            entity.Property(e => e.Stock).HasColumnName("stock");

            entity.HasOne(d => d.IdCategoryNavigation).WithMany(p => p.Products)
                .HasForeignKey(d => d.IdCategory)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("products_id_category_fkey");

            entity.HasOne(d => d.IdRestaurantNavigation).WithMany(p => p.Products)
                .HasForeignKey(d => d.IdRestaurant)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("products_id_restaurant_fkey");
        });

        modelBuilder.Entity<Restaurant>(entity =>
        {
            entity.HasKey(e => e.IdRestaurant).HasName("restaurants_pkey");
            entity.ToTable("restaurants");

            entity.Property(e => e.IdRestaurant).HasColumnName("id_restaurant");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.Name).HasMaxLength(100).HasColumnName("name");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}