using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quote.Domain.Entities;

namespace Quote.Infrastructure.Data.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.InvoiceNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(i => i.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(i => i.Subtotal)
            .HasPrecision(18, 2);

        builder.Property(i => i.GstAmount)
            .HasPrecision(18, 2);

        builder.Property(i => i.TotalAmount)
            .HasPrecision(18, 2);

        builder.Property(i => i.AmountPaid)
            .HasPrecision(18, 2);

        builder.Property(i => i.TradieBusinessName)
            .HasMaxLength(200);

        builder.Property(i => i.TradieAbn)
            .HasMaxLength(20);

        builder.Property(i => i.TradieEmail)
            .HasMaxLength(200);

        builder.Property(i => i.TradiePhone)
            .HasMaxLength(20);

        builder.Property(i => i.TradieAddress)
            .HasMaxLength(500);

        builder.Property(i => i.CustomerName)
            .HasMaxLength(200);

        builder.Property(i => i.CustomerEmail)
            .HasMaxLength(200);

        builder.Property(i => i.CustomerPhone)
            .HasMaxLength(20);

        builder.Property(i => i.CustomerAddress)
            .HasMaxLength(500);

        builder.Property(i => i.Description)
            .HasMaxLength(1000);

        builder.Property(i => i.Notes)
            .HasMaxLength(2000);

        builder.Property(i => i.TermsAndConditions)
            .HasMaxLength(5000);

        builder.Property(i => i.BankAccountName)
            .HasMaxLength(200);

        builder.Property(i => i.BankBsb)
            .HasMaxLength(10);

        builder.Property(i => i.BankAccountNumber)
            .HasMaxLength(20);

        builder.HasOne(i => i.Tradie)
            .WithMany()
            .HasForeignKey(i => i.TradieId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Customer)
            .WithMany()
            .HasForeignKey(i => i.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Job)
            .WithMany()
            .HasForeignKey(i => i.JobId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(i => i.Quote)
            .WithMany()
            .HasForeignKey(i => i.QuoteId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(i => i.LineItems)
            .WithOne(li => li.Invoice)
            .HasForeignKey(li => li.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(i => i.Payments)
            .WithOne(p => p.Invoice)
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => i.InvoiceNumber).IsUnique();
        builder.HasIndex(i => i.TradieId);
        builder.HasIndex(i => i.CustomerId);
        builder.HasIndex(i => i.Status);
        builder.HasIndex(i => i.DueDate);
    }
}

public class InvoiceLineItemConfiguration : IEntityTypeConfiguration<InvoiceLineItem>
{
    public void Configure(EntityTypeBuilder<InvoiceLineItem> builder)
    {
        builder.HasKey(li => li.Id);

        builder.Property(li => li.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(li => li.Unit)
            .HasMaxLength(50);

        builder.Property(li => li.Quantity)
            .HasPrecision(18, 4);

        builder.Property(li => li.UnitPrice)
            .HasPrecision(18, 2);

        builder.HasIndex(li => li.InvoiceId);
    }
}

public class InvoicePaymentConfiguration : IEntityTypeConfiguration<InvoicePayment>
{
    public void Configure(EntityTypeBuilder<InvoicePayment> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount)
            .HasPrecision(18, 2);

        builder.Property(p => p.PaymentMethod)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.TransactionId)
            .HasMaxLength(200);

        builder.Property(p => p.Reference)
            .HasMaxLength(200);

        builder.Property(p => p.Notes)
            .HasMaxLength(500);

        builder.HasIndex(p => p.InvoiceId);
        builder.HasIndex(p => p.TransactionId);
    }
}
