using FIGBrokerSvc.DataAccess;
using FIGBrokerSvc.Services;
using FIGCommon.Controllers;
using FIGCommon.Exceptions;
using FIGCommon.Interfaces;
using FIGCommon.Models;
using FIGCommon.Models.FIGProviderAPI;
using FIGCommon.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FIGBrokerSvc.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrderController : FIGBaseController
    {
        protected readonly OrderService _orderSvc;

        public OrderController(
              IWebHostEnvironment hostEnvironment
            , IHttpContextAccessor httpContextAccessor
            , ILogger<OrderController> logger
            , IServiceProvider serviceProvider
            , IConfiguration config
            , OrderService orderSvc
            ) : base(hostEnvironment, httpContextAccessor, logger, serviceProvider, config)
        {
            _orderSvc = orderSvc ?? throw new ArgumentNullException(nameof(orderSvc));
        }

        #region OrderPlacement

        // POST api/order/new
        [Authorize(Roles = $"{Role.Admin},{Role.SuperAdmin},{Role.Broker},{Role.SVC}")]
        [Route("new")]
        [HttpPost]
        public async Task<IActionResult> NewOrder([FromBody] OrderRequestDto req, CancellationToken ct)
        {
            try
            {
                OrderStatusDto? orderStatus = await _orderSvc.PlaceOrderNoWait(req, ct);
                if (orderStatus is null)
                {
                    throw new AppErrorException(ErrorCodes.OrderRejected, "", $"Could not place order for account {req.AccountId}");
                }
                return Ok(orderStatus);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // POST order/cancel
        [Authorize(Roles = $"{Role.Admin},{Role.SuperAdmin},{Role.Broker},{Role.SVC}")]
        [Route("cancel")]
        [HttpPost]
        public async Task<IActionResult> CancelOrder([FromBody] OrderCancelRequestDto req, CancellationToken ct)
        {
            try
            {
                OrderStatusDto? orderStatus = await _orderSvc.CancelOrder(req, ct);
                if (orderStatus is null)
                {
                    throw new AppErrorException(ErrorCodes.OrderRejected, "", $"Could not cancel order for account {req.AccountId}");

                }
                return Ok(orderStatus);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // POST order/cancelasync
        //[Authorize(Roles = $"{Role.Broker}")]
        [Authorize(Roles = $"{Role.Admin},{Role.SuperAdmin},{Role.Broker},{Role.SVC}")]
        [Route("cancelasync")]
        [HttpPost]
        public async Task<IActionResult> CancelOrderAsync([FromBody] OrderCancelRequestDto req, CancellationToken ct)
        {
            try
            {
                OrderStatusDto? orderStatus = await _orderSvc.CancelOrder(req, ct, 0);
                if (orderStatus is null)
                {
                    throw new AppErrorException(ErrorCodes.OrderRejected, "", $"Could not cancel order for account {req.AccountId}");
                }
                return Ok(orderStatus);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }
        #endregion OrderPlacement

        #region Prices

        // GET order/snapshot/{accountid}/{tickerId}
        [AllowAnonymous]
        [Route("snapshot/{accountid}/{tickerId}")]
        [HttpGet]
        public async Task<IActionResult> GetPriceSnapshot(string accountId, int tickerId, CancellationToken ct)
        {
            try
            {
                decimal? price = await _orderSvc.GetPriceSnapshot(accountId, tickerId, ct);
                if (price is null)
                {
                    throw new AppErrorException(ErrorCodes.OrderRejected, "", $"Could not retrieve price for ticker {tickerId}");
                }
                return Ok(price);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }
        #endregion Prices 

        #region OrderStatus 
        // POST api/status
        [Authorize(Roles = $"{Role.SVC}")]
        [Route("status")]
        [HttpPost]
        public async Task<IActionResult> GetOrderStatus([FromBody] OrderStatusRequestDto orderStatusReq)
        {
            try
            {
                // get account
                var acct = BrokerRepo.GetBrokerAccountByAccountId(orderStatusReq.AccountId);
                if (acct != null)
                {
                    // retrieve order by OrderRef
                    var order = BrokerRepo.GetOrderByOrderRefId(acct.Id, orderStatusReq.OrderRef);
                    if (order != null)
                    {
                        var result = new OrderStatusDto
                        {
                            OrderId = string.Empty,
                            BrokerRef = order.BrokerRef,
                            OrderRefId = order.OrderRefId,
                            BrokerOrderId = order.BrokerOrderId,
                            Qty = order.Qty,
                            Price = order.Price,
                            FilledQty = order.FillQty,
                            AvgFillPrice = order.FillPrice,
                            StatusCode = order.OrderStatus,
                            StatusTime = order.LastUpdateTime
                        };
                        return Ok(result);
                    }
                }
                return NotFound();
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }


        // GET api/status/force/{accountId}/{orderRef}
        [Authorize(Roles = $"{Role.SVC}")]
        //[AllowAnonymous]
        [Route("status/force/{accountId}/{orderRef}")]
        [HttpGet]
        public async Task<IActionResult> ForceSendStatus(string accountId, string orderRef)
        {
            try
            {
                // get account
                var acct = BrokerRepo.GetBrokerAccountByAccountId(accountId);
                if (acct != null)
                {
                    // retrieve order by OrderRef
                    var order = BrokerRepo.GetOrderByOrderRefId(acct.Id, orderRef);
                    if (order != null)
                    {
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var scopedServices = scope.ServiceProvider;
                            var atsAPI = scopedServices.GetRequiredService<ATSApi>();
                            var result = await atsAPI.SendStatusUpdate(new OrderStatusDto
                            {
                                OrderId = string.Empty,
                                BrokerRef = order.BrokerRef,
                                OrderRefId = order.OrderRefId,
                                BrokerOrderId = order.BrokerOrderId,
                                Qty = order.Qty,
                                Price = order.Price,
                                FilledQty = order.FillQty,
                                AvgFillPrice = order.FillPrice,
                                StatusCode = order.OrderStatus,
                                StatusTime = order.LastUpdateTime
                            });
                            return Ok(result);
                        }

                    }
                }
                return Ok(false);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }
        #endregion OrderStatus 


    }
}
