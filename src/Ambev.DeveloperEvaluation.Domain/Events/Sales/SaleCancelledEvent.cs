using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Events;

/// <summary>
/// Domain event raised when a sale is cancelled
/// </summary>
public class SaleCancelledEvent
{
    /// <summary>
    /// Gets the ID of the cancelled sale
    /// </summary>
    public Guid SaleId { get; }

    /// <summary>
    /// Gets the sale number
    /// </summary>
    public string SaleNumber { get; }

    /// <summary>
    /// Gets the customer ID
    /// </summary>
    public Guid CustomerId { get; }

    /// <summary>
    /// Gets the customer name
    /// </summary>
    public string CustomerName { get; }

    /// <summary>
    /// Gets the branch ID
    /// </summary>
    public Guid BranchId { get; }

    /// <summary>
    /// Gets the branch name
    /// </summary>
    public string BranchName { get; }

    /// <summary>
    /// Gets the total amount that was cancelled
    /// </summary>
    public decimal TotalAmount { get; }

    /// <summary>
    /// Gets the date when the sale was cancelled
    /// </summary>
    public DateTime CancelledAt { get; }

    /// <summary>
    /// Gets the reason for cancellation
    /// </summary>
    public string CancellationReason { get; }

    /// <summary>
    /// Initializes a new instance of the SaleCancelledEvent
    /// </summary>
    /// <param name="sale">The cancelled sale</param>
    /// <param name="cancellationReason">The reason for cancellation</param>
    public SaleCancelledEvent(Sale sale, string cancellationReason = "Sale cancelled")
    {
        SaleId = sale.Id;
        SaleNumber = sale.SaleNumber;
        CustomerId = sale.CustomerId;
        CustomerName = sale.CustomerName;
        BranchId = sale.BranchId;
        BranchName = sale.BranchName;
        TotalAmount = sale.TotalAmount;
        CancelledAt = sale.UpdatedAt ?? DateTime.UtcNow;
        CancellationReason = cancellationReason;
    }
}