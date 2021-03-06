﻿using System;
using System.Globalization;
using System.Threading.Tasks;
using ShopVisibleAccess.Misc;
using ShopVisibleAccess.Models;
using ShopVisibleAccess.Models.Order;
using ShopVisibleAccess.OrderServices;

namespace ShopVisibleAccess
{
	public sealed class ShopVisibleOrdersService: IShopVisibleOrdersService
	{
		private readonly OrderServiceSoapClient _client;
		private readonly ShopVisibleCredentials _credentials;

		public ShopVisibleOrdersService( ShopVisibleCredentials credentials )
		{
			this._credentials = credentials;
			this._client = new OrderServiceSoapClient( credentials.OrdersEndpointName );
		}

		public bool IsOrdersReceived()
		{
			try
			{
				var endDateUtc = DateTime.UtcNow.AddMinutes( -5 );
				var startDateUtc = endDateUtc.AddHours( -1 );

				var xmlnewOrders = this._client.GetOrdersByDateTimeRange( this._credentials.ClientName, this._credentials.Guid,
					startDateUtc.ToString( CultureInfo.InvariantCulture ), endDateUtc.ToString( CultureInfo.InvariantCulture ), "true" );
				var newOrders = XmlSerializeHelpers.Deserialize< ShopVisibleOrders >( xmlnewOrders.OuterXml );

				if( newOrders.Response.ResponseHasErrors && newOrders.Response.ResponseCode != "SUCCESS" )
					return false;

				return true;
			}
			catch( Exception )
			{
				return false;
			}
		}

		public ShopVisibleOrders GetOrders( DateTime startDateUtc, DateTime endDateUtc )
		{
			var orders = new ShopVisibleOrders();
			ActionPolicies.Get.Do( () =>
			{
				var currentStartDate = startDateUtc;

				while( currentStartDate < endDateUtc )
				{
					var currentEndDate = currentStartDate.AddHours( 1 );
					if( currentEndDate > endDateUtc )
						currentEndDate = endDateUtc;

					var xmlnewOrders = this._client.GetOrdersByDateTimeRange( this._credentials.ClientName, this._credentials.Guid,
						currentStartDate.ToString( CultureInfo.InvariantCulture ),
						currentEndDate.ToString( CultureInfo.InvariantCulture ),
						"true" );
					var newOrders = XmlSerializeHelpers.Deserialize< ShopVisibleOrders >( xmlnewOrders.OuterXml );

					if( newOrders.Response.ResponseHasErrors && newOrders.Response.ResponseCode != "SUCCESS" )
					{
						throw new Exception(
							string.Format(
								"Sync Orders. Client: {0}, DateRange: ({1};{2}), IterationDateRange: ({3};{4}), ErrorDescription: {5}", this._credentials.ClientName,
								startDateUtc, endDateUtc, currentStartDate, currentEndDate, newOrders.Response.ResponseDescription ) );
					}

					orders.Orders.AddRange( newOrders.Orders );

					currentStartDate = currentEndDate;
				}

				if( this.ConcatModifiedOrders( startDateUtc, endDateUtc ) )
				{
					var xmlmodifiedOrders = this._client.GetChangedOrdersByDateRange( this._credentials.ClientName, this._credentials.Guid,
						startDateUtc.ToString( CultureInfo.InvariantCulture ), endDateUtc.ToString( CultureInfo.InvariantCulture ), "true" );
					var modifiedOrders = XmlSerializeHelpers.Deserialize< ShopVisibleOrders >( xmlmodifiedOrders.OuterXml );

					if( modifiedOrders.Response.ResponseHasErrors && modifiedOrders.Response.ResponseCode != "SUCCESS" )
					{
						throw new Exception( string.Format(
							"Sync Changed Orders. Client: {0}, DateRange: ({1};{2}), ErrorDescription: {3}", this._credentials.ClientName,
							startDateUtc, endDateUtc, modifiedOrders.Response.ResponseDescription ) );
					}

					orders.Orders.AddRange( modifiedOrders.Orders );
				}
			} );

			return orders;
		}

