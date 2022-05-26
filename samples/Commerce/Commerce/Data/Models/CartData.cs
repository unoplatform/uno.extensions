namespace Commerce.Data.Models;

public record CartData(IImmutableList<CartItemData> Items);
