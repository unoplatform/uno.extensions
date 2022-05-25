using System;
using System.Linq;

namespace Commerce.Data.Models;

public record CartItemData(ProductData Product, uint Quantity);
