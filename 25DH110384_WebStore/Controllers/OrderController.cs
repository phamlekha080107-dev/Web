using _25DH110384_WebStore.Models;
using _25DH110384_WebStore.Models.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace _25DH110384_WebStore.Controllers
{
    public class OrderController : Controller
    {
        private QuanLiKhachHangEntities1 db = new QuanLiKhachHangEntities1();

        // GET: Order
        public ActionResult Index()
        {
            return View();
        }

        // GET: Order/Checkout
        public ActionResult Checkout()
        {
            // 1. Kiểm tra giỏ hàng
            var cart = Session["Cart"] as List<CartItem>;
            if (cart == null || !cart.Any())
            {
                return RedirectToAction("Index", "Home");
            }

            // 2. Kiểm tra xác thực người dùng qua Session
            if (Session["Username"] == null)
            {
                return RedirectToAction("Login", "Home");
            }

            string username = Session["Username"].ToString();

            // 3. Lấy thông tin khách hàng từ CSDL
            var customer = db.Customers.SingleOrDefault(c => c.Username == username);
            if (customer == null)
            {
                return RedirectToAction("Login", "Home");
            }

            var model = new CheckoutVM
            {
                CartItems = cart,
                TotalAmount = cart.Sum(item => item.TotalPrice),
                OrderDate = DateTime.Now,
                ShippingAddress = customer.CustomerAddress,
                CustomerID = customer.CustomerID,
                Username = customer.Username
            };

            return View(model);
        }

        // POST: Order/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        // GET: Order/Checkout
        public ActionResult CheckOut()
        {
            var cart = Session["Cart"] as List<CartItem>;
            if (cart == null || !cart.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            if (Session["Username"] == null)
            {
                return RedirectToAction("Login", "Home");
            }

            string username = Session["Username"].ToString();

            // 1. Tìm thông tin Customer
            var customer = db.Customers.SingleOrDefault(c => c.Username == username);

            // 2. Tự tạo Customer mặc định nếu tài khoản chưa có trong bảng Customer
            if (customer == null)
            {
                var user = db.Users.SingleOrDefault(u => u.Username == username);
                if (user == null)
                {
                    return RedirectToAction("Login", "Home");
                }

                customer = new Customer
                {
                    Username = user.Username,
                    CustomerName = user.Username,
                    CustomerAddress = "Chưa cập nhật địa chỉ",
                    CustomerPhone = "0123456789",
                    CustomerEmail = user.Username + "@gmail.com"
                };
                db.Customers.Add(customer);
                db.SaveChanges();
            }

            var model = new CheckoutVM
            {
                CartItems = cart,
                TotalAmount = cart.Sum(item => item.TotalPrice),
                OrderDate = DateTime.Now,
                ShippingAddress = customer.CustomerAddress,
                CustomerID = customer.CustomerID,
                Username = customer.Username
            };

            return View(model);
        }

        // Action nhận callback lưu đơn hàng sau khi thanh toán thành công (PayPal hoặc COD)
        public ActionResult CreateOrder(CheckoutVM model)
        {
            var cart = Session["Cart"] as List<CartItem>;
            if (cart == null || !cart.Any())
            {
                return RedirectToAction("Index", "Home");
            }

            string username = Session["Username"] != null ? Session["Username"].ToString() : model.Username;
            var customer = db.Customers.SingleOrDefault(c => c.Username == username);

            if (customer == null)
            {
                return RedirectToAction("Login", "Home");
            }

            string paymentStatus = model.PaymentMethod == "Paypal" ? "Thanh toán paypal" : "Thanh toán tiền mặt";
            return SaveOrderAndRedirect(model, customer, cart, paymentStatus);
        }

        // Hàm dùng chung để lưu Order & OrderDetail vào CSDL
        private ActionResult SaveOrderAndRedirect(CheckoutVM model, Customer customer, List<CartItem> cart, string paymentStatus)
        {
            var order = new Order
            {
                CustomerID = customer.CustomerID,
                OrderDate = DateTime.Now,
                TotalAmount = model.TotalAmount,
                PaymentStatus = paymentStatus,
                PaymentMethod = model.PaymentMethod,
                ShippingMethod = model.ShippingMethod,
                ShippingAddress = model.ShippingAddress,
                OrderDetails = cart.Select(item => new OrderDetail
                {
                    ProductID = item.ProductID,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice
                }).ToList()
            };

            db.Orders.Add(order);
            db.SaveChanges();

            // Xóa giỏ hàng sau khi đặt hàng thành công
            Session["Cart"] = null;

            return RedirectToAction("OrderSuccess", new { id = order.OrderID });
        }

        // GET: Order/OrderSuccess/5
        public ActionResult OrderSuccess(int id)
        {
            var order = db.Orders.Include("OrderDetails").SingleOrDefault(o => o.OrderID == id);
            if (order == null)
            {
                return HttpNotFound();
            }

            return View(order);
        }


        // GET: Order/MyOrder
        public ActionResult MyOrder()
        {
            // Kiểm tra đăng nhập qua Session
            if (Session["Username"] == null)
            {
                return RedirectToAction("Login", "Home");
            }

            string username = Session["Username"].ToString();
            var customer = db.Customers.FirstOrDefault(c => c.Username == username);

            if (customer == null)
            {
                return View(new List<Order>());
            }

            // Lấy danh sách đơn hàng kèm theo thông tin chi tiết và sản phẩm
            var orders = db.Orders
                           .Include("OrderDetails.Product")
                           .Where(o => o.CustomerID == customer.CustomerID)
                           .OrderByDescending(o => o.OrderDate)
                           .ToList();

            return View(orders);
        }
    }
}