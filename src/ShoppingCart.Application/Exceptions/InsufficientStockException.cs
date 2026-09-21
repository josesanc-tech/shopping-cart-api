namespace ShoppingCart.Application.Exceptions;

public class InsufficientStockException : ConflictException
{
    public string ProductName { get; }
    public int AvailableStock { get; }

    public InsufficientStockException(string productName, int availableStock)
        : base($"Stock insuficiente para '{productName}'. Disponible: {availableStock}")
    {
        ProductName = productName;
        AvailableStock = availableStock;
    }
}