		public async Task< ShopVisibleOrders > GetOrdersAsync( DateTime startDateUtc, DateTime endDateUtc )
		{
			var orders = new ShopVisibleOrders();
			await ActionPolicies.GetAsync.Do( async () =>
			{
				var currentStartDate = startDateUtc;

				while( currentStartDate < endDateUtc )
				{
					var currentEndDate = currentStartDate.AddHours( 1 );
					if( currentEndDate > endDateUtc )
						currentEndDate = endDateUtc;

					var xmlnewOrders = await this._client.GetOrdersByDateTimeRangeAsync( this._credentials.ClientName, this._credentials.Guid,
						currentStartDate.ToString( CultureInfo.InvariantCulture ),
						currentEndDate.ToString( CultureInfo.InvariantCulture ),
						"true" );
					var newOrders = XmlSerializeHelpers.Deserialize< ShopVisibleOrders >( xmlnewOrders.OuterXml );

					if( newOrders.Response.ResponseHasErrors && newOrders.Response.ResponseCode != "SUCCESS" )
					{
						throw new Exception(
							string.Format(
								"Sync Orders. Client: {0}, DateRange: ({1};{2}), IterationDateRange: ({3};{4}), ErrorDescription: {5}", this._credentials.ClientName,
								startDateUtc, endDateUtc, currentStartDate, currentEndDate, newOrders.Response.ResponseDescription ) );
					}

					orders.Orders.AddRange( newOrders.Orders );

					currentStartDate = currentEndDate;
				}

				if( this.ConcatModifiedOrders( startDateUtc, endDateUtc ) )
				{
					var xmlmodifiedOrders =
						await this._client.GetChangedOrdersByDateRangeAsync( this._credentials.ClientName, this._credentials.Guid,
							startDateUtc.ToString( CultureInfo.InvariantCulture ), endDateUtc.ToString( CultureInfo.InvariantCulture ), "true" );
					var modifiedOrders = XmlSerializeHelpers.Deserialize< ShopVisibleOrders >( xmlmodifiedOrders.OuterXml );

					if( modifiedOrders.Response.ResponseHasErrors && modifiedOrders.Response.ResponseCode != "SUCCESS" )
					{
						throw new Exception( string.Format(
							"Sync Changed Orders. Client: {0}, DateRange: ({1};{2}), ErrorDescription: {3}", this._credentials.ClientName,
							startDateUtc, endDateUtc, modifiedOrders.Response.ResponseDescription ) );
					}

					orders.Orders.AddRange( modifiedOrders.Orders );
				}
			} );

			return orders;
		}

		public async Task< ShopVisibleOrders > GetOrdersToExportAdvancedAsync( ProcessingOptions processingOptions, AvailableExportTypes exportType, bool returnAddressesOnly, bool includeCustomerTokens, int ordersToReturn, int[] orderStatusOverride, int[] itemStatusOverride, int[] includeSupplierIds, int buyersRemorse = 60 )
		{
			var orders = new ShopVisibleOrders();
			includeSupplierIds = includeSupplierIds ?? new int[ 0 ];
			orderStatusOverride = orderStatusOverride ?? new int[ 0 ];
			itemStatusOverride = itemStatusOverride ?? new int[ 0 ];
			var requestParameters = processingOptions.ToPipedStrings( exportType, buyersRemorse, includeSupplierIds, returnAddressesOnly, includeCustomerTokens, ordersToReturn, orderStatusOverride, itemStatusOverride );

			await ActionPolicies.GetAsync.Do( async () =>
			{
				var xmlnewOrders = await this._client.GetOrdersToExportAdvancedAsync(
					this._credentials.ClientName,
					this._credentials.Guid,
					requestParameters
					);

				var newOrders = XmlSerializeHelpers.Deserialize< ShopVisibleOrders >( xmlnewOrders.OuterXml );

				if( newOrders.Response.ResponseHasErrors && newOrders.Response.ResponseCode != "SUCCESS" )
				{
					throw new Exception(
						string.Format(
							"Sync Orders. Client: {0}, Parameters: ({1}) ErrorDescription: {2}", this._credentials.ClientName,
							requestParameters, newOrders.Response.ResponseDescription ) );
				}

				orders.Orders.AddRange( newOrders.Orders );
			} );

			return orders;
		}

		private bool ConcatModifiedOrders( DateTime startDateUtc, DateTime endDateUtc )
		{
			return endDateUtc - startDateUtc < TimeSpan.FromDays( 2 );
		}
	}

	[ Flags ]
	public enum ProcessingOptions
	{
		ExportType = 0x1,
		BuyersRemorseMinutes = 0x2,
		IncludeSupplierIds = 0x4,
		ExcludeSupplierIds = 0x8,
		OrdersToReturn = 0x10,
		ReturnOrderAddressesOnly = 0x20,
		IncludeCustomerTokens = 0x40,
		OrderStatusOverride = 0x80,
		ItemStatusOverride = 0x100
	}

	public enum AvailableExportTypes
	{
		Customer = 0,
		CustomerListrak = 1,
		OrderListrak = 2,
		OrderDefault = 3,
		OrderGenericFile = 4,
		CartAllExport = 5,
		CartAbandonedExport = 6,
		CartAbandonedTierExport = 7,
		CartAbandonedNonTierExport = 8,
		GoogleTrustedStoreShipOrderExport = 9,
		GoogleTrustedStoreCancelledOrderExport = 10
	}
}