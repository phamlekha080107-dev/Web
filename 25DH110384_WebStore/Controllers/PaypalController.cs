using _25DH110384_WebStore.Models;
using _25DH110384_WebStore.Models.ViewModel;
using PayPal.Api;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Item = PayPal.Api.Item;
using Payer = PayPal.Api.Payer;

namespace _25DH110384_WebStore.Controllers
{
    public class PaypalController : Controller
    {
        private QuanLiKhachHangEntities1 db = new QuanLiKhachHangEntities1();

        private Payment CreatePayment(APIContext apiContext, string redirectUrl, CheckoutVM model)
        {
            // 1. Lấy lại giỏ hàng từ Session nếu model.CartItems bị null khi Redirect
            if (model.CartItems == null || !model.CartItems.Any())
            {
                var cart = Session["Cart"] as Cart;
                if (cart != null && cart.Items != null)
                {
                    model.CartItems = cart.Items.ToList();
                }
            }

            // 2. Quy đổi VND -> USD (tỷ giá 25.000) để không vượt mức 10.000 USD của Sandbox
            const decimal exchangeRate = 25000m;
            var itemList = new ItemList() { items = new List<Item>() };
            decimal subtotalUSD = 0m;

            foreach (var item in model.CartItems)
            {
                decimal priceUSD = Math.Round(item.UnitPrice / exchangeRate, 2);
                if (priceUSD < 0.01m) priceUSD = 0.01m;

                subtotalUSD += priceUSD * item.Quantity;

                itemList.items.Add(new Item()
                {
                    name = item.ProductName,
                    currency = "USD",
                    price = priceUSD.ToString("0.00", CultureInfo.InvariantCulture),
                    quantity = item.Quantity.ToString(),
                    sku = item.ProductID.ToString()
                });
            }

            string totalStr = subtotalUSD.ToString("0.00", CultureInfo.InvariantCulture);

            var payer = new Payer() { payment_method = "paypal" };

            var redirUrls = new RedirectUrls()
            {
                cancel_url = redirectUrl,
                return_url = redirectUrl
            };

            var details = new Details()
            {
                tax = "0.00",
                shipping = "0.00",
                subtotal = totalStr
            };

            var amount = new Amount()
            {
                currency = "USD",
                total = totalStr,
                details = details
            };

            var transactionList = new List<Transaction>();
            transactionList.Add(new Transaction()
            {
                description = "Thanh toan don hang PHONE & ACCES",
                invoice_number = Convert.ToString((new Random()).Next(100000)),
                amount = amount,
                item_list = itemList
            });

            var payment = new Payment()
            {
                intent = "sale",
                payer = payer,
                transactions = transactionList,
                redirect_urls = redirUrls
            };

            return payment.Create(apiContext);
        }

        private APIContext GetAPIContext()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            var config = new Dictionary<string, string>
            {
                { "mode", ConfigurationManager.AppSettings["PayPalMode"] },
                { "clientId", ConfigurationManager.AppSettings["PayPalClientId"] },
                { "clientSecret", ConfigurationManager.AppSettings["PayPalClientSecret"] }
            };
            var accessToken = new OAuthTokenCredential(config).GetAccessToken();
            return new APIContext(accessToken);
        }

        public ActionResult PaymentWithPaypal(CheckoutVM model)
        {
            APIContext apiContext = GetAPIContext();

            try
            {
                string payerId = Request.Params["PayerID"];

                if (string.IsNullOrEmpty(payerId))
                {
                    // Lưu lại đầy đủ thông tin CheckoutVM trước khi rời trang sang PayPal
                    if (model.OrderDate == DateTime.MinValue)
                    {
                        model.OrderDate = DateTime.Now;
                    }
                    Session["CheckoutModel"] = model;

                    string baseURI = Request.Url.Scheme + "://" + Request.Url.Authority + "/Paypal/PaymentWithPaypal?";
                    var guid = Convert.ToString((new Random()).Next(100000));
                    var createdPayment = CreatePayment(apiContext, baseURI + "guid=" + guid, model);

                    var links = createdPayment.links.GetEnumerator();
                    string paypalRedirectUrl = null;
                    while (links.MoveNext())
                    {
                        Links lnk = links.Current;
                        if (lnk.rel.ToLower().Trim().Equals("approval_url"))
                        {
                            paypalRedirectUrl = lnk.href;
                        }
                    }

                    Session.Add(guid, createdPayment.id);
                    return Redirect(paypalRedirectUrl);
                }
                else
                {
                    var guid = Request.Params["guid"];
                    var executedPayment = ExecutePayment(apiContext, payerId, Session[guid] as string);

                    if (executedPayment.state.ToLower() != "approved")
                    {
                        ViewBag.ErrorMessage = "Trạng thái thanh toán chưa được duyệt: " + executedPayment.state;
                        return View("FailureView");
                    }

                    // Khôi phục lại CheckoutVM ban đầu để CreateOrder lưu đúng CustomerID, địa chỉ và tổng tiền
                    var savedModel = Session["CheckoutModel"] as CheckoutVM ?? model;
                    Session.Remove("CheckoutModel");

                    return RedirectToAction("CreateOrder", "Order", savedModel);
                }
            }
            catch (PayPal.PaymentsException pex)
            {
                ViewBag.ErrorMessage = !string.IsNullOrEmpty(pex.Response) ? pex.Response : pex.Message;
                return View("FailureView");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return View("FailureView");
            }
        }

        private Payment ExecutePayment(APIContext apiContext, string payerId, string paymentId)
        {
            var paymentExecution = new PaymentExecution() { payer_id = payerId };
            var payment = new Payment() { id = paymentId };
            return payment.Execute(apiContext, paymentExecution);
        }
    }
}