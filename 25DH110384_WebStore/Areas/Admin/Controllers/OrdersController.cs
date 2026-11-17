using _25DH110384_WebStore.Models;
using PagedList;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace _25DH110384_WebStore.Areas.Admin.Controllers
{
    public class OrdersController : Controller
    {
        private QuanLiKhachHangEntities1 db = new QuanLiKhachHangEntities1();

        // Danh sách trạng thái chuẩn dùng chung cho bộ lọc và cập nhật đơn hàng
        private List<string> GetAllOrderStatuses()
        {
            return new List<string>
            {
                "Chờ thanh toán",
                "Đang xử lý",
                "Đã xử lý",
                "Đang vận chuyển",
                
                "Đã hủy"
            };
        }

        // GET: Admin/Orders
        public ActionResult Index(string customerName, string paymentStatus, DateTime? orderDate, int? page)
        {
            var standardStatuses = GetAllOrderStatuses();
            var dbStatuses = db.Orders
                .Where(o => !string.IsNullOrEmpty(o.PaymentStatus))
                .Select(o => o.PaymentStatus)
                .Distinct()
                .ToList();

            ViewBag.PaymentStatuses = standardStatuses.Union(dbStatuses).ToList();
            ViewBag.AllStatuses = standardStatuses;

            var orders = db.Orders.Include(o => o.Customer).AsQueryable();

            // 1. Tìm theo Tên khách hàng hoặc Mã đơn hàng
            if (!string.IsNullOrWhiteSpace(customerName))
            {
                string keyword = customerName.Trim().ToLower();
                string numericPart = keyword.StartsWith("#") ? keyword.Substring(1) : keyword;
                int searchOrderId;
                bool isOrderId = int.TryParse(numericPart, out searchOrderId);

                orders = orders.Where(o =>
                    (isOrderId && o.OrderID == searchOrderId) ||
                    (o.Customer != null &&
                        ((o.Customer.CustomerName != null && o.Customer.CustomerName.ToLower().Contains(keyword)) ||
                         (o.Customer.Username != null && o.Customer.Username.ToLower().Contains(keyword))))
                );
            }

            // 2. Tìm theo Trạng thái thanh toán / xử lý
            if (!string.IsNullOrWhiteSpace(paymentStatus))
            {
                orders = orders.Where(o => o.PaymentStatus == paymentStatus);
            }

            // 3. Tìm theo Ngày đặt hàng
            if (orderDate.HasValue)
            {
                orders = orders.Where(o => DbFunctions.TruncateTime(o.OrderDate) == DbFunctions.TruncateTime(orderDate.Value));
            }

            ViewBag.CustomerName = customerName;
            ViewBag.PaymentStatus = paymentStatus;
            ViewBag.OrderDate = orderDate.HasValue ? orderDate.Value.ToString("yyyy-MM-dd") : "";

            int pageSize = 15;
            int pageNumber = (page ?? 1);
            if (pageNumber < 1) pageNumber = 1;

            var pagedOrders = orders
                .OrderByDescending(o => o.OrderDate)
                .ToPagedList(pageNumber, pageSize);

            return View(pagedOrders);
        }

        // GET: Admin/Orders/DetailedOrder/5
        public ActionResult DetailedOrder(int? id)
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

            ViewBag.AllStatuses = GetAllOrderStatuses();
            return View("DetailedOrder", order);
        }

        // GET: Admin/Orders/UpdateStatus/5
        public ActionResult UpdateStatus(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Order order = db.Orders
                            .Include(o => o.Customer)
                            .FirstOrDefault(o => o.OrderID == id);

            if (order == null)
            {
                return HttpNotFound();
            }

            ViewBag.AllStatuses = GetAllOrderStatuses();
            return View(order);
        }

        // POST: Admin/Orders/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateStatus(int OrderID, string PaymentStatus, string returnUrl = null)
        {
            var order = db.Orders.Find(OrderID);
            if (order == null)
            {
                return HttpNotFound();
            }

            if (!string.IsNullOrWhiteSpace(PaymentStatus))
            {
                order.PaymentStatus = PaymentStatus.Trim();
                db.SaveChanges();
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("DetailedOrder", new { id = OrderID });
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