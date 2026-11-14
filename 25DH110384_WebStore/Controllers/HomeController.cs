using _25DH110384_WebStore.Models;
using _25DH110384_WebStore.Models.ViewModel;
using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;

namespace _25DH110384_WebStore.Controllers
{
    public class HomeController : Controller
    {
        private QuanLiKhachHangEntities1 db = new QuanLiKhachHangEntities1();

        public ActionResult Index(string searchTerm, int? page)
        {
            var model = new HomeProductVM();
            var products = db.Products.AsQueryable();

            // Tìm kiếm sản phẩm dựa trên từ khóa
            if (!string.IsNullOrEmpty(searchTerm))
            {
                model.SearchTerm = searchTerm;
                products = products.Where(p => p.ProductName.Contains(searchTerm) ||
                                              p.ProductDescription.Contains(searchTerm) ||
                                              p.Category.CategoryName.Contains(searchTerm));
            }

            int pageNumber = page ?? 1;
            int pageSize = 6;

            // Lấy top 10 sản phẩm bán chạy nhất
            model.FeaturedProducts = products.OrderByDescending(p => p.OrderDetails.Count()).Take(10).ToList();

            // Lấy 20 sản phẩm bán ế nhất và phân trang
            model.NewProducts = products.OrderBy(p => p.OrderDetails.Count()).Take(20).ToPagedList(pageNumber, pageSize);

            return View(model);
        }

        public ActionResult ProductDetails(int? id, int? quantity, int? page)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product pro = db.Products.Find(id);
            if (pro == null)
            {
                return HttpNotFound();
            }

            // Lấy tất cả các sản phẩm cùng danh mục
            var products = db.Products.Where(p => p.CategoryID == pro.CategoryID && p.ProductID != pro.ProductID).AsQueryable();

            ProductDetailVM model = new ProductDetailVM();

            int pageNumber = page ?? 1;
            int pageSize = model.PageSize;
            model.product = pro;
            model.RelatedProducts = products.OrderBy(p => p.ProductID).Take(8).ToPagedList(pageNumber, pageSize).ToList();
            model.TopProducts = products.OrderByDescending(p => p.OrderDetails.Count()).Take(8).ToPagedList(pageNumber, pageSize);

            if (quantity.HasValue)
            {
                model.quantity = quantity.Value;
            }

            return View(model);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";
            return View();
        }

        // ================= ĐĂNG NHẬP (LOGIN) =================

        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginVM model)
        {
            if (ModelState.IsValid)
            {
                var user = db.Users.FirstOrDefault(u => u.Username == model.Username && u.Password == model.Password);
                if (user != null)
                {
                    Session["Username"] = user.Username;
                    Session["UserRole"] = user.UserRole;

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không chính xác!";
                }
            }
            return View(model);
        }

        // ================= DANH MỤC ĐIỆN THOẠI =================

        // GET: Home/PageDienThoai
        public ActionResult PageDienThoai(string searchTerm, string sortOrder, int? page)
        {
            var model = new HomeProductVM();

            var products = db.Products
                .Include("Category")
                .Where(p => p.Category.CategoryName.Trim() == "Điện thoại")
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                model.SearchTerm = searchTerm;
                products = products.Where(p =>
                    p.ProductName.Contains(searchTerm) ||
                    p.ProductDescription.Contains(searchTerm));
            }

            ViewBag.CurrentSort = sortOrder;
            switch (sortOrder)
            {
                case "asc":
                    products = products.OrderBy(p => p.ProductPrice);
                    break;
                case "desc":
                    products = products.OrderByDescending(p => p.ProductPrice);
                    break;
                default:
                    products = products.OrderByDescending(p => p.ProductID);
                    break;
            }

            int pageNumber = page ?? 1;
            int pageSize = 8;
            model.NewProducts = products.ToPagedList(pageNumber, pageSize);

            return View(model);
        }


        public ActionResult PagePhuKien(string searchTerm, string sortOrder, int? page)
        {
            var model = new HomeProductVM();

            var products = db.Products
                .Include("Category")
                .Where(p => p.Category.CategoryName.Trim() == "Phụ kiện")
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                model.SearchTerm = searchTerm;
                products = products.Where(p =>
                    p.ProductName.Contains(searchTerm) ||
                    p.ProductDescription.Contains(searchTerm));
            }

            ViewBag.CurrentSort = sortOrder;
            switch (sortOrder)
            {
                case "asc":
                    products = products.OrderBy(p => p.ProductPrice);
                    break;
                case "desc":
                    products = products.OrderByDescending(p => p.ProductPrice);
                    break;
                default:
                    products = products.OrderByDescending(p => p.ProductID);
                    break;
            }

            int pageNumber = page ?? 1;
            int pageSize = 8;
            model.NewProducts = products.ToPagedList(pageNumber, pageSize);

            return View(model);
        }

        public ActionResult PageThietBi(string searchTerm, string sortOrder, int? page)
        {
            var model = new HomeProductVM();

            var products = db.Products
                .Include("Category")
                .Where(p => p.Category.CategoryName.Trim() == "Phụ kiện")
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                model.SearchTerm = searchTerm;
                products = products.Where(p =>
                    p.ProductName.Contains(searchTerm) ||
                    p.ProductDescription.Contains(searchTerm));
            }

            ViewBag.CurrentSort = sortOrder;
            switch (sortOrder)
            {
                case "asc":
                    products = products.OrderBy(p => p.ProductPrice);
                    break;
                case "desc":
                    products = products.OrderByDescending(p => p.ProductPrice);
                    break;
                default:
                    products = products.OrderByDescending(p => p.ProductID);
                    break;
            }

            int pageNumber = page ?? 1;
            int pageSize = 8;
            model.NewProducts = products.ToPagedList(pageNumber, pageSize);

            return View(model);
        }
    }
}