using System;
using System.Collections.Immutable;
using System.Linq;

namespace Commerce.Data.Models;

public record CartData(IImmutableList<CartItemData> Items);
