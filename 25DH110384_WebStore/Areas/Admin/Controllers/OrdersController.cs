using _25DH110384_WebStore.Models;
using PagedList;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace _25DH110384_WebStore.Areas.Admin.Controllers
{
    public class OrdersController : Controller
    {
        private QuanLiKhachHangEntities1 db = new QuanLiKhachHangEntities1();

        // GET: Admin/Orders
        public ActionResult Index(string customerName, string paymentStatus, DateTime? orderDate, int? page)
        {
            // Lấy danh sách các trạng thái thanh toán hiện có trong DB để hiển thị lên dropdown
            ViewBag.PaymentStatuses = db.Orders
                .Where(o => !string.IsNullOrEmpty(o.PaymentStatus))
                .Select(o => o.PaymentStatus)
                .Distinct()
                .ToList();

            var orders = db.Orders.Include(o => o.Customer).AsQueryable();

            // 1. Tìm theo Tên khách hàng (hoặc Username)
            if (!string.IsNullOrWhiteSpace(customerName))
            {
                string keyword = customerName.Trim().ToLower();
                orders = orders.Where(o => o.Customer != null &&
                    ((o.Customer.CustomerName != null && o.Customer.CustomerName.ToLower().Contains(keyword)) ||
                     (o.Customer.Username != null && o.Customer.Username.ToLower().Contains(keyword))));
            }

            // 2. Tìm theo Trạng thái thanh toán
            if (!string.IsNullOrWhiteSpace(paymentStatus))
            {
                orders = orders.Where(o => o.PaymentStatus == paymentStatus);
            }

            // 3. Tìm theo Ngày đặt hàng
            if (orderDate.HasValue)
            {
                orders = orders.Where(o => DbFunctions.TruncateTime(o.OrderDate) == DbFunctions.TruncateTime(orderDate.Value));
            }

            // Lưu lại điều kiện tìm kiếm để giữ trên giao diện và khi chuyển trang
            ViewBag.CustomerName = customerName;
            ViewBag.PaymentStatus = paymentStatus;
            ViewBag.OrderDate = orderDate.HasValue ? orderDate.Value.ToString("yyyy-MM-dd") : "";

            // Phân trang: 15 đơn hàng / trang
            int pageSize = 15;
            int pageNumber = (page ?? 1);
            if (pageNumber < 1) pageNumber = 1;

            var pagedOrders = orders
                .OrderByDescending(o => o.OrderDate)
                .ToPagedList(pageNumber, pageSize);

            return View(pagedOrders);
        }

        // GET: Admin/Orders/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Order order = db.Orders
                            .Include(o => o.Customer)
                            .Include(o => o.OrderDetails.Select(od => od.Product))
                            .FirstOrDefault(o => o.OrderID == id);

            if (order == null)
            {
                return HttpNotFound();
            }
            return View(order);
        }

        // POST: Admin/Orders/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var order = db.Orders.Find(id);
            if (order != null)
            {
                var orderDetails = db.OrderDetails.Where(od => od.OrderID == id).ToList();
                if (orderDetails.Any())
                {
                    db.OrderDetails.RemoveRange(orderDetails);
                }

                db.Orders.Remove(order);
                db.SaveChanges();
            }

            return RedirectToAction("Index");
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