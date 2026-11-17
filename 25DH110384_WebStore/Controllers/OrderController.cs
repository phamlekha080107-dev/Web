using _25DH110384_WebStore.Models;
using _25DH110384_WebStore.Models.ViewModel;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace _25DH110384_WebStore.Controllers
{
    public class OrderController : Controller
    {
        private QuanLiKhachHangEntities1 db = new QuanLiKhachHangEntities1();

        // Chuẩn hóa trạng thái đơn hàng về 5 nhóm hiển thị trên các Tab
        private string NormalizeOrderStatus(string rawStatus)
        {
            if (string.IsNullOrWhiteSpace(rawStatus))
                return "Chờ thanh toán";

            string s = rawStatus.Trim().ToLower();
            if (s.Contains("chờ") || s.Contains("chưa thanh toán"))
                return "Chờ thanh toán";
            if (s.Contains("xử lý") || s.Contains("tiền mặt") || s.Contains("đã thanh toán") || s.Contains("paypal"))
                return "Đang xử lý";
            if (s.Contains("vận chuyển") || s.Contains("đang giao"))
                return "Đang vận chuyển";
            if (s.Contains("đã giao"))
                return "Đã giao";
            if (s.Contains("hủy"))
                return "Đã hủy";

            return "Chờ thanh toán";
        }

        // GET: Order/Checkout
        public ActionResult Checkout()
        {
            // 1. Kiểm tra giỏ hàng
            var cart = Session["Cart"] as _25DH110384_WebStore.Models.ViewModel.Cart;
            if (cart == null || cart.Items == null || !cart.Items.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            // 2. Kiểm tra xác thực người dùng
            if (Session["Username"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            string username = Session["Username"].ToString();
            var customer = db.Customers.FirstOrDefault(c => c.Username == username);

            // Tự động tạo bản ghi Customer nếu chưa tồn tại trong CSDL
            if (customer == null)
            {
                var user = db.Users.FirstOrDefault(u => u.Username == username);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
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

            // Chuyển đổi dữ liệu Giỏ hàng sang List<CartItem> cho CheckoutVM
            var cartItemsList = cart.Items.Select(i => new CartItem
            {
                ProductID = i.ProductID,
                ProductName = i.ProductName,
                ProductImage = i.ProductImage,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity
            }).ToList();

            var model = new CheckoutVM
            {
                CartItems = cartItemsList,
                TotalAmount = cartItemsList.Sum(item => item.TotalPrice),
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
        public ActionResult Checkout(CheckoutVM model)
        {
            var cart = Session["Cart"] as _25DH110384_WebStore.Models.ViewModel.Cart;
            if (cart == null || cart.Items == null || !cart.Items.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            if (Session["Username"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            string username = Session["Username"].ToString();
            var customer = db.Customers.FirstOrDefault(c => c.Username == username);

            if (customer == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Nếu chọn thanh toán Paypal
            if (model.PaymentMethod == "Paypal")
            {
                return RedirectToAction("PaymentWithPaypal", "Paypal", model);
            }

            // Chuyển đổi item để lưu đơn hàng
            var cartItemsList = cart.Items.Select(i => new CartItem
            {
                ProductID = i.ProductID,
                ProductName = i.ProductName,
                ProductImage = i.ProductImage,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
            }).ToList();

            // Lưu đơn hàng với thời gian thực hiện tại
            return SaveOrderAndRedirect(model, customer, cartItemsList, "Đang xử lý");
        }

        private ActionResult SaveOrderAndRedirect(CheckoutVM model, Customer customer, List<CartItem> cartItems, string paymentStatus)
        {
            var order = new Order
            {
                CustomerID = customer.CustomerID,
                OrderDate = DateTime.Now,
                TotalAmount = model.TotalAmount,
                PaymentStatus = paymentStatus,
                PaymentMethod = model.PaymentMethod ?? "Tiền mặt",
                ShippingMethod = model.ShippingMethod ?? "Giao hàng nhanh",
                ShippingAddress = string.IsNullOrEmpty(model.ShippingAddress) ? customer.CustomerAddress : model.ShippingAddress,
                OrderDetails = cartItems.Select(item => new OrderDetail
                {
                    ProductID = item.ProductID,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice
                }).ToList()
            };

            db.Orders.Add(order);
            db.SaveChanges();

            // Xóa sạch giỏ hàng sau khi đặt thành công
            Session["Cart"] = null;

            return RedirectToAction("OrderSuccess", new { id = order.OrderID });
        }

        // GET: Order/OrderSuccess/5
        public ActionResult OrderSuccess(int id)
        {
            var order = db.Orders.Include("OrderDetails.Product").SingleOrDefault(o => o.OrderID == id);
            if (order == null)
            {
                return HttpNotFound();
            }

            return View(order);
        }

        // GET: Order/MyOrder
        public ActionResult MyOrder(string searchTerm, string status = "Tất cả đơn", bool showAll = false)
        {
            if (Session["Username"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            string username = Session["Username"].ToString();
            string currentStatus = string.IsNullOrWhiteSpace(status) ? "Tất cả đơn" : status.Trim();

            ViewBag.SearchTerm = searchTerm;
            ViewBag.SelectedStatus = currentStatus;
            ViewBag.ShowAll = showAll;
            ViewBag.HasMore = false;

            // Lấy toàn bộ CustomerID gắn với Username đang đăng nhập
            var customerIds = db.Customers
                                .Where(c => c.Username == username)
                                .Select(c => (int?)c.CustomerID)
                                .ToList();

            if (!customerIds.Any())
            {
                return View(new List<Order>());
            }

            var query = db.Orders
                          .AsNoTracking()
                          .Include("OrderDetails.Product")
                          .Where(o => customerIds.Contains(o.CustomerID))
                          .AsQueryable();

            // 1. Tìm kiếm theo Mã đơn hàng hoặc Tên sản phẩm
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string keyword = searchTerm.Trim().ToLower();
                string numericPart = keyword.StartsWith("#") ? keyword.Substring(1) : keyword;
                int searchOrderId;
                bool isOrderId = int.TryParse(numericPart, out searchOrderId);

                query = query.Where(o =>
                    (isOrderId && o.OrderID == searchOrderId) ||
                    o.OrderDetails.Any(od => od.Product != null &&
                                             od.Product.ProductName != null &&
                                             od.Product.ProductName.ToLower().Contains(keyword))
                );
            }

            var orders = query
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            // Chuẩn hóa tên trạng thái để gom nhóm và lọc tab chính xác
            foreach (var order in orders)
            {
                order.PaymentStatus = NormalizeOrderStatus(order.PaymentStatus);
            }

            // 2. Lọc theo từng tab trạng thái cụ thể (2 đơn gần nhất, Xem thêm tối đa 50 đơn)
            if (currentStatus != "Tất cả đơn")
            {
                var statusOrders = orders.Where(o => o.PaymentStatus == currentStatus).ToList();
                ViewBag.HasMore = !showAll && statusOrders.Count > 2;
                orders = statusOrders.Take(showAll ? 50 : 2).ToList();
            }

            return View(orders);
        }

        // Mua lại -> Thêm sản phẩm vào Giỏ hàng với số lượng = 1
        public ActionResult BuyAgain(int id, int? productId)
        {
            if (Session["Username"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var order = db.Orders
                          .Include("OrderDetails.Product.Category")
                          .SingleOrDefault(o => o.OrderID == id);

            if (order != null && order.OrderDetails != null)
            {
                var cartService = new CartService(Session);

                var detailsToAdd = productId.HasValue
                    ? order.OrderDetails.Where(d => d.ProductID == productId.Value)
                    : order.OrderDetails;

                foreach (var detail in detailsToAdd)
                {
                    if (detail.Product != null)
                    {
                        cartService.GetCart().AddItem(
                            detail.Product.ProductID,
                            detail.Product.ProductImage,
                            detail.Product.ProductName,
                            detail.UnitPrice,
                            1,
                            detail.Product.Category != null ? detail.Product.Category.CategoryName : string.Empty
                        );
                    }
                }
            }

            return RedirectToAction("Index", "Cart");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}