namespace CLDV7112_PROJECT2.Models
{
    /// <summary>
    /// Represents a single item placed in a customer's shopping cart.
    /// Stores the product ID, name, unit price, quantity, category, and image URL.
    /// </summary>
    public class CartItem
    {
        // Unique identifier for the product
        public string ProductId { get; set; } = string.Empty;

        // Display name of the item
        public string ProductName { get; set; } = string.Empty;

        // Unit price in South African Rand (ZAR)
        public decimal Price { get; set; }

        // Image URL for rendering product thumbnail in cart view
        public string ImageUrl { get; set; } = string.Empty;

        // Product category (e.g. Electronics, Clothing)
        public string Category { get; set; } = string.Empty;

        // Number of units selected by the customer
        public int Quantity { get; set; }

        // Calculated line total (Price x Quantity)
        public decimal LineTotal => Price * Quantity;
    }
}
