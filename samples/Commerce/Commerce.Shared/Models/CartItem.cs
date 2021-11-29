using System;
using System.Linq;

namespace Commerce.Models;

public record CartItem(Product Product, int Quantity);
