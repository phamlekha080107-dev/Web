using _25DH110384_WebStore.Models;
using _25DH110384_WebStore.Models.ViewModel;
using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;

namespace _25DH110384_WebStore.Controllers
{
    public class CartController : Controller
    {
        private QuanLiKhachHangEntities1 db = new QuanLiKhachHangEntities1();

        // Hàm lấy dịch vụ giỏ hàng
        private CartService GetCartService()
        {
            return new CartService(Session);
        }

        // Hiển thị giỏ hàng
        public ActionResult Index(int? page)
        {
            var cart = GetCartService().GetCart();
            var products = db.Products.ToList();
            var similarProducts = new List<Product>();

            if (cart.Items != null && cart.Items.Any())
            {
                similarProducts = products.Where(p => cart.Items.Any(ci => ci.Category == p.Category.CategoryName)
                    && !cart.Items.Any(ci => ci.ProductID == p.ProductID)).ToList();
            }

            int pageNumber = page ?? 1;
            int pageSize = cart.PageSize;

            cart.SimilarProducts = similarProducts.OrderBy(p => p.ProductID).ToPagedList(pageNumber, pageSize);
            return View(cart);
        }

        // Thêm sản phẩm vào giỏ (BẮT BUỘC ĐĂNG NHẬP)
        public ActionResult AddToCart(int id, int quantity = 1)
        {
            // 1. Kiểm tra xem người dùng đã đăng nhập hay chưa
            if (Session["Username"] == null)
            {
                // Chưa đăng nhập -> Chuyển hướng sang trang đăng nhập của AccountController
                return RedirectToAction("Login", "Account");
            }

            // 2. Nếu đã đăng nhập -> Thực hiện thêm sản phẩm vào giỏ hàng bình thường
            var product = db.Products.Find(id);
            if (product != null)
            {
                var cartService = GetCartService();
                cartService.GetCart().AddItem(
                    product.ProductID,
                    product.ProductImage,
                    product.ProductName,
                    product.ProductPrice,
                    quantity,
                    product.Category != null ? product.Category.CategoryName : string.Empty
                );
            }
            return RedirectToAction("Index");
        }

        // Xóa sản phẩm khỏi giỏ
        public ActionResult RemoveFromCart(int id)
        {
            var cartService = GetCartService();
            cartService.GetCart().RemoveItem(id);
            return RedirectToAction("Index");
        }

        // Làm trống giỏ hàng
        public ActionResult ClearCart()
        {
            GetCartService().ClearCart();
            return RedirectToAction("Index");
        }

        // Cập nhật số lượng của sản phẩm đã chọn
        [HttpPost]
        public ActionResult UpdateQuantity(int id, int quantity)
        {
            var cartService = GetCartService();
            cartService.GetCart().UpdateQuantity(id, quantity);
            return RedirectToAction("Index");
        }

        
    }
}