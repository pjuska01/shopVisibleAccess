﻿using System;
using System.Threading.Tasks;
using ShopVisibleAccess.Models.Order;

namespace ShopVisibleAccess
{
	public interface IShopVisibleOrdersService
	{
		bool IsOrdersReceived();
		ShopVisibleOrders GetOrders( DateTime startDateUtc, DateTime endDateUtc );
		Task< ShopVisibleOrders > GetOrdersAsync( DateTime startDateUtc, DateTime endDateUtc );
		Task< ShopVisibleOrders > GetOrdersToExportAdvancedAsync( ProcessingOptions processingOptions, AvailableExportTypes exportType, bool returnAddressesOnly, bool includeCustomerTokens, int ordersToReturn, int[] orderStatusOverride, int[] itemStatusOverride, int[] includeSupplierIds, int buyersRemorse = 60 );
	}
}