using BeaconAr.Domain.MasterData.Entities;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace BeaconAr.Data.MasterData.Context;

public partial class MasterDataDbContext
{
    #region Private Methods

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "EF Core Power Tools declares this generated partial hook as an instance method.")]
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>().Ignore(customer => customer.CustomerAddress);
        modelBuilder.Entity<CustomerAddress>()
            .HasOne(address => address.Customer)
            .WithMany(customer => customer.CustomerAddresses)
            .HasForeignKey(address => address.CustomerId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_CustomerAddress_Customer");
    }

    #endregion
}
