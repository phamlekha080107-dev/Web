using _25DH110384_WebStore.Models;
using _25DH110384_WebStore.Models.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace _25DH110384_WebStore.Controllers
{
    public class OrderController : Controller
    {
        private QuanLiKhachHangEntities1 db = new QuanLiKhachHangEntities1();

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
            var customer = db.Customers.SingleOrDefault(c => c.Username == username);

            // Tự động tạo bản ghi Customer nếu chưa tồn tại trong CSDL
            if (customer == null)
            {
                var user = db.Users.SingleOrDefault(u => u.Username == username);
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
            var customer = db.Customers.SingleOrDefault(c => c.Username == username);

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
            return SaveOrderAndRedirect(model, customer, cartItemsList, "Thanh toán tiền mặt");
        }

        private ActionResult SaveOrderAndRedirect(CheckoutVM model, Customer customer, List<CartItem> cartItems, string paymentStatus)
        {
            DateTime currentDateTime = DateTime.Now;
            var order = new Order
            {
                CustomerID = customer.CustomerID,
                OrderDate = DateTime.Now, // Lưu chính xác ngày giờ bấm đặt hàng
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
            // Include thêm Product để nạp đủ Tên và Ảnh sản phẩm ra View
            var order = db.Orders.Include("OrderDetails.Product").SingleOrDefault(o => o.OrderID == id);
            if (order == null)
            {
                return HttpNotFound();
            }

            return View(order);
        }

        // GET: Order/MyOrder
        public ActionResult MyOrder()
        {
            if (Session["Username"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            string username = Session["Username"].ToString();
            var customer = db.Customers.FirstOrDefault(c => c.Username == username);

            if (customer == null)
            {
                return View(new List<Order>());
            }

            var orders = db.Orders
                           .Include("OrderDetails.Product")
                           .Where(o => o.CustomerID == customer.CustomerID)
                           .OrderByDescending(o => o.OrderDate)
                           .ToList();

            return View(orders);
        }

        // Mua lại đơn hàng cũ -> Đưa vào giỏ hàng và chuyển đến trang Giỏ hàng để người dùng tùy chỉnh số lượng
        public ActionResult BuyAgain(int id)
        {
            // Kiểm tra đăng nhập
            if (Session["Username"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Tìm đơn hàng kèm chi tiết sản phẩm và sản phẩm
            var db = new QuanLiKhachHangEntities1();
            var order = db.Orders
                          .Include("OrderDetails.Product")
                          .SingleOrDefault(o => o.OrderID == id);

            if (order != null && order.OrderDetails != null)
            {
                // Lấy dịch vụ giỏ hàng thông quan Session
                var cartService = new CartService(Session);

                foreach (var detail in order.OrderDetails)
                {
                    if (detail.Product != null)
                    {
                        cartService.GetCart().AddItem(
                            detail.Product.ProductID,
                            detail.Product.ProductImage,
                            detail.Product.ProductName,
                            detail.UnitPrice,
                            detail.Quantity,
                            detail.Product.Category != null ? detail.Product.Category.CategoryName : string.Empty
                        );
                    }
                }
            }

            // ĐÃ SỬA: Chuyển hướng sang trang Giỏ hàng (Cart/Index) thay vì trang Checkout
            return RedirectToAction("Index", "Cart");
        }
    }
}