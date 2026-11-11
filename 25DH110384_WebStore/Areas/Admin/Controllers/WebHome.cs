using _25DH110384_WebStore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace _25DH110384_WebStore.Areas.Admin.Controllers
{
    public class WebHomeController : Controller
    {
        // GET: Admin/Home
        // Khởi tạo DbContext (sửa tên DbContext nếu của em khác)
        private QuanLiKhachHangEntities1 db = new QuanLiKhachHangEntities1();

        // GET: Admin/Home
        public ActionResult Index()
        {
            // Truy vấn LINQ thống kê theo từng Loại Hàng (Category)
            var thongKeData = db.Products
                .GroupBy(p => p.Category.CategoryName) // Nhóm theo tên loại hàng từ bảng Category
                .Select(g => new ThongKeVM
                {
                    LoaiHang = g.Key,
                    SoLuong = g.Count(),
                    GiaCaoNhat = g.Max(p => p.ProductPrice), // Sử dụng ProductPrice theo đúng model của em
                    GiaThapNhat = g.Min(p => p.ProductPrice),
                    GiaTrungBinh = (decimal)g.Average(p => p.ProductPrice)
                }).ToList();

            return View(thongKeData);
        }
    }
}