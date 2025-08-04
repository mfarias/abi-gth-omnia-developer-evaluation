﻿using MediatR;
using FluentValidation;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;

/// <summary>
/// Handler for processing UpdateSaleCommand requests
/// </summary>
public class UpdateSaleHandler : IRequestHandler<UpdateSaleCommand, UpdateSaleResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly ILogger<UpdateSaleHandler> _logger;

    /// <summary>
    /// Initializes a new instance of UpdateSaleHandler
    /// </summary>
    /// <param name="saleRepository">The sale repository</param>
    /// <param name="logger">The logger instance</param>
    public UpdateSaleHandler(ISaleRepository saleRepository, ILogger<UpdateSaleHandler> logger)
    {
        _saleRepository = saleRepository;
        _logger = logger;
    }

    /// <summary>
    /// Handles the UpdateSaleCommand request
    /// </summary>
    /// <param name="command">The UpdateSale command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated sale details</returns>
    public async Task<UpdateSaleResult> Handle(UpdateSaleCommand command, CancellationToken cancellationToken)
    {
        var validator = new UpdateSaleValidator();
        var validationResult = await validator.ValidateAsync(command, cancellationToken);

        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var sale = await _saleRepository.GetByIdAsync(command.Id, cancellationToken);
        if (sale == null)
            throw new KeyNotFoundException($"Sale with ID {command.Id} not found");

        if (sale.Status == Domain.Enums.SaleStatus.Cancelled)
            throw new InvalidOperationException($"Cannot update cancelled sale {sale.SaleNumber}");

        var operations = new List<string>();

        foreach (var itemToAdd in command.ItemsToAdd)
        {
            var saleItem = new SaleItem(
                itemToAdd.ProductId,
                itemToAdd.ProductName,
                itemToAdd.ProductSku,
                itemToAdd.UnitPrice,
                itemToAdd.Quantity
            );

            sale.AddItem(saleItem);
            operations.Add($"Added {itemToAdd.Quantity} x {itemToAdd.ProductName}");
        }

        foreach (var itemToUpdate in command.ItemsToUpdate)
        {
            sale.UpdateItemQuantity(itemToUpdate.ProductId, itemToUpdate.Quantity);
            operations.Add($"Updated quantity for product {itemToUpdate.ProductId} to {itemToUpdate.Quantity}");
        }

        foreach (var productIdToRemove in command.ProductIdsToRemove)
        {
            sale.RemoveItem(productIdToRemove);
            operations.Add($"Removed product {productIdToRemove}");
        }

        var saleValidation = sale.Validate();
        if (!saleValidation.IsValid)
        {
            var errorMessages = string.Join(", ", saleValidation.Errors.Select(e => e.Error));
            throw new ValidationException($"Sale validation failed: {errorMessages}");
        }

        var updatedSale = await _saleRepository.UpdateAsync(sale, cancellationToken);

        var modificationType = string.Join("; ", operations);
        var saleModifiedEvent = new SaleModifiedEvent(updatedSale, modificationType);
        _logger.LogInformation("Sale modified: {@SaleModifiedEvent}", saleModifiedEvent);

        return new UpdateSaleResult
        {
            Id = updatedSale.Id,
            SaleNumber = updatedSale.SaleNumber,
            TotalAmount = updatedSale.TotalAmount,
            Status = updatedSale.Status,
            ItemCount = updatedSale.Items.Count,
            UpdatedAt = updatedSale.UpdatedAt ?? DateTime.UtcNow,
            Message = $"Sale {updatedSale.SaleNumber} updated successfully. Operations: {modificationType}"
        };
    }
}